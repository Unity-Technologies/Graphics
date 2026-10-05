using Unity.GraphCommon.LowLevel.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    class ParticleSystemPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            var particleSystemContainer = context.data.GetOrCreate<ParticleSystemContainer>();
            var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();

            // One system per system task, found before any task is assigned to one, so that a task can
            // name its system - or an init task its parent - whatever order the systems were built in.
            using var taskNodeIds = context.graph.TaskNodes.Snapshot();
            foreach (var taskNodeId in taskNodeIds)
            {
                var systemTaskNode = context.graph.TaskNodes[taskNodeId];
                if (systemTaskNode.Task is not ParticleSystemTask)
                    continue;

                var particleDataView = systemTaskNode.DataBindings[0].DataView;
                if (particleDataView.DataDescription is not ParticleData particleData)
                    continue;

                var particleSystem = new ParticleSystemContainer.ParticleSystem(particleData.Name);
                particleSystem.Capacity = particleData.Capacity;
                particleSystem.SetSystemTask(new ParticleSystemContainer.Task("System", taskNodeId));
                if (GenerateDeadList(systemTaskNode, particleDataView, layoutCompilationData, context.graph))
                    particleSystem.SystemFlags |= UnityEngine.VFX.VFXSystemFlag.SystemHasKill;

                particleSystemContainer.Add(particleDataView.Id, particleSystem);
            }

            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is ParticleSystemTask)
                    continue;

                var particleSystem = FindOwner(taskNode, particleSystemContainer);
                if (particleSystem == null)
                    continue;

                if (taskNode.Task is VfxTemplatedTask templatedTask)
                {
                    particleSystemContainer.AddTask(particleSystem,
                        new ParticleSystemContainer.Task(taskNode.Name, taskNode.Id));

                    if (templatedTask.Stage == VfxTaskStage.Initialize)
                        LinkParent(taskNode, particleSystem, particleSystemContainer);
                    if (templatedTask.IsOutput)
                        FlagIndirectDraw(taskNode, particleSystem);
                }
                else
                {
                    // Its runtime type is the lowering's to tell, and ReplaceTask's to record.
                    particleSystemContainer.AddTask(particleSystem,
                        new ParticleSystemContainer.Task(taskNode.Name, taskNode.Id));
                }
            }

            PropagateOwnership(particleSystemContainer, context.graph.CreateTraverser());

            return true;
        }

        // A task is a system's when it binds that system's particles under the particle data key, the
        // way a context task binds the system it is a context of. Reading another system's particles -
        // the source attributes a GPU event hands an init task - goes under another key, and claims
        // nothing.
        static ParticleSystemContainer.ParticleSystem FindOwner(TaskNode taskNode, ParticleSystemContainer particleSystemContainer)
        {
            ParticleSystemContainer.ParticleSystem owner = null;
            foreach (var binding in taskNode.DataBindings)
            {
                if (!binding.BindingDataKey.Equals(VfxTemplateBindings.ParticleDataBindingKey))
                    continue;

                var particleSystem = particleSystemContainer.Find(binding.DataView.DataContainer.RootDataView.Id);
                Debug.Assert(particleSystem != null, $"Task {taskNode.Name} binds particles that belong to no system.");
                Debug.Assert(owner == null || owner == particleSystem, $"Task {taskNode.Name} binds the particles of both {owner?.Name} and {particleSystem?.Name}.");
                owner = particleSystem;
            }
            return owner;
        }

        // An init task reading another system's particles is fed by that system's GPU events, which
        // makes it the parent.
        static void LinkParent(TaskNode initTaskNode, ParticleSystemContainer.ParticleSystem particleSystem, ParticleSystemContainer particleSystemContainer)
        {
            foreach (var dataNode in initTaskNode.DataNodes)
            {
                var rootDataView = dataNode.DataContainer.RootDataView;
                if (rootDataView.DataDescription is not ParticleData)
                    continue;

                var parent = particleSystemContainer.Find(rootDataView.Id);
                if (parent == null || parent == particleSystem)
                    continue;

                Debug.Assert(particleSystem.Parent == null || particleSystem.Parent == parent, $"{particleSystem.Name} is fed by more than one system.");
                particleSystem.Parent = parent;
                particleSystem.SystemFlags |= UnityEngine.VFX.VFXSystemFlag.SystemReceivedEventGPU;
            }
        }

        // An output drawing through an indirection is what gives its system an indirect buffer.
        static void FlagIndirectDraw(TaskNode outputTaskNode, ParticleSystemContainer.ParticleSystem particleSystem)
        {
            foreach (var dataNode in outputTaskNode.DataNodes)
            {
                if (dataNode.DataContainer.RootDataView.DataDescription is not RenderingData)
                    continue;

                foreach (var dataView in dataNode.UsedDataViews)
                {
                    if (dataView.DataDescription is DrawIndirectionData)
                    {
                        particleSystem.SystemFlags |= UnityEngine.VFX.VFXSystemFlag.SystemHasIndirectBuffer;
                        return;
                    }
                }
            }
        }

        // A task that works on no particle data of its own - a sort, which only ever sees the rendering
        // data an output update filled - belongs to the system that feeds it. Parents-first, so a task
        // is only ever asked about once its parents have been answered.
        static void PropagateOwnership(ParticleSystemContainer particleSystemContainer, GraphTraverser traverser)
        {
            foreach (var taskNode in traverser.TraverseTasksParentsFirst())
            {
                if (particleSystemContainer.TryGetOwner(taskNode.Id, out _))
                    continue;

                ParticleSystemContainer.ParticleSystem inherited = null;
                foreach (var parent in taskNode.Parents)
                {
                    if (!particleSystemContainer.TryGetOwner(parent.Id, out var owner))
                        continue;

                    if (inherited == null)
                    {
                        inherited = owner;
                        // Its runtime type is the lowering's to tell, and ReplaceTask's to record.
                        particleSystemContainer.AddTask(owner, new ParticleSystemContainer.Task(taskNode.Name, taskNode.Id));
                    }
                    else if (inherited != owner)
                    {
                        // Nothing feeds a task from two systems today without binding one of their
                        // particle data, which would have settled it outright.
                        Debug.LogWarning($"Task {taskNode.Name} is fed by both {inherited.Name} and " +
                            $"{owner.Name}; it is emitted as part of {inherited.Name}.");
                    }
                }
            }
        }

        bool GenerateDeadList(TaskNode systemTaskNode, in DataView particleDataView, AttributeSetLayoutCompilationData layoutCompilationData, IGraph graph)
        {
            if (particleDataView.FindSubData(ParticleData.AttributeDataKey, out var attributeDataView))
            {
                var attributeData = attributeDataView.DataDescription as AttributeData;
                var layout = layoutCompilationData[attributeData];
                if (layout.ContainsAttribute(VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.Alive)))
                {
                    if (particleDataView.FindSubData(ParticleData.DeadlistKey, out var deadListData))
                    {
                        graph.OverrideDataDescription(deadListData.Id, new DeadListData());
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// The particle systems of the effect and the tasks each one is made of. A task joins a system
    /// through <see cref="AddTask"/> alone, which is what keeps "which system owns this task" and "which
    /// tasks make up this system" one and the same thing: <see cref="ParticleSystemPass"/> adds the tasks
    /// bound to a system's particle data and the ones fed by those, the lowering swaps each for the task
    /// it lowered to, and the list is only ever reordered afterwards, never rebuilt.
    /// </summary>
    class ParticleSystemContainer : IEnumerable, IEnumerable<ParticleSystemContainer.ParticleSystem>
    {
        // The one way to a system's mutable task list, kept to this class by being private to it.
        interface ITaskList
        {
            List<Task> Tasks { get; }
        }

        readonly Dictionary<DataViewId, ParticleSystem> m_ParticleSystems = new();
        readonly Dictionary<TaskNodeId, ParticleSystem> m_Owners = new();

        public int Count => m_ParticleSystems.Count;

        public void Add(DataViewId particleDataViewId, ParticleSystem particleSystem) => m_ParticleSystems.Add(particleDataViewId, particleSystem);

        public ParticleSystem Find(DataViewId particleDataViewId)
        {
            m_ParticleSystems.TryGetValue(particleDataViewId, out var particleSystem);
            return particleSystem;
        }

        public bool TryGetOwner(TaskNodeId taskNodeId, out ParticleSystem particleSystem) => m_Owners.TryGetValue(taskNodeId, out particleSystem);

        /// <summary>
        /// Makes a task one of the system's. A logical task with no runtime type yet - a sort, before it
        /// is lowered - is added with none, for <see cref="ReplaceTask"/> to fill in once lowered.
        /// </summary>
        public void AddTask(ParticleSystem particleSystem, in Task task)
        {
            Debug.Assert(!m_Owners.ContainsKey(task.Id), $"Task {task.Name} is already part of a system.");
            m_Owners.Add(task.Id, particleSystem);
            TasksOf(particleSystem).Add(task);
        }

        /// <summary>
        /// Puts the task a logical one was lowered to in its place in the system. Nothing to do for a
        /// task that belongs to no system.
        /// </summary>
        public void ReplaceTask(TaskNodeId oldTaskNodeId, in Task newTask)
        {
            if (!m_Owners.Remove(oldTaskNodeId, out var particleSystem))
                return;

            m_Owners.Add(newTask.Id, particleSystem);
            var tasks = TasksOf(particleSystem);
            tasks[IndexOf(tasks, oldTaskNodeId)] = newTask;
        }

        public void SetInstanceSplitIndex(ParticleSystem particleSystem, int taskIndex, uint instanceSplitIndex)
        {
            var tasks = TasksOf(particleSystem);
            var task = tasks[taskIndex];
            task.InstanceSplitIndex = instanceSplitIndex;
            tasks[taskIndex] = task;
        }

        static List<Task> TasksOf(ParticleSystem particleSystem) => ((ITaskList)particleSystem).Tasks;

        static int IndexOf(List<Task> tasks, TaskNodeId taskNodeId)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Id.Equals(taskNodeId))
                    return i;
            }
            throw new KeyNotFoundException($"Task {taskNodeId} owned by a system is not in its task list.");
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<ParticleSystem> IEnumerable<ParticleSystem>.GetEnumerator() => GetEnumerator();
        public Dictionary<DataViewId, ParticleSystem>.ValueCollection.Enumerator GetEnumerator() => m_ParticleSystems.Values.GetEnumerator();

        /// <summary>
        /// The systems with every parent ahead of its children, which is the order the asset lists them
        /// in: a child names its parent by index, so the parent has to have one by then.
        /// </summary>
        public IEnumerable<ParticleSystem> EnumerateParentsFirst()
        {
            var emitted = new HashSet<ParticleSystem>();
            var pending = new List<ParticleSystem>(m_ParticleSystems.Values);
            var wave = new List<ParticleSystem>();

            while (pending.Count > 0)
            {
                // A whole depth is collected before any of it is emitted, so that emitting a parent
                // cannot pull its own child into the same wave.
                wave.Clear();
                foreach (var particleSystem in pending)
                {
                    if (particleSystem.Parent == null || emitted.Contains(particleSystem.Parent))
                        wave.Add(particleSystem);
                }

                // A parent outside this container, or a cycle: nothing left can ever become ready, so
                // yield the remainder in insertion order rather than dropping it silently.
                if (wave.Count == 0)
                {
                    foreach (var particleSystem in pending)
                        yield return particleSystem;
                    yield break;
                }

                foreach (var particleSystem in wave)
                {
                    emitted.Add(particleSystem);
                    pending.Remove(particleSystem);
                    yield return particleSystem;
                }
            }
        }

        public class ParticleSystem : ITaskList
        {
            readonly List<Task> m_Tasks = new();

            public string Name { get; }
            public uint Capacity { get; set; }
            public IReadOnlyList<Task> Tasks => m_Tasks;
            List<Task> ITaskList.Tasks => m_Tasks;

            public Task SystemTask { get; private set; }

            public ParticleSystem Parent { get; set; }
            public UnityEngine.VFX.VFXSystemFlag SystemFlags { get; set; }

            public void SetSystemTask(Task systemTask)
            {
                SystemTask = systemTask;
            }

            public ParticleSystem(string name)
            {
                Name = name;
            }
        }

        public struct Task
        {
            public string Name { get; }
            public TaskNodeId Id { get; }
            public uint InstanceSplitIndex { get; set; }

            public Task(string name, TaskNodeId id)
            {
                Name = name;
                Id = id;
                InstanceSplitIndex = 0;
            }
        }
    }
}

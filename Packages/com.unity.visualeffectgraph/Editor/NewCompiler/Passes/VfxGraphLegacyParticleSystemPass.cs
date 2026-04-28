using Unity.GraphCommon.LowLevel.Editor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VfxGraphLegacyParticleSystemPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            List<TaskNodeId> systemTaskNodeIds = new(); //TODO: Temp, due to graph being fully invalidated when changing data

            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is PlaceholderSystemTask systemTask)
                {
                    systemTaskNodeIds.Add(taskNode.Id);
                }
            }
            var particleSystemContainer = context.data.GetOrCreate<VfxGraphLegacyParticleSystemContainer>();
            var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();
            var traverser = context.graph.CreateTraverser();

            foreach (var systemTaskNodeId in systemTaskNodeIds)
            {
                var systemTaskNode = context.graph.TaskNodes[systemTaskNodeId];

                var particleDataView = systemTaskNode.DataBindings[0].DataView;
                if (particleDataView.DataDescription is ParticleData particleData)
                {
                    CollectParticleSystem(systemTaskNode, particleDataView, traverser, particleSystemContainer);
                    GenerateDeadList(systemTaskNode, particleDataView, layoutCompilationData, context.graph);
                }
            }

            return true;
        }

        void CollectParticleSystem(TaskNode systemTaskNode, in DataView particleDataView, GraphTraverser traverser, VfxGraphLegacyParticleSystemContainer particleSystemContainer)
        {
            var particleSystem = new VfxGraphLegacyParticleSystemContainer.ParticleSystem($"ParticleSystem{particleSystemContainer.Count + 1}");
            var particleData = particleDataView.DataDescription as ParticleData;

            particleSystem.Capacity = particleData.Capacity;
            CollectTasks(systemTaskNode, particleDataView, traverser, particleSystemContainer, particleSystem);
            particleSystemContainer.Add(particleDataView.Id, particleSystem);
        }

        void CollectTasks(TaskNode systemTaskNode, in DataView particleDataView, GraphTraverser traverser, VfxGraphLegacyParticleSystemContainer particleSystemContainer, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem)
        {
            foreach (var taskNode in traverser.TraverseTaskDownwards(systemTaskNode))
            {
                if (!taskNode.DataBindings.FindDataView(particleDataView.Id).HasValue)
                {
                    continue;
                }

                if (taskNode.Task is TemplatedTask templatedTask)
                {
                    UnityEngine.VFX.VFXTaskType taskType = UnityEngine.VFX.VFXTaskType.None;
                    switch (templatedTask.TemplateName)
                    {
                        case "Init":
                            taskType = UnityEngine.VFX.VFXTaskType.Initialize;

                            foreach (var dataNode in taskNode.DataNodes)
                            {
                                var rootDataView = dataNode.DataContainer.RootDataView;
                                if (rootDataView.DataDescription is ParticleData && !rootDataView.Id.Equals(particleDataView.Id))
                                {
                                    particleSystem.Parent = particleSystemContainer.Find(rootDataView.Id);
                                }
                            }

                            break;
                        case "Update":
                            taskType = UnityEngine.VFX.VFXTaskType.Update;
                            break;
                        case "Output":
                            taskType = UnityEngine.VFX.VFXTaskType.ParticleQuadOutput;
                            break;
                        default:
                            continue;
                    }
                    var task = new VfxGraphLegacyParticleSystemContainer.Task(taskNode.Name ?? templatedTask.TemplateName, taskNode.Id, taskType);
                    particleSystem.Tasks.Add(task);
                }

                if (taskNode.Task is PlaceholderSystemTask)
                {
                    var task = new VfxGraphLegacyParticleSystemContainer.Task("System", taskNode.Id, UnityEngine.VFX.VFXTaskType.None);
                    particleSystem.SetSystemTask(task);
                }
            }
        }

        void GenerateDeadList(TaskNode systemTaskNode, in DataView particleDataView, AttributeSetLayoutCompilationData layoutCompilationData, IMutableGraph graph)
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
                    }
                }
            }
        }
    }

    class VfxGraphLegacyParticleSystemContainer : IEnumerable, IEnumerable<VfxGraphLegacyParticleSystemContainer.ParticleSystem>
    {
        readonly Dictionary<DataViewId, ParticleSystem> m_ParticleSystems = new();

        public int Count => m_ParticleSystems.Count;

        public void Add(DataViewId particleDataViewId, ParticleSystem particleSystem) => m_ParticleSystems.Add(particleDataViewId, particleSystem);

        public ParticleSystem Find(DataViewId particleDataViewId)
        {
            m_ParticleSystems.TryGetValue(particleDataViewId, out var particleSystem);
            return particleSystem;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<ParticleSystem> IEnumerable<ParticleSystem>.GetEnumerator() => GetEnumerator();
        public Dictionary<DataViewId, ParticleSystem>.ValueCollection.Enumerator GetEnumerator() => m_ParticleSystems.Values.GetEnumerator();

        public class ParticleSystem
        {
            public string Name { get; }
            public uint Capacity { get; set; }
            public List<Task> Tasks { get; } = new();

            public Task SystemTask { get; private set; }

            public ParticleSystem Parent { get; set; }

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
            public UnityEngine.VFX.VFXTaskType TaskType { get; }

            public Task(string name, TaskNodeId id, UnityEngine.VFX.VFXTaskType taskType)
            {
                Name = name;
                Id = id;
                TaskType = taskType;
            }
        }
    }
}

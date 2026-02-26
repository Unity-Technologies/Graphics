using Unity.GraphCommon.LowLevel.Editor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VfxGraphLegacyParticleSystemPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            var particleSystemContainer = context.data.GetOrCreate<VfxGraphLegacyParticleSystemContainer>();
            var traverser = context.graph.CreateTraverser();

            List<DataViewId> particleDataViewIds = new(); //TODO: Temp

            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is PlaceholderSystemTask systemTask)
                {
                    var particleDataView = taskNode.DataBindings[0].DataView;
                    if (particleDataView.DataDescription is ParticleData particleData)
                    {
                        var particleSystem = new VfxGraphLegacyParticleSystemContainer.ParticleSystem($"ParticleSystem{particleSystemContainer.Count + 1}");
                        particleSystem.Capacity = particleData.Capacity;
                        CollectTasks(traverser, taskNode, particleSystem);
                        particleSystemContainer.Add(particleSystem);

                        if (particleDataView.FindSubData(ParticleData.AttributeDataKey, out var attributeDataView))
                        {
                            var attributeData = attributeDataView.DataDescription as AttributeData;
                            var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();
                            var layout = layoutCompilationData[attributeData];
                            if (layout.ContainsAttribute(VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.Alive)))
                            {
                                particleDataViewIds.Add(particleDataView.Id); // TODO: Temp, get deadlist subdata here directly, when data change does not invalidate task iterators
                            }
                        }
                    }
                }
            }
            // TODO: Temp
            foreach(var particleDataViewId in particleDataViewIds)
            {
                var deadListDataId = context.graph.GetSubdata(particleDataViewId, ParticleData.DeadlistKey);
                if(deadListDataId.IsValid)
                    context.graph.OverrideDataDescription(deadListDataId, new DeadListData());
            }
            return true;
        }

        void CollectTasks(GraphTraverser traverser, TaskNode rootTaskNode, VfxGraphLegacyParticleSystemContainer.ParticleSystem particleSystem)
        {
            foreach (var taskNode in traverser.TraverseTaskDownwards(rootTaskNode))
            {
                if (taskNode.Task is TemplatedTask templatedTask)
                {
                    UnityEngine.VFX.VFXTaskType taskType = UnityEngine.VFX.VFXTaskType.None;
                    switch (templatedTask.TemplateName)
                    {
                        case "Init":
                            taskType = UnityEngine.VFX.VFXTaskType.Initialize;
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
                    var task = new VfxGraphLegacyParticleSystemContainer.Task(templatedTask.TemplateName, taskNode.Id, taskType);
                    particleSystem.Tasks.Add(task);
                }

                if (taskNode.Task is PlaceholderSystemTask)
                {
                    var task = new VfxGraphLegacyParticleSystemContainer.Task("System", taskNode.Id, UnityEngine.VFX.VFXTaskType.None);
                    particleSystem.SetSystemTask(task);
                }
            }
        }
    }

    class VfxGraphLegacyParticleSystemContainer : IEnumerable, IEnumerable<VfxGraphLegacyParticleSystemContainer.ParticleSystem>
    {
        readonly List<ParticleSystem> m_ParticleSystems = new();

        public int Count => m_ParticleSystems.Count;

        public void Add(ParticleSystem particleSystem)
        {
            m_ParticleSystems.Add(particleSystem);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<ParticleSystem> IEnumerable<ParticleSystem>.GetEnumerator() => GetEnumerator();
        public List<ParticleSystem>.Enumerator GetEnumerator() => m_ParticleSystems.GetEnumerator();

        public class ParticleSystem
        {
            public string Name { get; }
            public uint Capacity { get; set; }
            public List<Task> Tasks { get; } = new();

            public Task SystemTask { get; private set; }

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

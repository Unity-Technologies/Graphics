using System.Collections;
using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Groups the spawner tasks into systems, one per event data view, mirroring what
    /// <see cref="ParticleSystemPass"/> does for particle systems. Collecting them up front
    /// means the output pass no longer has to rediscover the grouping after the tasks have been lowered.
    /// </summary>
    class SpawnerSystemPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            var spawnerSystemContainer = context.data.GetOrCreate<SpawnerSystemContainer>();

            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is not SpawnerTask spawnerTask)
                    continue;

                var eventDataBinding = taskNode.DataBindings[spawnerTask.SpawnDataKey];
                if (!eventDataBinding.HasValue)
                    continue;

                var eventDataViewId = eventDataBinding.Value.DataView.Id;
                var spawnerSystem = spawnerSystemContainer.Find(eventDataViewId);
                if (spawnerSystem == null)
                {
                    spawnerSystem = new SpawnerSystemContainer.SpawnerSystem(eventDataViewId);
                    spawnerSystemContainer.Add(eventDataViewId, spawnerSystem);
                }

                spawnerSystem.Tasks.Add(new SpawnerSystemContainer.Task(
                    taskNode.Id, (UnityEngine.VFX.VFXTaskType)spawnerTask.SpawnerType));
            }

            return true;
        }
    }

    class SpawnerSystemContainer : IEnumerable, IEnumerable<SpawnerSystemContainer.SpawnerSystem>
    {
        readonly Dictionary<DataViewId, SpawnerSystem> m_SpawnerSystems = new();

        public int Count => m_SpawnerSystems.Count;

        public void Add(DataViewId eventDataViewId, SpawnerSystem spawnerSystem)
            => m_SpawnerSystems.Add(eventDataViewId, spawnerSystem);

        public SpawnerSystem Find(DataViewId eventDataViewId)
        {
            m_SpawnerSystems.TryGetValue(eventDataViewId, out var spawnerSystem);
            return spawnerSystem;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<SpawnerSystem> IEnumerable<SpawnerSystem>.GetEnumerator() => GetEnumerator();
        public Dictionary<DataViewId, SpawnerSystem>.ValueCollection.Enumerator GetEnumerator() => m_SpawnerSystems.Values.GetEnumerator();

        public class SpawnerSystem
        {
            /// <summary>
            /// Repointed at the concrete spawn buffer by <see cref="SpawnerTaskLowering"/>,
            /// once the event data has been lowered.
            /// </summary>
            public DataViewId EventDataViewId { get; set; }

            public List<Task> Tasks { get; } = new();

            public SpawnerSystem(DataViewId eventDataViewId) => EventDataViewId = eventDataViewId;
        }

        public struct Task
        {
            public TaskNodeId Id { get; }
            public UnityEngine.VFX.VFXTaskType TaskType { get; }

            public Task(TaskNodeId id, UnityEngine.VFX.VFXTaskType taskType)
            {
                Id = id;
                TaskType = taskType;
            }
        }
    }
}

using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Lowers the spawner tasks. Every buffer a spawner touches is its own output, so they all bind under
    /// the single name the runtime reads a spawner's output by.
    /// </summary>
    class SpawnerToVfxTaskPass : TaskReplacementPass
    {
        static readonly NameDataKey k_SpawnerOutputKey = new("spawner_output");

        protected override bool IsTargetTask(ITask task) => task is SpawnerTask;

        protected override IDataKey GetConcreteBindingKey(DataView dataView) => k_SpawnerOutputKey;

        protected override void PatchSystemContainer(
            Dictionary<TaskNodeId, TaskNodeId> replacements,
            ref CompilationContext context)
        {
            var spawnerSystemContainer = context.data.Get<SpawnerSystemContainer>();
            foreach (var spawnerSystem in spawnerSystemContainer)
            {
                for (int i = 0; i < spawnerSystem.Tasks.Count; i++)
                {
                    var task = spawnerSystem.Tasks[i];
                    if (replacements.TryGetValue(task.Id, out var replacementId))
                        spawnerSystem.Tasks[i] = new SpawnerSystemContainer.Task(replacementId, task.TaskType);
                }

                // The system is identified by its event data view, which has just been lowered to a buffer.
                if (m_DataConcretizationResult.TryGetConcreteDataView(spawnerSystem.EventDataViewId, out var concreteViewId))
                    spawnerSystem.EventDataViewId = concreteViewId;
            }
        }
    }
}

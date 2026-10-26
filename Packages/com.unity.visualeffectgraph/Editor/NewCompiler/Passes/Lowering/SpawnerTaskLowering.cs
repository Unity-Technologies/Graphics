using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Lowers the spawner tasks. Every buffer a spawner touches is its own output, so they all bind under
    /// the single name the runtime reads a spawner's output by.
    /// </summary>
    class SpawnerTaskLowering : TaskLoweringPass.TaskLowering<SpawnerTask>
    {
        static readonly NameDataKey k_SpawnerOutputKey = new("spawner_output");

        public override IDataKey GetConcreteBindingKey(SpawnerTask task, IDataKey logicalBindingKey, DataView dataView)
            => k_SpawnerOutputKey;

        public override void ConfigureTask(SpawnerTask oldTask, VfxTask newTask)
        {
            // A spawner's kind rides its SpawnerSystemContainer entry, and the runtime implements it, so
            // there is no generated code to point at either.
            newTask.HasShaderSource = false;
        }

        public override void OnTaskLowered(TaskNodeId oldId, TaskNodeId newId, ref CompilationContext context)
        {
            var spawnerSystemContainer = context.data.Get<SpawnerSystemContainer>();
            foreach (var spawnerSystem in spawnerSystemContainer)
            {
                for (int i = 0; i < spawnerSystem.Tasks.Count; i++)
                {
                    var task = spawnerSystem.Tasks[i];
                    if (task.Id.Equals(oldId))
                        spawnerSystem.Tasks[i] = new SpawnerSystemContainer.Task(newId, task.TaskType);
                }
            }
        }

        public override void OnLoweringComplete(ref CompilationContext context)
        {
            var spawnerSystemContainer = context.data.Get<SpawnerSystemContainer>();
            var concretization = context.data.Get<DataConcretizationResult>();

            // The system is identified by its event data view, which has been lowered to a buffer.
            foreach (var spawnerSystem in spawnerSystemContainer)
            {
                if (concretization.TryGetConcreteDataView(spawnerSystem.EventDataViewId, out var concreteViewId))
                    spawnerSystem.EventDataViewId = concreteViewId;
            }
        }
    }
}

using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class SortTaskLowering : TaskLoweringPass.TaskLowering<SortTask>
    {
        public override IDataKey GetConcreteBindingKey(SortTask task, IDataKey logicalBindingKey, DataView dataView)
            => dataView.DataDescription switch
            {
                SortingData => SortTask.BufferKeys.SourceBufferKey,
                SortScratchData => SortTask.BufferKeys.ScratchBufferKey,
                DrawIndirectionData => SortTask.BufferKeys.DestinationBufferKey,
                _ => null,
            };

        public override void ConfigureTask(SortTask oldTask, VfxTask newTask)
        {
            newTask.TaskType = UnityEngine.VFX.VFXTaskType.PerOutputSort;
            newTask.HasShaderSource = false;
            newTask.AddParameterValue(SortTask.ParameterKeys.GlobalSortKey, 0);
            newTask.AddParameterValue(SortTask.ParameterKeys.PerCameraSortKey, oldTask.IsPerCamera ? 1 : 0);
        }
    }
}

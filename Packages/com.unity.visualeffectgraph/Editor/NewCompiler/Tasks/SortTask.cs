using Unity.GraphCommon.LowLevel.Editor;
using EntityId = UnityEngine.EntityId;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Sorts the keys an output update registered into the indirection buffer the output draws through.
    /// The runtime implements the sort itself, so the task carries no generated code: it is described
    /// entirely by the buffers it is bound to and by the sorting mode it asks for.
    /// </summary>
    class SortTask : ITask
    {
        public static readonly IDataKey RenderingDataBindingKey = new UniqueDataKey("RenderingDataBinding");

        public static class BufferKeys
        {
            public static readonly NameDataKey SourceBufferKey = new("srcBuffer");
            public static readonly NameDataKey ScratchBufferKey = new("scratchBuffer");
            public static readonly NameDataKey DestinationBufferKey = new("dstBuffer");
        }

        public static class ParameterKeys
        {
            public static readonly NameDataKey GlobalSortKey = new("globalSort");
            public static readonly NameDataKey PerCameraSortKey = new("isPerCameraSort");
        }

        /// <summary>
        /// The sort's buffers in the order the runtime reads them, which is by position rather than by
        /// name. Only <see cref="OutputPass"/> needs this, to work around that.
        /// </summary>
        public static readonly IDataKey[] OrderedBufferKeys =
        {
            BufferKeys.SourceBufferKey,
            BufferKeys.ScratchBufferKey,
            BufferKeys.DestinationBufferKey,
        };

        public bool IsPerCamera { get; }

        public SortTask(bool isPerCamera)
        {
            IsPerCamera = isPerCamera;
        }

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            if (dataKey != RenderingDataBindingKey)
                return BindingUsage.Unknown;

            readUsage?.Add(DataPath.Root + RenderingData.SortingData);
            writeUsage?.Add(DataPath.Root + RenderingData.SortingData + SortingData.ScratchBufferKey);
            writeUsage?.Add(DataPath.Root + RenderingData.IndirectBuffer);

            return BindingUsage.ReadWrite;
        }
    }
}

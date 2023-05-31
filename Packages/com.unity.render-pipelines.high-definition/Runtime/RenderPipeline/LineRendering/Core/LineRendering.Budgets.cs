namespace UnityEngine.Rendering
{
    partial class LineRendering
    {
        internal static class Budgets
        {
            internal const int ByteSizeBinRecordPool = 256 * 1024 * 1024;
            internal const int ByteSizeBinRecordFormat = 4 + 4 + 4;
            internal static int BinRecordCount => (int)Mathf.Ceil(ByteSizeBinRecordPool / ByteSizeBinRecordFormat);

            internal const int ByteSizeWorkQueuePool = 256 * 1024 * 1024;
            internal const int ByteSizeWorkQueueFormat = 4;
            internal static int WorkQueueCount => (int)Mathf.Ceil(ByteSizeWorkQueuePool / ByteSizeWorkQueueFormat);

            internal const int TileSizeBin = 8;
        }
    }
}

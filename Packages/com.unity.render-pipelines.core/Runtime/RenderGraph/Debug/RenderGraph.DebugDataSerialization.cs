namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        internal static class DebugDataSerialization
        {
            public static string ToJson(DebugData debugData)
            {
                return debugData != null ? JsonUtility.ToJson(debugData, prettyPrint: false) : string.Empty;
            }

            public static DebugData FromJson(string json)
            {
                return !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<DebugData>(json) : null;
            }
        }
    }
}

using UnityEngine;

namespace UnityEditor.PathTracing.LightBakerBridge
{
    // This must be in its own file, otherwise the associated ScriptedImporter will malfunction.
    internal class BakeImport : ScriptableObject
    {
        public string BakeInputPath;
        public string LightmapRequestsPath;
        public string LightProbeRequestsPath;
        public string BakeOutputFolderPath;
        public int ProgressPort;
    }
}

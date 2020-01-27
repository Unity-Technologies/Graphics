using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    public struct GlobalXRSettings
    {
        internal static GlobalXRSettings NewDefault() => new GlobalXRSettings()
        {
            singlePass = true,
            occlusionMesh = true
        };

        public bool singlePass;
        public bool occlusionMesh;
    }
}

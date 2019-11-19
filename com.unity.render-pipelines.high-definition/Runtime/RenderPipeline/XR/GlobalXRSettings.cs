using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    public struct GlobalXRSettings
    {
        /// <summary>Default GlobalXRSettings</summary>
        public static readonly GlobalXRSettings @default = new GlobalXRSettings()
        {
            singlePass = true,
            occlusionMesh = true
        };

        public bool singlePass;
        public bool occlusionMesh;
    }
}

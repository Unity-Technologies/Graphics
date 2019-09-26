using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    public struct GlobalXRSettings
    {
        /// <summary>Default GlobalXRSettings</summary>
        public static readonly GlobalXRSettings @default = new GlobalXRSettings()
        {
            occlusionMesh = true
        };

        public bool occlusionMesh;
    }
}

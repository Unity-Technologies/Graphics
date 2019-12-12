using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    public struct GlobalXRSettings
    {
        /// <summary>Default GlobalXRSettings</summary>
        [Obsolete("Since 2019.3, use GlobalXRSettings.NewDefault() instead.")]
        public static readonly GlobalXRSettings @default = default;
        /// <summary>Default GlobalXRSettings</summary>
        public static GlobalXRSettings NewDefault() => new GlobalXRSettings()
        {
            singlePass = true,
            occlusionMesh = true
        };

        public bool singlePass;
        public bool occlusionMesh;
    }
}

using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Global Decal Settings.
    /// </summary>
    [Serializable]
    public struct GlobalDecalSettings
    {
        internal static GlobalDecalSettings NewDefault() => new GlobalDecalSettings()
        {
            drawDistance = 1000,
            atlasWidth = 4096,
            atlasHeight = 4096
        };

        /// <summary>Maximum draw distance.</summary>
        public int drawDistance;
        /// <summary>Decal atlas width in pixels.</summary>
        public int atlasWidth;
        /// <summary>Decal atlas height in pixels.</summary>
        public int atlasHeight;
        /// <summary>Enables per channel mask.</summary>
        public bool perChannelMask;
    }
}

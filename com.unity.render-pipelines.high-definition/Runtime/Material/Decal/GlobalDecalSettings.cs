using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public struct GlobalDecalSettings
    {
        internal static GlobalDecalSettings NewDefault() => new GlobalDecalSettings()
        {
            drawDistance = 1000,
            atlasWidth = 4096,
            atlasHeight = 4096
        };

        public int drawDistance;
        public int atlasWidth;
        public int atlasHeight;
        public bool perChannelMask;
    }
}

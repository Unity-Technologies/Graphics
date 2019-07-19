using System;

namespace UnityEditor.Rendering.HighDefinition
{
    public struct LODFadeSettings
    {
        public bool Enabled;
        public bool SpeedTreeMode;

        public static readonly LODFadeSettings Default = new LODFadeSettings()
        {
            Enabled = true,
            SpeedTreeMode = false,
        };
    }
}

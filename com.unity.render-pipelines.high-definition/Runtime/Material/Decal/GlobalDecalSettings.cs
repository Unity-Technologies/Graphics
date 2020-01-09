using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public struct GlobalDecalSettings
    {
        /// <summary>Default GlobalDecalSettings</summary>
        [Obsolete("Since 2019.3, use GlobalDecalSettings.NewDefault() instead.")]
        public static readonly GlobalDecalSettings @default = default;
        /// <summary>Default GlobalDecalSettings</summary>
        public static GlobalDecalSettings NewDefault() => new GlobalDecalSettings()
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

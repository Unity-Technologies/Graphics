using System;
#if UNITY_EDITOR
// TODO @ SHADERS: Enable as many of the rules (currently commented out) as make sense
//                 once the setting asset aggregation behavior is finalized.  More fine tuning
//                 of these rules is also desirable (current rules have been interpreted from
//                 the variant stripping logic)
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Global Decal Settings.
    /// </summary>
    [Serializable]
    public struct GlobalDecalSettings
    {
        internal const int k_DefaultAtlasSize = 4096;

        internal static GlobalDecalSettings NewDefault() => new GlobalDecalSettings()
        {
            drawDistance = 1000,
            atlasWidth = k_DefaultAtlasSize,
            atlasHeight = k_DefaultAtlasSize,
            transparentTextureResolution = new IntScalableSetting(new[] { 256, 512, 1024 }, ScalableSettingSchemaId.With3Levels)
        };

        /// <summary>Maximum draw distance.</summary>
        public int drawDistance;
        /// <summary>Decal atlas width in pixels.</summary>
        public int atlasWidth;
        /// <summary>Decal atlas height in pixels.</summary>
        public int atlasHeight;
        /// <summary>Resolution of textures in the decal atlas for shader graphs that affect transparent materials</summary>
        public IntScalableSetting transparentTextureResolution;
        /// <summary>Enables per channel mask.</summary>
#if UNITY_EDITOR // multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
        // Coarse control of decal variants is based on RenderPipelineSettings.SupportDecals
        // This setting only handles the fine tuning if decals are enabled
        // [ShaderKeywordFilter.RemoveIf(false, keywordNames: "DECALS_4RT")]
        // [ShaderKeywordFilter.RemoveIf(true, keywordNames: "DECALS_3RT")]
#endif
        public bool perChannelMask;
    }
}

using System;

namespace UnityEngine.Rendering.Universal
{
    #region Material Settings
    [GenerateHLSL]
    public enum DebugMaterialMode
    {
        None,
        Albedo,
        Specular,
        Alpha,
        Smoothness,
        AmbientOcclusion,
        Emission,
        NormalWorldSpace,
        NormalTangentSpace,
        LightingComplexity,
        Metallic,
        SpriteMask,
    }

    [GenerateHLSL]
    public enum DebugVertexAttributeMode
    {
        None,
        Texcoord0,
        Texcoord1,
        Texcoord2,
        Texcoord3,
        Color,
        Tangent,
        Normal,
    }

    [GenerateHLSL]
    public enum DebugMaterialValidationMode
    {
        None,
        Albedo,
        Metallic
    }

    #endregion

    #region Rendering Settings
    [GenerateHLSL]
    public enum DebugFullScreenMode
    {
        None,
        Depth,

        // TODO: Restore this once we have access to the screen-space shadow texture...
        //MainLightShadowsOnly,
        AdditionalLightsShadowMap,
        MainLightShadowMap,
    }

    [GenerateHLSL]
    public enum DebugSceneOverrideMode
    {
        None,
        Overdraw,
        Wireframe,
        SolidWireframe,
        ShadedWireframe,
    }

    [GenerateHLSL]
    public enum DebugMipInfoMode
    {
        None,
        Level,
        Count,
        Ratio
    }

    [GenerateHLSL]
    public enum DebugPostProcessingMode
    {
        Disabled,
        Auto,
        Enabled
    };

    [GenerateHLSL]
    public enum DebugValidationMode
    {
        None,
        [InspectorName("Highlight NaN, Inf and Negative Values")]
        HighlightNanInfNegative,
        [InspectorName("Highlight Values Outside Range")]
        HighlightOutsideOfRange
    }

    [GenerateHLSL]
    public enum PixelValidationChannels
    {
        RGB,
        R,
        G,
        B,
        A
    }
    #endregion

    #region Lighting settings
    [GenerateHLSL]
    public enum DebugLightingMode
    {
        None,
        ShadowCascades,
        LightingWithoutNormalMaps,
        LightingWithNormalMaps,
        Reflections,
        ReflectionsWithSmoothness,
    }

    [GenerateHLSL, Flags]
    public enum DebugLightingFeatureFlags
    {
        None,
        GlobalIllumination = 0x1,
        MainLight = 0x2,
        AdditionalLights = 0x4,
        VertexLighting = 0x8,
        Emission = 0x10,
        AmbientOcclusion = 0x20,
    }
    #endregion
}

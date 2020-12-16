
using System;

namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL]
    public enum DebugMaterialIndex
    {
        None,
        Unlit,
        Diffuse,
        Specular,
        Alpha,
        Smoothness,
        AmbientOcclusion,
        Emission,
        NormalWorldSpace,
        NormalTangentSpace,
        LightingComplexity,
        LOD,
        Metallic,
    }

    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,
        Depth,

        // TODO: Restore this once we have access to the screen-space shadow texture...
        //MainLightShadowsOnly,
        AdditionalLightsShadowMap,
        MainLightShadowMap,
    }

    [GenerateHLSL]
    public enum SceneOverrides
    {
        None,
        Overdraw,
        Wireframe,
        SolidWireframe,
    }

    [GenerateHLSL]
    public enum LightingDebugMode
    {
        None,
        ShadowCascades,
        LightOnly,
        LightDetail,
        Reflections,
        ReflectionsWithSmoothness,
    }

    [GenerateHLSL]
    public enum VertexAttributeDebugMode
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

    [GenerateHLSL, Flags]
    public enum DebugLightingFeature
    {
        GlobalIllumination = 1 << 0,
        MainLight = 1 << 1,
        AdditionalLights = 1 << 2,
        VertexLighting = 1 << 3,
        Emission = 1 << 4,
        AmbientOcclusion = 1 << 5,
    }

    [GenerateHLSL]
    public enum DebugValidationMode
    {
        None,
        HighlightNanInfNegative,
        HighlightOutsideOfRange,
        ValidateAlbedo,
    }

    [GenerateHLSL]
    public enum DebugMipInfo
    {
        None,
        Level,
        Count,

        //CountReduction,
        //StreamingMipBudget,
        //StreamingMip,
    }
}

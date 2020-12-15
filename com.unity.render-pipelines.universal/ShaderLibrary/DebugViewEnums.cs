
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
        None,
        GlobalIllumination = 0x1,
        MainLight = 0x2,
        AdditionalLights = 0x4,
        VertexLighting = 0x8,
        Emission = 0x10,
        AmbientOcclusion = 0x20,
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

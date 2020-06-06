using System;

namespace UnityEngine.Rendering.Universal
{
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

    public enum DebugReplacementPassType
    {
        None,
        Overdraw,
        Wireframe,
        SolidWireframe,
        Attributes,
    }

    public enum LightingDebugMode
    {
        None,
        ShadowCascades,
        LightOnly,
        LightDetail,
        Reflections,
        ReflectionsWithSmoothness,
    }

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

    [Flags]
    public enum PBRLightingDebugMode
    {
        None,
        GI = 0x1,
        PBRLight = 0x2,
        AdditionalLights = 0x4,
        VertexLighting = 0x8,
        Emission = 0x10,
    }

    public enum DebugValidationMode
    {
        None,
        HiglightNanInfNegative,
        HighlightOutsideOfRange,
        ValidateAlbedo,
    }

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


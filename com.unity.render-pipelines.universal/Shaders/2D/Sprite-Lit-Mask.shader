Shader "Universal Render Pipeline/2D/Sprite-Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Cutoff ("Mask alpha cutoff", Range(0.0, 1.0)) = 0.0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend Off
        ColorMask 0

        Pass
        {
            Tags{ "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex MaskRenderingVertex
            #pragma fragment MaskRenderingFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SpriteMaskShared.hlsl"
            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "NormalsRendering" }
            HLSLPROGRAM
            #pragma vertex MaskRenderingVertex
            #pragma fragment MaskRenderingFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SpriteMaskShared.hlsl"
            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex MaskRenderingVertex
            #pragma fragment MaskRenderingFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SpriteMaskShared.hlsl"
            ENDHLSL
        }

    }
}

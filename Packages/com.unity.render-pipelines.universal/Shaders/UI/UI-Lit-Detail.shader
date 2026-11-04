Shader "UI/Lit/Detail"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Specular ("Specular Color", Color) = (0,0,0,0)
        _MainTex ("Diffuse (RGB), Alpha (A)", 2D) = "white" {}
        _MainBump ("Diffuse Bump Map", 2D) = "bump" {}
        _DetailTex ("Detail (RGB)", 2D) = "white" {}
        _DetailBump ("Detail Bump Map", 2D) = "bump" {}
        _Strength ("Detail Strength", Range(0.0, 1.0)) = 0.2
        [PowerSlider(5.0)] _Shininess ("Shininess", Range(0.01, 1.0)) = 0.2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex UILitVertex
            #pragma fragment Fragment

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_FALLOFF_QUADRATIC
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UI/UILit.hlsl"

            half4 Fragment(UILitVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy) + _TextureSampleAdd;
                half4 detail = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, input.detailUV.xy);
                half4 nmMain = SAMPLE_TEXTURE2D(_MainBump, sampler_MainBump, input.uv.xy);
                half4 nmDetail = SAMPLE_TEXTURE2D(_DetailBump, sampler_DetailBump, input.detailUV.xy);
                half3 normalTS = UnpackNormal((nmMain + nmDetail) * 0.5);
                float3 normalWS = UILitTangentToWorld(input, normalTS);

                col.rgb = lerp(col.rgb, col.rgb * detail.rgb, detail.a * _Strength);
                col *= input.color;

                half3 color = UILitShade(input, col.rgb, _Specular.a, _Shininess, normalWS);
                return UILitOutput(input, color, col.a);
            }
            ENDHLSL
        }
    }

    Fallback "UI/Lit/Transparent"
}

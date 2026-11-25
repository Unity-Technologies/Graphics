Shader "UI/Lit/Refraction"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Specular ("Specular Color", Color) = (0,0,0,0)
        _MainTex ("Diffuse (RGB), Alpha (A)", 2D) = "white" {}
        _MainBump ("Diffuse Bump Map", 2D) = "bump" {}
        _Mask ("Mask (Specularity, Shininess, Refraction)", 2D) = "black" {}
        [PowerSlider(5.0)] _Shininess ("Shininess", Range(0.01, 1.0)) = 0.2
        _Focus ("Focus", Range(-100.0, 100.0)) = -100.0

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
        LOD 400

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
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_MainBump, sampler_MainBump, input.uv.zw));
                half3 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv.xy).rgb;

                // Replaces the Built-in GrabPass with the URP opaque texture
                col.rgb = UILitApplyRefraction(input, col.rgb, normalTS, mask.b);
                col *= input.color;

                float3 normalWS = UILitTangentToWorld(input, normalTS);
                half3 color = UILitShade(input, col.rgb, _Specular.a * mask.r, _Shininess * mask.g, normalWS);
                return UILitOutput(input, color, col.a);
            }
            ENDHLSL
        }
    }

    Fallback "UI/Lit/Transparent"
}

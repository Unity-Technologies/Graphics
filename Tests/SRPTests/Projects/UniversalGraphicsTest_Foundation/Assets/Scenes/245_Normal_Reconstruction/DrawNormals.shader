Shader "Hidden/Universal Render Pipeline/DrawNormals"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "DrawNormals"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile _DRAW_NORMALS_TAP1 _DRAW_NORMALS_TAP3 _DRAW_NORMALS_TAP5 _DRAW_NORMALS_TAP9
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_DRAW_NORMALS_TAP9)
                half3 normalWS = ReconstructNormalTap9(input.positionCS.xy * _ScaleBias.xy + _ScaleBias.zw);
#elif defined(_DRAW_NORMALS_TAP5)
                half3 normalWS = ReconstructNormalTap5(input.positionCS.xy * _ScaleBias.xy + _ScaleBias.zw);
#elif defined(_DRAW_NORMALS_TAP3)
                half3 normalWS = ReconstructNormalTap3(input.positionCS.xy * _ScaleBias.xy + _ScaleBias.zw);
#else
                half3 normalWS = ReconstructNormalDerivative(input.positionCS.xy * _ScaleBias.xy + _ScaleBias.zw);
#endif

                // Add subtle lines between the test cases.
                if(any(int2(input.positionCS.xy) == int2(_ScreenSize.xy)/2))
                    return half4((normalWS * 0.5 + 0.5) * 0.75, 1);

                return half4(normalWS * 0.5 + 0.5, 1);
            }
            ENDHLSL
        }
    }
}

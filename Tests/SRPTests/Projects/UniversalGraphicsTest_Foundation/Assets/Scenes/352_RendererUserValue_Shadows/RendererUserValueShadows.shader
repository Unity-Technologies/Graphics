// Scales vertices by the renderer user value (unity_RendererUserValue, as float bits) in
// both the Forward and ShadowCaster passes, so a stale value in either pass shows up as a
// shadow/sphere size mismatch. No multi_compile_instancing on purpose: instancing would
// supply correct per-instance values and mask the regression this test covers.
//
// Single-pass instanced stereo (XR) is still handled explicitly in the Forward pass: XR
// injects STEREO_INSTANCING_ON, which routes UNITY_MATRIX_M through the per-instance
// unity_ObjectToWorldArray. Without UNITY_SETUP_INSTANCE_ID that array read is undefined and
// the geometry collapses off-screen (spheres vanish, shadows survive since the ShadowCaster
// pass renders mono). This stereo setup does NOT enable UNITY_INSTANCED_RENDERING_LAYER, so
// unity_RendererUserValue still resolves to the non-instanced UnityPerDraw value under test.
Shader "Test/RendererUserValueShadows"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Declared outside UnityPerMaterial: SRP Batcher compatibility is decided per subshader,
        // so this alone forces every pass through the non-batcher render loops under test.
        float4 _BaseColor;

        // Each sphere is initialized by the shared
        // Assets/Scenes/352_RendererUserValue/SetUserValue.cs component, which sets
        // Renderer.SetShaderUserValue to the raw bits of a float scale.
        // 0 (unset) is treated as scale 1.
        float3 ScaleByUserValue(float3 positionOS)
        {
            float scale = asfloat(unity_RendererUserValue);
            return positionOS * (scale == 0.0 ? 1.0 : scale);
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(ScaleByUserValue(input.positionOS.xyz));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(_BaseColor.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer,
            // same as in com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(ScaleByUserValue(input.positionOS.xyz));
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                Varyings output;
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}

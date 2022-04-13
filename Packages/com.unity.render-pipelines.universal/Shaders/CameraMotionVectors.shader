Shader "Hidden/kMotion/CameraMotionVectors"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite On
            ZTest Always

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        #if defined(USING_STEREO_MATRICES)
        float4x4 _PrevViewProjMStereo[2];
#define _PrevViewProjM _PrevViewProjMStereo[unity_StereoEyeIndex]
#else
#define  _PrevViewProjM _PrevViewProjMatrix
#endif

            // -------------------------------------
            // Structs
            struct Attributes
            {
                uint vertexID   : SV_VertexID;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
            };

            // -------------------------------------
            // Vertex
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // -------------------------------------
            // Fragment
            half4 frag(Varyings input, out float outDepth : SV_Depth) : SV_Target
            {
                // Calculate PositionInputs
                half depth = LoadSceneDepth(input.position.xy).x;
                outDepth = depth;
                half2 screenSize = _ScreenSize.zw;
                PositionInputs positionInputs = GetPositionInput(input.position.xy, screenSize, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

                // Calculate positions
                float4 previousPositionVP = mul(_PrevViewProjM, float4(positionInputs.positionWS, 1.0));
                float4 positionVP = mul(UNITY_MATRIX_VP, float4(positionInputs.positionWS, 1.0));

                previousPositionVP.xy *= rcp(previousPositionVP.w);
                positionVP.xy *= rcp(positionVP.w);

                // Calculate velocity
                float2 velocity = (positionVP.xy - previousPositionVP.xy);
                #if UNITY_UV_STARTS_AT_TOP
                    velocity.y = -velocity.y;
                #endif

                // Convert velocity from Clip space (-1..1) to NDC 0..1 space
                // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
                // Note: ((positionVP * 0.5 + 0.5) - (previousPositionVP * 0.5 + 0.5)) = (velocity * 0.5)
                return half4(velocity.xy * 0.5, 0, 0);
            }

            ENDHLSL
        }
    }
}

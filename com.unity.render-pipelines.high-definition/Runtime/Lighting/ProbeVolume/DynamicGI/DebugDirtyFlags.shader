Shader "Hidden/Debug/DebugDirtyFlags"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "Transparent" }
        ZWrite On
        Cull Front

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "Forward" }

            HLSLPROGRAM

            #pragma editor_sync_compilation

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbePropagationGlobals.hlsl"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float4x4 _ProbeIndex3DToPositionWSMatrix;
            float _ProbeVolumeProbeDisplayRadiusWS;
            StructuredBuffer<int> _ProbeVolumeDirtyFlags;

            v2f vert(appdata v)
            {
                v2f o;

                uint probeIndex = v.vertexID / 6u;
                uint probeTriangleIndex = (v.vertexID / 3u) & 1u;
                uint probeVertexIndex = v.vertexID - probeIndex * 6u - probeTriangleIndex * 3u;

                uint3 probeCoordinate = ProbeIndexToProbeCoordinates(probeIndex);
                bool dirty = IsProbeDirty(_ProbeVolumeDirtyFlags, ProbeCoordinateToPaddedProbeIndex(probeCoordinate));

                float2 vertexPositionOS = (probeTriangleIndex == 1u)
                    ? float2((probeVertexIndex & 1u), saturate(probeVertexIndex))
                    : float2(saturate(probeVertexIndex), saturate((float)probeVertexIndex - 1.0));
                o.uv = vertexPositionOS;
                vertexPositionOS = vertexPositionOS * 2.0 - 1.0;
                vertexPositionOS *= _ProbeVolumeProbeDisplayRadiusWS;

                float3 probeOriginWS = mul(_ProbeIndex3DToPositionWSMatrix, float4(probeCoordinate, 1.0)).xyz;
                float3 probeOriginRWS = GetCameraRelativePositionWS(probeOriginWS);
                
                float3 cameraRightWS = mul(float4(1.0, 0.0, 0.0, 0.0), UNITY_MATRIX_V).xyz;
                float3 cameraUpWS = mul(float4(0.0, 1.0, 0.0, 0.0), UNITY_MATRIX_V).xyz;

                float3 positionRWS = (cameraRightWS * vertexPositionOS.x + cameraUpWS * vertexPositionOS.y) + probeOriginRWS;

                o.color = dirty ? float3(1, 0.5, 0) : float3(0.5, 0, 0);
                o.positionCS = TransformWorldToHClip(positionRWS);

                return o;
            }

            void ClipProbeSphere(float2 uv)
            {
                float2 positionProbeCard = uv * 2.0 - 1.0;
                clip(dot(positionProbeCard, positionProbeCard) < 1.0 ? 1.0 : -1.0);
            }

            float4 frag(v2f i) : SV_Target
            {
                ClipProbeSphere(i.uv);
                return float4(i.color, 1.0);
            }

            ENDHLSL
        }
    }
}

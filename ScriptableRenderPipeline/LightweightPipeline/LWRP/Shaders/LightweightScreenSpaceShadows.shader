Shader "Hidden/LightweightPipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags {}

        Pass
        {
            ZTest Always ZWrite Off

            HLSLPROGRAM
            #pragma vertex   Vertex
            #pragma fragment Fragment

            #include "LWRP/ShaderLibrary/Core.hlsl"

            
            #define MAX_SHADOW_CASCADES 4

            CBUFFER_START(_ShadowBuffer)
            // Last cascade is initialized with a no-op matrix. It always transforms
            // shadow coord to half(0, 0, NEAR_PLANE). We use this trick to avoid
            // branching since ComputeCascadeIndex can return cascade index = MAX_SHADOW_CASCADES
            float4x4 _WorldToShadow[MAX_SHADOW_CASCADES + 1];
            float4 _DirShadowSplitSpheres[MAX_SHADOW_CASCADES];
            float4 _DirShadowSplitSphereRadii;
            half4 _ShadowOffset0;
            half4 _ShadowOffset1;
            half4 _ShadowOffset2;
            half4 _ShadowOffset3;
            half4 _ShadowData; // (x: shadowStrength)
            CBUFFER_END

            //Scene Depth
            TEXTURE2D(_Depth);
            SAMPLER(sampler_Depth);

            //Shadow Cascades
            TEXTURE2D_SHADOW(_ShadowCascades);
            SAMPLER_CMP(sampler_ShadowCascades);

            //Far plane corners in view space
            float4 _FrustumCorners[4];

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                uint   id     : SV_VertexID;
            };

            struct VertexOutput
            {
                half4  pos    : SV_POSITION;
                half2  uv     : TEXCOORD0;
                float3 ray    : TEXCOORD1;
            };

            VertexOutput Vertex(VertexInput i)
            {
                VertexOutput o;
                o.pos = TransformObjectToHClip(i.vertex.xyz);
                o.uv  = i.uv;
                o.ray = _FrustumCorners[i.id]; 
                return o;
            }

            float4 GetCascadeWeights(float3 wpos)
            {
                float3 fromCenter0 = wpos.xyz - _DirShadowSplitSpheres[0].xyz;
                float3 fromCenter1 = wpos.xyz - _DirShadowSplitSpheres[1].xyz;
                float3 fromCenter2 = wpos.xyz - _DirShadowSplitSpheres[2].xyz;
                float3 fromCenter3 = wpos.xyz - _DirShadowSplitSpheres[3].xyz;
                float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

                half4 weights = half4(distances2 < _DirShadowSplitSphereRadii);
                weights.yzw = saturate(weights.yzw - weights.xyz);

                return weights;
            }

            float4 GetShadowCoordinates (float4 wpos, float4 cascadeWeights)
            {
                float3 sc0 = mul (_WorldToShadow[0], wpos).xyz;
                float3 sc1 = mul (_WorldToShadow[1], wpos).xyz;
                float3 sc2 = mul (_WorldToShadow[2], wpos).xyz;
                float3 sc3 = mul (_WorldToShadow[3], wpos).xyz;
                float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);

                return shadowMapCoordinate;
            }

            //TODO: Handle orthographic.
            //NOTE: This function exists in Core library via inv projection.
            float3 ComputeViewSpacePosition(VertexOutput i)
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_Depth, sampler_Depth, i.uv);
                depth = Linear01Depth(depth, _ZBufferParams);

                return i.ray * depth;
            }

            half4 Fragment(VertexOutput i) : SV_Target
            {
                float3 vpos           = ComputeViewSpacePosition(i);
                float4 wpos           = mul(unity_CameraToWorld, float4(vpos, 1));
                float4 cascadeWeights = GetCascadeWeights(wpos);
                float4 coords         = GetShadowCoordinates(wpos, cascadeWeights);

                half shadow = SAMPLE_TEXTURE2D_SHADOW(_ShadowCascades, sampler_ShadowCascades, coords);

                return half4(shadow, shadow, shadow, 1);
            }

            ENDHLSL
        }
    }
}
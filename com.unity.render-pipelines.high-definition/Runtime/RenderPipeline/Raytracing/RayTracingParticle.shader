Shader "HDRP/RayTracingParticle"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _DisplacementMode("DisplacementMode", Int) = 0
        [ToggleUI] _DisplacementLockObjectScale("displacement lock object scale", Float) = 1.0
        [ToggleUI] _DisplacementLockTilingScale("displacement lock tiling scale", Float) = 1.0
        [ToggleUI] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0
    }

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline"="HDRenderPipeline" "RenderType" = "HDRayTracingParticleShader" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            // Pass 0
            Cull   Off
            ZTest  Less // Required for XR occlusion mesh optimization
            ZWrite Off

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One SrcAlpha, Zero One

            HLSLPROGRAM

            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output = (Varyings)0;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return float4(1.0, 1.0, 1.0, 0.0);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags{ "RenderPipeline"="HDRenderPipeline" }
        Pass
        {
            Name "GBufferDXR"
            Tags{ "LightMode" = "GBufferDXR" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps5
            #pragma raytracing surface_shader

            #define PROCEDURAL_RAY_TRACING
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/HDRayTracingParticleSystem.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl"
            
            StructuredBuffer<ParticleAABB> _AABBBuffer;
            StructuredBuffer<ParticleDescriptor> _ParticleBuffer;

            [shader("closesthit")]
            void ClosestHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
            {
                ParticleDescriptor descriptor = _ParticleBuffer[PrimitiveIndex()];
                StandardBSDFData standardLitData;
                ZERO_INITIALIZE(StandardBSDFData, standardLitData);
                standardLitData.baseColor = float3(1, 0, 0);
                standardLitData.isUnlit = 1;
                standardLitData.normalWS = float3(0, 0, 1);
                standardLitData.emissiveAndBaked = descriptor.color * GetInverseCurrentExposureMultiplier();
                EncodeIntoStandardGBuffer(standardLitData, rayIntersectionGbuffer.gbuffer0, rayIntersectionGbuffer.gbuffer1, rayIntersectionGbuffer.gbuffer2, rayIntersectionGbuffer.gbuffer3);
                rayIntersectionGbuffer.t = 50.0;
            }

            bool RayBoxIntersectionTest(in float3 rayWorldOrigin, in float3 rayWorldDirection, in float3 boxPosWorld, in float3 boxHalfSize, out float outHitT)
            {
                // convert from world to box space
                float3 rd = rayWorldDirection;
                float3 ro = rayWorldOrigin - boxPosWorld;

                // ray-box intersection in box space
                float3 m = 1.0 / rd;
                float3 s = float3(
                    (rd.x < 0.0) ? 1.0 : -1.0,
                    (rd.y < 0.0) ? 1.0 : -1.0,
                    (rd.z < 0.0) ? 1.0 : -1.0);

                float3 t1 = m * (-ro + s * boxHalfSize);
                float3 t2 = m * (-ro - s * boxHalfSize);

                float tN = max(max(t1.x, t1.y), t1.z);
                float tF = min(min(t2.x, t2.y), t2.z);

                if (tN > tF || tF < 0.0) 
                    return false;

                outHitT = tN;
                return true;
            }
            [shader("intersection")]
            void IntersectionShaderGBuffer()
            {
                ParticleAABB particleAABB = _AABBBuffer[PrimitiveIndex()];
                float3 aabbPos = (particleAABB.minV + particleAABB.maxV) * 0.5f - _WorldSpaceCameraPos;
                float3 aabbSize = particleAABB.maxV - particleAABB.minV;
        
                float outHitT = 0;

                if (RayBoxIntersectionTest(WorldRayOrigin(), WorldRayDirection(), aabbPos, aabbSize * 0.5, outHitT))
                {
                    AttributeData attributeData;
                    attributeData.barycentrics = float2(0.5, 0.5);
                    ReportHit(outHitT, 0, attributeData);
                }
            }

            ENDHLSL
        }
    }
}

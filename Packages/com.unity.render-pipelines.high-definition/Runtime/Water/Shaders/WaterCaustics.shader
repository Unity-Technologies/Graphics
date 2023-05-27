Shader "Hidden/HDRP/WaterCaustics"
{
    Properties {}

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    //#pragma enable_d3d11_debug_symbols

    #pragma vertex Vert
    #pragma fragment Frag

    // General includes
    #define HIGH_RESOLUTION_WATER
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

    // Water includes
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterUtilities.hlsl"

    // This factor enlarges the size of the grid to compensate missing information on the sides
    #define GRID_SCALE_FACTOR 1.1
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            // This program doesn't require any culling or ztesting
            Cull   Off
            ZTest  Off
            ZWrite Off

            HLSLPROGRAM

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float3 originalPos : TEXCOORD0;
                float3 refractedPos : TEXCOORD1;
            };

            // Resolution of the caustics geometry
            int _CausticGeometryResolution;
            int _CausticsNormalsMipOffset;
            float _CausticsVirtualPlane;

            bool IntersectPlane(float3 ray_originWS, float3 ray_dir, float3 pos, float3 normal, out float t)
            {
                float3 ray_originPS = ray_originWS;
                float denom = dot(normal, ray_dir);
                bool flag = false;
                t = -1.0f;
                if (abs(denom) > 1e-6)
                {
                    float3 d = pos - ray_originPS;
                    t = dot(d, normal) / denom;
                    flag = (t >= 0);
                }
                return flag;
            }

            Varyings EvaluateVaryings(float3 gridPositionWS)
            {
                float3 center = float3(0, 0, 0);
                float3 baseNormal = float3(0, 1, 0);
                float planeDistance = _CausticsVirtualPlane;
                float3 incidentDir = float3(0, -1, 0);

                // Evaluate the normal at the position
                float3 surfaceGradient = EvaluateWaterSurfaceGradient_VS(gridPositionWS, _CausticsNormalsMipOffset, _CausticsBandIndex);
                float3 normal = SurfaceGradientResolveNormal(baseNormal, surfaceGradient);

                // Evaluate the refraction vector
                float3 refractedDirection = refract(incidentDir, normal, WATER_INV_IOR);

                // Intersect the straight ray with the ground plane
                float t;
                IntersectPlane(gridPositionWS, refractedDirection, center - float3(0, _CausticsVirtualPlane, 0), baseNormal, t);

                // Intersect the refracted ray with the ground plane
                float tRef;
                IntersectPlane(gridPositionWS, incidentDir, center - float3(0, _CausticsVirtualPlane, 0), baseNormal, tRef);

                // Fill the varyings
                Varyings output;
                output.originalPos = gridPositionWS + incidentDir * t;
                output.refractedPos = gridPositionWS + refractedDirection * tRef;
                float2 clipPos = output.refractedPos.xz / (_CausticsRegionSize * 0.5f);
                #if UNITY_UV_STARTS_AT_TOP
                    clipPos.y = -clipPos.y;
                #endif
                output.positionCS = float4(clipPos, UNITY_NEAR_CLIP_VALUE, 1.0);
                return output;
            }

            Varyings Vert(Attributes input)
            {
                // Compute the coordinates of the vertex
                uint w = input.vertexID / (_CausticGeometryResolution + 1);
                uint h = input.vertexID % (_CausticGeometryResolution + 1);

                // Compute the world space position of the vertex
                float2 vertexPos = float2(w / (float)_CausticGeometryResolution, h / (float)_CausticGeometryResolution);
                float2 gridSize = _CausticsRegionSize * GRID_SCALE_FACTOR;
                float3 center = float3(0, 0, 0);
                float3 gridPositionWS = float3(vertexPos.x * gridSize.x, 0, vertexPos.y * gridSize.y) - float3(gridSize.x * 0.5, 0, gridSize.y * 0.5) + center - 0.5 / _BandResolution;

                // Evaluate the caustics data
                return EvaluateVaryings(gridPositionWS);
            }

            float2 Frag(Varyings input) : SV_Target
            {
                // https://medium.com/@evanwallace/rendering-realtime-caustics-in-webgl-2a99a29a0b2c
                // Compute the original triangle area using the derivatives
                float intialTriangleArea = length(ddx(input.originalPos)) * length(ddy(input.originalPos));
                // Compute the refracted triangle area using the derivatives
                float refractedTriangleArea = length(ddx(input.refractedPos)) * length(ddy(input.refractedPos));
                // Compute the ratio between the two
                return float2(clamp(intialTriangleArea / refractedTriangleArea, 0.0, 5.0), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

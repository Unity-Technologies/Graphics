Shader "Hidden/HDRenderLoop/SkyHDRI"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Color.hlsl"
            #include "Common.hlsl"
            #include "CommonLighting.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/ShaderVariables.hlsl"

            #define IS_RENDERING_SKY
            #include "AtmosphericScattering.hlsl"

            TEXTURECUBE(_Cubemap);
            SAMPLERCUBE(sampler_Cubemap);

            float4x4 _InvViewProjMatrix;
            float4   _SkyParam; // x exposure, y multiplier, z rotation

            struct Attributes
            {
                float3 positionCS : POSITION;
                float3 eyeVector : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 eyeVector : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                // TODO: implement SV_vertexID full screen quad
                Varyings output;
                output.positionCS = float4(input.positionCS.xy, UNITY_RAW_FAR_CLIP_VALUE
                    , 1.0);
                output.eyeVector = input.eyeVector;

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.eyeVector);

                // Rotate direction
                float phi = DegToRad(_SkyParam.z);
                float cosPhi, sinPhi;
                sincos(phi, sinPhi, cosPhi);
                float3 rotDirX = float3(cosPhi, 0, -sinPhi);
                float3 rotDirY = float3(sinPhi, 0, cosPhi);
                dir = float3(dot(rotDirX, dir), dir.y, dot(rotDirY, dir));

                float3 skyDome = ClampToFloat16Max(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgb * exp2(_SkyParam.x) * _SkyParam.y);

                Coordinate coord = GetCoordinate(input.positionCS.xy, _ScreenSize.zw);

                // Get the depth value of the scene, or use 0.01 otherwise.
                float  rawDepth   = max(LOAD_TEXTURE2D(_CameraDepthTexture, coord.unPositionSS).r, 0.01);
                float3 positionWS = UnprojectToWorld(rawDepth, coord.positionSS, _InvViewProjMatrix);

                float4 c1, c2, c3;
                VolundTransferScatter(positionWS, c1, c2, c3);

                float4 coord1 = float4(c1.rgb + c3.rgb, max(0.f, 1.f - c1.a - c3.a));
                float3 coord2 = c2.rgb;

                float sunCos = dot(normalize(dir), _SunDirection);
                float miePh  = MiePhase(sunCos, _MiePhaseAnisotropy);

                float2 occlusion  = float2(1.0, 1.0); // TODO.
                float  extinction = coord1.a;
                float3 scatter    = coord1.rgb * occlusion.x + coord2 * miePh * occlusion.y;

                #ifdef ATMOSPHERICS_DEBUG
                    switch (_AtmosphericsDebugMode)
                    {
                        case ATMOSPHERICS_DBG_RAYLEIGH:           return c1;
                        case ATMOSPHERICS_DBG_MIE:                return c2 * miePh;
                        case ATMOSPHERICS_DBG_HEIGHT:             return c3;
                        case ATMOSPHERICS_DBG_SCATTERING:         return float4(scatter, 0.0);
                        case ATMOSPHERICS_DBG_OCCLUSION:          return float4(occlusion.xy, 0.0, 0.0);
                        case ATMOSPHERICS_DBG_OCCLUDEDSCATTERING: return float4(scatter, 0.0);
                    }
                #endif

                // Blend with the color of the scene.
                return float4(skyDome * extinction + scatter, 0.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}

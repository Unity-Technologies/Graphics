Shader "Hidden/HDRP/Sky/PbrSky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    // #pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"

    int _HasGroundAlbedoTexture;    // bool...
    int _HasGroundEmissionTexture;  // bool...
    int _HasSpaceEmissionTexture;   // bool...
    int _RenderSunDisk;             // bool...

    float _GroundEmissionMultiplier;
    float _SpaceEmissionMultiplier;

    // Sky framework does not set up global shader variables (even per-view ones),
    // so they can contain garbage. It's very difficult to not include them, however,
    // since the sky framework includes them internally in many header files.
    // Just don't use them. Ever.
    float3   _WorldSpaceCameraPos1;
    float4x4 _ViewMatrix1;
    #undef UNITY_MATRIX_V
    #define UNITY_MATRIX_V _ViewMatrix1

    // 3x3, but Unity can only set 4x4...
    float4x4 _PlanetRotation;
    float4x4 _SpaceRotation;

    TEXTURECUBE(_GroundAlbedoTexture);
    TEXTURECUBE(_GroundEmissionTexture);
    TEXTURECUBE(_SpaceEmissionTexture);

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float4 RenderSky(Varyings input)
    {
        const float R = _PlanetaryRadius;

        // TODO: Not sure it's possible to precompute cam rel pos since variables
        // in the two constant buffers may be set at a different frequency?
        const float3 O = _WorldSpaceCameraPos1 - _PlanetCenterPosition;
        const float3 V = GetSkyViewDirWS(input.positionCS.xy);

        bool renderSunDisk = _RenderSunDisk != 0;

        float3 N; float r; // These params correspond to the entry point
        float tEntry = IntersectAtmosphere(O, V, N, r).x;
        float tExit  = IntersectAtmosphere(O, V, N, r).y;

        float NdotV  = dot(N, V);
        float cosChi = -NdotV;
        float cosHor = ComputeCosineOfHorizonAngle(r);

        bool rayIntersectsAtmosphere = (tEntry >= 0);
        bool lookAboveHorizon        = (cosChi >= cosHor);

        float  tFrag    = FLT_INF;
        float3 radiance = 0;

        if (renderSunDisk)
        {
            // Intersect and shade emissive celestial bodies.
            // Unfortunately, they don't write depth.
            for (uint i = 0; i < _DirectionalLightCount; i++)
            {
                DirectionalLightData light = _DirectionalLightDatas[i];

                // Use scalar or integer cores (more efficient).
                bool interactsWithSky = asint(light.distanceFromCamera) >= 0;

                // Celestial body must be outside the atmosphere (request from Pierre D).
                float lightDist = max(light.distanceFromCamera, tExit);

                if (interactsWithSky && asint(light.angularDiameter) != 0 && lightDist < tFrag)
                {
                    // We may be able to see the celestial body.
                    float3 L = -light.forward.xyz;

                    float LdotV    = -dot(L, V);
                    float rad      = acos(LdotV);
                    float radInner = 0.5 * light.angularDiameter;
                    float cosInner = cos(radInner);
                    float cosOuter = cos(radInner + light.flareSize);

                    // float solidAngle = TWO_PI * (1 - cosInner);
                    float solidAngle = 1; // Don't scale...

                    if (LdotV >= cosOuter)
                    {
                        // Sun flare is visible. Sun disk may or may not be visible.
                        // Assume uniform emission.
                        float3 color = light.color.rgb;
                        float  scale = rcp(solidAngle);

                        if (LdotV >= cosInner) // Sun disk.
                        {
                            tFrag = lightDist;

                            if (light.surfaceTextureScaleOffset.x > 0)
                            {
                                // The cookie code de-normalizes the axes.
                                float2 proj   = float2(dot(-V, normalize(light.right)), dot(-V, normalize(light.up)));
                                float2 angles = HALF_PI - acos(proj);
                                float2 uv     = angles * rcp(radInner) * 0.5 + 0.5;

                                color *= SampleCookie2D(uv, light.surfaceTextureScaleOffset);
                                // color *= SAMPLE_TEXTURE2D_ARRAY(_CookieTextures, s_linear_clamp_sampler, uv, light.surfaceTextureIndex).rgb;
                            }
                            
                            color *= light.surfaceTint;
                        }
                        else // Flare region.
                        {
                            float r = max(0, rad - radInner);
                            float w = saturate(1 - r * rcp(light.flareSize));

                            color *= light.flareTint;
                            scale *= pow(w, light.flareFalloff);
                        }

                        radiance += color * scale;
                    }
                }
            }
        }

        if (rayIntersectsAtmosphere && !lookAboveHorizon) // See the ground?
        {
            float tGround = tEntry + IntersectSphere(R, cosChi, r).x;

            if (tGround < tFrag)
            {
                // Closest so far.
                // Make it negative to communicate to EvaluatePbrAtmosphere that we intersected the ground.
                tFrag = -tGround;

                radiance = 0;

                float3 gP = O + tGround * -V;
                float3 gN = normalize(gP);

                if (_HasGroundEmissionTexture)
                {
                    float4 ts = SAMPLE_TEXTURECUBE(_GroundEmissionTexture, s_trilinear_clamp_sampler, mul(gN, (float3x3)_PlanetRotation));
                    radiance += _GroundEmissionMultiplier * ts.rgb;
                }

                float3 albedo = _GroundAlbedo;

                if (_HasGroundAlbedoTexture)
                {
                    albedo *= SAMPLE_TEXTURECUBE(_GroundAlbedoTexture, s_trilinear_clamp_sampler, mul(gN, (float3x3)_PlanetRotation)).rgb;
                }

                float3 gBrdf = INV_PI * albedo;

                // Shade the ground.
                for (uint i = 0; i < _DirectionalLightCount; i++)
                {
                    DirectionalLightData light = _DirectionalLightDatas[i];

                    // Use scalar or integer cores (more efficient).
                    bool interactsWithSky = asint(light.distanceFromCamera) >= 0;

                    float3 L          = -light.forward.xyz;
                    float3 intensity  = light.color.rgb;
                    float3 irradiance = SampleGroundIrradianceTexture(dot(gN, L));

                    radiance += gBrdf * (irradiance * intensity); // Scale from unit intensity to light's intensity
                }
            }
        }
        else if (tFrag == FLT_INF) // See the stars?
        {
            if (_HasSpaceEmissionTexture)
            {
                // V points towards the camera.
                float4 ts = SAMPLE_TEXTURECUBE(_SpaceEmissionTexture, s_trilinear_clamp_sampler, mul(-V, (float3x3)_SpaceRotation));
                radiance += _SpaceEmissionMultiplier * ts.rgb;
            }
        }

        float3 skyColor = 0, skyOpacity = 0;

        if (rayIntersectsAtmosphere)
        {
            float distAlongRay = tFrag;
            EvaluatePbrAtmosphere(_WorldSpaceCameraPos1, V, distAlongRay, renderSunDisk, skyColor, skyOpacity);
        }

        skyColor += radiance * (1 - skyOpacity);
        skyColor *= _IntensityMultiplier;

        return float4(skyColor, 1.0);
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderSky(input); // The cube map is not pre-exposed
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float4 value = RenderSky(input);
        value.rgb *= GetCurrentExposureMultiplier(); // Only the full-screen pass is pre-exposed
        return value;
    }

    float4 FragBlack(Varyings input) : SV_Target
    {
        return 0;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBlack
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBlack
            ENDHLSL
        }

    }
    Fallback Off
}

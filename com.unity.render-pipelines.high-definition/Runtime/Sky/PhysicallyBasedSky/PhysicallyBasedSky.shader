Shader "Hidden/HDRP/Sky/PbrSky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma multi_compile _ PATH_TRACED_PREVIEW

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

// #define PATH_TRACED_PREVIEW

#ifdef PATH_TRACED_PREVIEW
    #define MAKE_FLT(sgn, exponent, fraction) asfloat((((sgn) & 0x1) << 31) | (((exponent) & 0xFF) << 23) | ((fraction) & 0xFFFFFF))
    #define FLT_SNAN MAKE_FLT(0x1, 0xFF, 0x1)
    #define DEBUG
#ifdef DEBUG
    #define ASSERT(x) if (!(bool)(x)) return FLT_SNAN
#else  // DEBUG
    #define ASSERT(x) x
#endif // DEBUG

    static const uint s_RandomPrimes[10] = { 0xD974CF83,
                                             0xFAF269B5,
                                             0xAE727FA9,
                                             0x5BA52335,
                                             0xA4E819D5,
                                             0xDD638559,
                                             0xC0972367,
                                             0x4B190D9B,
                                             0xD1894DB5,
                                             0xA78BCBB3 };

    struct Ray
    {
        float3 origin;
        float3 direction;
        float  frequency;
    };

    // 'l' is the size of the permutation vector (typically, the number of strata per dimension).
    uint permute(uint i, uint l, uint p)
    {
        if (p == 0) return i; // identity permutation when (p == 0)

        uint w = l - 1;

        w |= w >> 1;
        w |= w >> 2;
        w |= w >> 4;
        w |= w >> 8;
        w |= w >> 16;
        do
        {
            i ^= p; i *= 0xe170893d;
            i ^= p >> 16;
            i ^= (i & w) >> 4;
            i ^= p >> 8; i *= 0x0929eb3f;
            i ^= p >> 23;
            i ^= (i & w) >> 1; i *= 1 | p >> 27;
            i *= 0x6935fa69;
            i ^= (i & w) >> 11; i *= 0x74dcb303;
            i ^= (i & w) >> 2;  i *= 0x9e501cc3;
            i ^= (i & w) >> 2;  i *= 0xc860a3df;
            i &= w;
            i ^= i >> 5;
        } while (i >= l);
        return (i + p) % l;
    }

    float randfloat(uint i, uint p)
    {
        if (p == 0) return 0.5f; // always 0.5 when (p == 0)

        i ^= p;
        i ^= i >> 17;
        i ^= i >> 10; i *= 0xb36534e5;
        i ^= i >> 12;
        i ^= i >> 21; i *= 0x93fc4795;
        i ^= 0xdf6e307f;
        i ^= i >> 17; i *= 1 | p >> 18;

        float f = i * (1.0f / 4294967808.0f);

        ASSERT(0 <= f && f < 1);

        return f;
    }

    // Multi-dimensional correlated multi-jittered sequence.
    // We specialize it for 6D, which means we generate 64 points.
    float cmj6D(uint pointIndex, uint dimIndex, uint seed)
    {
        const uint s = 2;           // Number of strata per dimension
        const uint t = 6;           // Strength of the orthogonal array (number of dimensions)
        const uint n = 64;          // Size of the sequence: n = s^t
        const uint p = seed;        // Pseudo-random permutation seed
        const uint i = permute(pointIndex, n, p); // Shuffle the points
        const uint j = dimIndex;

        ASSERT(i < n);
        ASSERT(j < t);

        const uint p1 = (p * (j + 1)) * 0x51633e2d;
        const uint p2 = (p * (j + 1)) * 0x68bc21eb;
        const uint p3 = (p * (j + 1)) * 0x02e5be93;

        uint digits[t];

        uint d; // Avoid compiler warning

        // digits = toBaseS(i, s);
        for (d = 0; d < t; d /= s, d++)
        {
            digits[d] = d % s;
        }

        uint stratum = permute(digits[j], s, p1);

        // digits = allButJ(digits, j);
        for (d = j; d < (t - 1); d++)
        {
            digits[d] = digits[d + 1];
        }

        uint poly = 0;

        // poly = evalPoly(digits, s);
        for (d = (t - 2); d != -1; d--)
        {
            poly = (poly * s) + digits[d]; // Horner's rule
        }

        uint  stm      = n / s; // pow(s, t - 1)
        uint  sStratum = permute(poly, stm, p2);
        float jitter   = randfloat(i, p3);

        return (stratum + (sStratum + jitter) / stm) / s;
    }

    // 1x path per thread per call.
    // Tracing multiple paths can be achieved via multiple passes.
    float3 PathTraceSky(uint2  groupCoord, uint threadIndex,
                        float3 rayOrigin,  uint pathIndex, uint numPaths, uint numBounces)
    {
        // Integral over ((1 + NumBounces) * 3) dimensions:
        // 2x for filter IS + 1x for photon frequency selection (bounce #0);
        // 1x for scattering location + 2x for the scattering direction (bounce #i).
        uint numDimensions = 3 * numBounces;

        uint groupSize = 8;
        uint bounce    = 0;

        // The group size is (8*8 == 64). It matches the number of points the RNG provides,
        // so we trace a group of paths at the same time.
        // It supports up to 6 dimensions at a time (2^6 == 64).
        // Therefore, every 6 dimensions (2 bounces), we must switch to a new permutation seed.
        // Every path (per pixel) must also use a unique seed.
        // Note: this guarantees good distribution of samples within the group,
        // but not within each individual pixel (for numPaths > 1).
        // Perhaps we could do something using powers of 2?
        uint   seed = permute(pathIndex, numPaths, s_RandomPrimes[bounce / 2]);
        float3 rnd  = float3(cmj6D(threadIndex, (bounce * 3 + 0) % 5, seed),
                             cmj6D(threadIndex, (bounce * 3 + 1) % 5, seed),
                             cmj6D(threadIndex, (bounce * 3 + 2) % 5, seed));

        // Determine which pixel the path contributes to.
        float2 filterOffset = rnd.xy; // TODO: do not use the box filter?
        float2 screenCoord  = (groupCoord * groupSize) + filterOffset * groupSize;

        Ray ray; // Pinhole camera...

        ray.origin    = rayOrigin;
        ray.direction = -normalize(mul(float4(screenCoord, 1, 1), _PixelCoordToViewDirWS).xyz);
        ray.frequency = floor(rnd.z * 3); // Color channel, for now...

        for (bounce = 1; bounce < numBounces; bounce++)
        {
            seed = permute(pathIndex, numPaths, s_RandomPrimes[bounce / 2]);
            rnd  = float3(cmj6D(threadIndex, (bounce * 3 + 0) % 5, seed),
                          cmj6D(threadIndex, (bounce * 3 + 1) % 5, seed),
                          cmj6D(threadIndex, (bounce * 3 + 2) % 5, seed));


        }

        return 0;
    }

#endif // PATH_TRACED_PREVIEW

    float4 RenderSky(Varyings input)
    {
        const float R = _PlanetaryRadius;


    #ifdef PATH_TRACED_PREVIEW
        float3 rayOrigin = _WorldSpaceCameraPos1 - _PlanetCenterPosition;
        float3 skyColor  = PathTraceSky(rayOrigin, input.positionCS.xy, 1, 1);
    #else // PATH_TRACED_PREVIEW

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
                                color *= light.surfaceTint;
                            }
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
    #endif // PATH_TRACED_PREVIEW

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

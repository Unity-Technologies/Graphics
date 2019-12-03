Shader "Hidden/HDRP/Sky/PbrSky"
{
    HLSLINCLUDE

    #define USE_PATH_SKY 1
    #define NUM_BOUNCES 1

    #pragma vertex Vert

    // #pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

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

    // Utilities

    static const uint s_RandomPrimes[11] = { 0xD974CF83,
                                             0xFAF269B5,
                                             0xAE727FA9,
                                             0x5BA52335,
                                             0xA4E819D5,
                                             0xDD638559,
                                             0xC0972367,
                                             0x4B190D9B,
                                             0xD1894DB5,
                                             0xA78BCBB3,
                                             0xCBE0EC0B };

    uint permute(uint i, uint l, uint p)
    {
        if (p == 0) return i; // identity permutation when p == 0
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
            i ^= (i & w) >> 2; i *= 0x9e501cc3;
            i ^= (i & w) >> 2; i *= 0xc860a3df;
            i &= w;
            i ^= i >> 5;
        } while (i >= l);
        return (i + p) % l;
    }

    float randfloat(uint i, uint p)
    {
        if (p == 0) return 0.5f; // always 0.5 when p == 0
        i ^= p;
        i ^= i >> 17;
        i ^= i >> 10;
        i *= 0xb36534e5;
        i ^= i >> 12;
        i ^= i >> 21;
        i *= 0x93fc4795;
        i ^= 0xdf6e307f;
        i ^= i >> 17;
        i *= 1 | p >> 18;

        float f = i * (1.0f / 4294967808.0f);

        return f;
    }

    // // Compute the digits of decimal value Ã¢â‚¬ËœvÃ¢â‚¬Ëœ expressed in base Ã¢â‚¬ËœsÃ¢â‚¬Ëœ
    // void toBaseS(uint v, uint s, uint t, out uint outData[NUM_BOUNCES])
    // {
    //     for (uint i = 0; i < t; v /= s, ++i)
    //     {
    //         outData[i] = v % s;
    //     }
    // }

    // // Copy all but the j-th element of vector in
    // void allButJ(const uint inData[NUM_BOUNCES], uint inSize, uint omit, out uint outData[NUM_BOUNCES - 1])
    // {
    //     uint i = 0;

    //     for (; i < omit; ++i)
    //     {
    //         outData[i] = inData[i];
    //     }

    //     i++;

    //     for (; i < inSize; ++i)
    //     {
    //         outData[i - 1] = inData[i];
    //     }
    // }

    // // Evaluate polynomial with coefficients a at location arg
    // uint evalPoly(const uint inData[NUM_BOUNCES - 1], uint inSize, uint arg)
    // {
    //     uint ans  = 0;
    //     int  last = inSize - 1;

    //     for (int i = last; i >= 0; i--)
    //     {
    //         ans = (ans * arg) + inData[i]; // HornerÃ¢â‚¬â„¢s rule
    //     }

    //     return ans;
    // }

    // float cmjdD(uint i, // Sample index
    //         uint j, // Dimension (<= s+1)
    //         uint s, // Number of levels/strata
    //         uint t, // Strength of OA (t=d)
    //         uint p) // Pseudo-random permutation seed (per set)
    // {
    //     uint iDigits[NUM_BOUNCES];
    //     uint pDigits[NUM_BOUNCES - 1];

    //     // memset(iDigits, 0, t       * sizeof(uint));
    //     // memset(pDigits, 0, (t - 1) * sizeof(uint));

    //     uint p0 = p;
    //     uint p1 = p * (j + 1) * 0x51633e2d;
    //     uint p2 = p * (j + 1) * 0x68bc21eb;
    //     uint p3 = p * (j + 1) * 0x02e5be93;

    //     uint N   = (uint)round(pow(s, t)); // Total number of samples
    //     uint stm = N / s; // pow(s, t-1)

    //     i = permute(i, N, p0);

    //     toBaseS(i, s, t, iDigits);

    //     uint stratum = permute(iDigits[j], s, p1);

    //     allButJ(iDigits, t, j, pDigits);

    //     uint sStratum = evalPoly(pDigits, t - 1, s);
    //              sStratum = permute(sStratum, stm, p2);

    //     float jitter = randfloat(i, p3);

    //     return (stratum + (sStratum + jitter) / stm) / s;
    // }

    float2 cmj2D(int s, int m, int n, int p)
    {
        int   sx = permute(s % m, m, p * 0xA511E9B3);
        int   sy = permute(s / m, n, p * 0x63D83595);
        float jx = randfloat(s,      p * 0xA399D265);
        float jy = randfloat(s,      p * 0x711AD6A5);

        float2 r = float2((s % m + (sy + jx) / n) / m,
                          (s / m + (sx + jy) / m) / n);
        return r;
    }

    float SampleRectExpMedium(float optDepth, float height, float cosTheta, float rcpSeaLvlAtt, float rcpH)
    {
        float t = optDepth * rcpSeaLvlAtt;
        float p = cosTheta * rcpH;

        if (abs(p) > FLT_EPS)
        {
            t = -log(1.0f - p * t * exp(height * rcpH)) * rcp(p);
        }

        return t;
    }

    float RadAtDist(float r, float cosTheta, float t)
    {
        return sqrt(r * r + t * (t + 2.0f * (r * cosTheta)));
    }

    float CosAtDist(float r, float cosTheta, float t, float radAtDist)
    {
        return (t + r * cosTheta) * rcp(radAtDist);
    }

    float EvalOptDepthSpherExpMedium(float r, float cosTheta, float t,
                                     float seaLvlAtt, float Z,
                                     float H, float rcpH)
    {
        float rX        = r;
        float cosThetaX = cosTheta;
        float rY        = RadAtDist(rX, cosThetaX, t);
        float cosThetaY = CosAtDist(rX, cosThetaX, t, rY);
        // Potentially swap X and Y.
        // Convention: at the point Y, the ray points up.
        cosThetaX = (cosThetaY >= 0) ? cosThetaX : -cosThetaX;
        cosThetaY = abs(cosThetaY);
        float zX  = rX * rcpH;
        float zY  = rY * rcpH;
        float chX = RescaledChapmanFunction(zX, Z, cosThetaX);
        float chY = ChapmanUpperApprox(zY, cosThetaY) * exp(Z - zY); // Rescaling adds 'exp'
        // We may have swapped X and Y.
        float ch = abs(chX - chY);
        return ch * H * seaLvlAtt;
    }

    float SampleSpherExpMedium(float optDepth, float r, float cosTheta,
                               float rcpSeaLvlAtt, float Z, float R, float H, float rcpH)
    {
        // This allows us to use (seaLvlAtt = rcpSeaLvlAtt = 1) below.
        optDepth *= rcpSeaLvlAtt;
        float rcpOptDepth = rcp(optDepth); // Must not be 0.
        // Make an initial guess.
        float t = SampleRectExpMedium(optDepth, r - R, cosTheta, 1, rcpH);
        float relDiff;
        uint numIterations = 0;
        do // Perform a Newton–Raphson iteration.
        {
            float radAtDist = RadAtDist(r, cosTheta, t);
            // Evaluate the function and its (reciprocal) derivative:
            // f (t) = OptDepthAtDist(t) - GivenOptDepth = 0,
            // f'(t) = AttCoefAtDist(t).
            // The sea level attenuation coefficient cancels out during division.
            float optDepthAtDist = EvalOptDepthSpherExpMedium(r, cosTheta, t, 1, Z, H, rcpH);
            float rcpAttCoefAtDist = 1 * exp((radAtDist - R) * rcpH);
            // Refine the initial guess.
            // t1 = t0 - f(t0) / f'(t0).
            t = t - (optDepthAtDist - optDepth) * rcpAttCoefAtDist;
            relDiff = optDepthAtDist * rcpOptDepth - 1;
            numIterations++;
        } while (numIterations < 4);
        // } while (abs(relDiff) > 0.001); // Stop when the accuracy goal has been reached.
        return t;
    }

    // Input position is relative to sphere origin
    float3 EvalLight(float3 position, bool useNormal, DirectionalLightData light)
    {
        float3 color = 0;
        float3 L = -light.forward;

        // Use scalar or integer cores (more efficient).
        bool interactsWithSky = asint(light.distanceFromCamera) > 0;

        if (interactsWithSky)
        {
            // TODO: should probably unify height attenuation somehow...
            // TODO: Not sure it's possible to precompute cam rel pos since variables
            // in the two constant buffers may be set at a different frequency?
            float3 X       = position;
            float r        = length(X);
            float cosHoriz = ComputeCosineOfHorizonAngle(r);
            float cosTheta = dot(X, L) * rcp(r); // Normalize

            if (cosTheta >= cosHoriz) // Above horizon
            {
                color = light.color;
                float3 oDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
                // Cannot do this once for both the sky and the fog because the sky may be desaturated. :-(
                float3 transm  = TransmittanceFromOpticalDepth(oDepth);
                float3 opacity = 1 - transm;
                color *= 1 - (Desaturate(opacity, _AlphaSaturation) * _AlphaMultiplier);
                if (useNormal)
                {
                    color *= saturate(dot(normalize(position), L));
                }
            }
        }

        return color;
    }

    float3 SpectralTracking(uint2 positonSS, uint numWavelengths, uint numPaths, uint numBounces)
    {
        const float A = _AtmosphericRadius;
        const float R = _PlanetaryRadius;
        const float n = _AirDensityFalloff;
        const float H = _AirScaleHeight;
        const float Z = n * R;

        float3 color = 0;

        for (uint p = 0; p < numPaths; p++) // Iterate over paths
        {
            const uint numStrata   = (uint)sqrt(numPaths);
            const uint permutation = positonSS.x ^ positonSS.y;

            // Sample the sensor.
            const float2 subPixel  = cmj2D(p, numStrata, numStrata, permutation ^ s_RandomPrimes[0]);
            const float2 sensorPos = positonSS + subPixel * _ScreenSize.zw; // TODO: verify (_ScreenSize != 0)

            // Init the ray.
            float3 O = _WorldSpaceCameraPos1 - _PlanetCenterPosition;
            const float3 V = GetSkyViewDirWS(sensorPos);

            float2 atmosEntryExit = IntersectSphere(A, dot(normalize(O), -V), length(O));

            bool rayIntersectsAtmosphere = atmosEntryExit.y >= 0;
            if (!rayIntersectsAtmosphere) continue; // Miss

            // Do not start outside the atmosphere.
            if (atmosEntryExit.x > 0)
            {
                O -= V * atmosEntryExit.x * (1 + FLT_EPS);
            }

            for (uint w = 0; w < numWavelengths; w++) // Iterate over wavelengths
            {
                float3 X =  O;
                float3 D = -V; // Point away from the camera
                float pathContribution = 0;

                for (uint b = 0; b < numBounces; b++) // Step along the path
                {
                    float  r = length(X);
                    float3 N = normalize(X);

                    if (r > A) break; // Outside of the atmosphere

                    float cosChi = dot(N, D);
                    float cosHor = ComputeCosineOfHorizonAngle(r);

                    bool lookAboveHorizon = (cosChi >= cosHor);

                    float maxOptDepth = ComputeAtmosphericOpticalDepth(r, cosChi, lookAboveHorizon)[w];
                    float maxOpacity  = OpacityFromOpticalDepth(maxOptDepth);
                    float rndOpacity  = randfloat(b, permutation ^ s_RandomPrimes[b + 1]);

                    bool surfaceContibution     = !lookAboveHorizon;
                    bool surfaceScatteringEvent = surfaceContibution && (rndOpacity > maxOpacity);

                    float bounceWeight = 1;
                    if (surfaceScatteringEvent)
                    {
                        float t = IntersectSphere(R, cosChi, r).x; // Assume we are outside

                        X += t * D;

                        /* Shade the surface point (account for atmospheric attenuation). */
                        bounceWeight *= _GroundAlbedo[w] / PI;
                        /* Pick a new direction for a Lambertian BRDF. */
                    }
                    else // Volume scattering event
                    {
                        bounceWeight           = surfaceContibution ? 1 : rndOpacity;
                        float opacityToInvert  = surfaceContibution ? rndOpacity : rndOpacity * maxOpacity;
                        float optDepthToInvert = OpticalDepthFromOpacity(opacityToInvert);

                        float t = SampleSpherExpMedium(optDepthToInvert, r, cosChi,
                                                       rcp(_AirSeaLevelExtinction[w]), Z, R, H, n);

                        X += t * D;

                        // TODO: do not generate scattering events outside the atmosphere. Should probably clamp the distance.

                        /* Shade the volume point (account for atmospheric attenuation). */
                        float volumeAlbedo = (_AirSeaLevelScattering / _AirSeaLevelExtinction)[w];
                        bounceWeight *= volumeAlbedo / (4.0f * PI);
                        /* Compute a new direction (uniformly for now). */
                    }

                    float lightColor = 0;
                    for (uint i = 0; i < _DirectionalLightCount; i++)
                    {
                        DirectionalLightData light = _DirectionalLightDatas[i];
                        lightColor += EvalLight(X, surfaceScatteringEvent, light)[w];
                    }

                    pathContribution += lightColor * bounceWeight;

                    const float2 random2D = cmj2D(p, numStrata, numStrata, permutation ^ s_RandomPrimes[b]);
                    if (surfaceScatteringEvent)
                    {
                        /* Shade the surface point (account for atmospheric attenuation). */
                        /* Pick a new direction for a Lambertian BRDF. */
                        float3x3 basis = GetLocalFrame(normalize(X));
                        D = SampleHemisphereCosine(random2D.x, random2D.y);
                        D = mul(basis, D);
                    }
                    else // Volume scattering event
                    {
                        // TODO: do not generate scattering events outside the atmosphere. Should probably clamp the distance.

                        /* Shade the volume point (account for atmospheric attenuation). */
                        /* Compute a new direction (uniformly for now). */
                        D = SampleSphereUniform(random2D.x, random2D.y);
                    }
                }

                color[w] += pathContribution / numPaths;
            }


        }

        return color;
    }

    float4 RenderSky(Varyings input)
    {
#if USE_PATH_SKY
        float3 skyColor = SpectralTracking((uint2)input.positionCS.xy, 3, 1, 1);
#else
        const float R = _PlanetaryRadius;

        // TODO: Not sure it's possible to precompute cam rel pos since variables
        // in the two constant buffers may be set at a different frequency?
        const float3 O = _WorldSpaceCameraPos1 - _PlanetCenterPosition;
        const float3 V = GetSkyViewDirWS(input.positionCS.xy);

        bool renderSunDisk = _RenderSunDisk != 0;

        float3 N; float r; // These params correspond to the entry point
        float tEntry = IntersectAtmosphere(O, V, N, r).x;

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

                if (interactsWithSky && asint(light.angularDiameter) != 0 && light.distanceFromCamera <= tFrag)
                {
                    // We may be able to see the celestial body.
                    float3 L = -light.forward.xyz;

                    float LdotV    = -dot(L, V);
                    float rad      = acos(LdotV);
                    float radInner = 0.5 * light.angularDiameter;
                    float cosInner = cos(radInner);
                    float cosOuter = cos(radInner + light.flareSize);

                    float solidAngle = 1; // Don't scale...
                    // float solidAngle = TWO_PI * (1 - cosInner);

                    if (LdotV >= cosOuter)
                    {
                        // Sun flare is visible. Sun disk may or may not be visible.
                        // Assume uniform emission.
                        float3 color = light.color.rgb;
                        float  scale = rcp(solidAngle);

                        if (LdotV >= cosInner) // Sun disk.
                        {
                            tFrag = light.distanceFromCamera;

                            if (light.surfaceTextureIndex != -1)
                            {
                                // The cookie code de-normalizes the axes.
                                float2 proj   = float2(dot(-V, normalize(light.right)), dot(-V, normalize(light.up)));
                                float2 angles = HALF_PI - acos(proj);
                                float2 uv     = angles * rcp(radInner) * 0.5 + 0.5;

                                color *= SAMPLE_TEXTURE2D_ARRAY(_CookieTextures, s_linear_clamp_sampler, uv, light.surfaceTextureIndex).rgb;
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

                        radiance = color * scale;
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
#endif // USE_PATH_SKY

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

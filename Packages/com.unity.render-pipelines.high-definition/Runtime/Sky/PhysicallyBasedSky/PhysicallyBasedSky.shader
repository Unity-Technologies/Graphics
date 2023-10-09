Shader "Hidden/HDRP/Sky/PbrSky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    // #pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyRendering.hlsl"

    int _HasGroundAlbedoTexture;    // bool...
    int _HasGroundEmissionTexture;  // bool...
    int _HasSpaceEmissionTexture;   // bool...

    float _GroundEmissionMultiplier;
    float _SpaceEmissionMultiplier;

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

        const float3 O = _PBRSkyCameraPosPS;
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
            radiance = RenderSunDisk(tFrag, tExit, V);

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

                float3 albedo = _GroundAlbedo.xyz;

                if (_HasGroundAlbedoTexture)
                {
                    albedo *= SAMPLE_TEXTURECUBE(_GroundAlbedoTexture, s_trilinear_clamp_sampler, mul(gN, (float3x3)_PlanetRotation)).rgb;
                }

                float3 gBrdf = INV_PI * albedo;

                // Shade the ground.
                for (uint i = 0; i < _CelestialLightCount; i++)
                {
                    CelestialBodyData light = _CelestialBodyDatas[i];

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
            float3 cameraPosWS = _PBRSkyCameraPosPS + _PlanetCenterPosition.xyz;
            EvaluatePbrAtmosphere(cameraPosWS, V, distAlongRay, renderSunDisk, skyColor, skyOpacity);
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

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "PBRSky Cubemap"

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
            Name "PBRSky"

            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }
    }
    Fallback Off
}

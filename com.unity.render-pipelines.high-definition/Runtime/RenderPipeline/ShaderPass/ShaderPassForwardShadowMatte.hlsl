#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

void ShadowLoopEverage( HDShadowContext shadowContext, PositionInputs posInput, float3 normalWS, uint featureFlags, uint renderLayer,
                        out float shadow)
{
    shadow = 1.0f;
    float shadowCount = 0.0f;

    // With XR single-pass and camera-relative: offset position to do lighting computations from the combined center view (original camera matrix).
    // This is required because there is only one list of lights generated on the CPU. Shadows are also generated once and shared between the instanced views.
    ApplyCameraRelativeXR(posInput.positionWS);

    // Initialize the contactShadow and contactShadowFade fields
    //InitContactShadow(posInput, context);

    // First of all we compute the shadow value of the directional light to reduce the VGPR pressure
    //if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        // Evaluate sun shadows.
        if (_DirectionalShadowIndex >= 0)
        {
            DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

            // TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
            float3 wi = -light.forward;

            // Is it worth sampling the shadow map?
            if ((light.lightDimmer > 0) && (light.shadowDimmer > 0)
                //&& // Note: Volumetric can have different dimmer, thus why we test it here
                //IsNonZeroBSDF(V, L, preLightData, bsdfData) &&
                //!ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex)
                )
            {
                float shadowD = GetDirectionalShadowAttenuation(shadowContext,
                                                                posInput.positionSS, posInput.positionWS, normalWS,
                                                                light.shadowIndex, wi);
                shadow = min(shadow, shadowD);
                shadowCount += 1.0f;
            }
        }
    }

    //if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        uint lightCount, lightStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, lightStart, lightCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        lightCount = _PunctualLightCount;
        lightStart = 0;
#endif

        bool fastPath = false;
    #if SCALARIZE_LIGHT_LOOP
        uint lightStartLane0;
        fastPath = IsFastPath(lightStart, lightStartLane0);

        if (fastPath)
        {
            lightStart = lightStartLane0;
        }
    #endif

        // Scalarized loop. All lights that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
        // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
        // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
        // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
        // Note that the above is valid only if wave intriniscs are supported.
        uint v_lightListOffset = 0;
        uint v_lightIdx = lightStart;

        while (v_lightListOffset < lightCount)
        {
            v_lightIdx = FetchIndex(lightStart, v_lightListOffset);
            uint s_lightIdx = ScalarizeElementIndex(v_lightIdx, fastPath);
            if (s_lightIdx == -1)
                break;
            
            LightData s_lightData = FetchLight(s_lightIdx);

            // If current scalar and vector light index match, we process the light. The v_lightListOffset for current thread is increased.
            // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
            // end up with a unique v_lightIdx value that is smaller than s_lightIdx hence being stuck in a loop. All the active lanes will not have this problem.
            if (s_lightIdx >= v_lightIdx)
            {
                v_lightListOffset++;
                if (IsMatchingLightLayer(s_lightData.lightLayers, renderLayer))
                {
                    //DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, s_lightData, bsdfData, builtinData);
                    //AccumulateDirectLighting(lighting, aggregateLighting);
                    float3 wi;
                    float4 distances; // {d, d^2, 1/d, d_proj}
                    GetPunctualLightVectors(posInput.positionWS, s_lightData, wi, distances);
                    float shadowP = GetPunctualShadowAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, s_lightData.shadowIndex, wi, distances.x, s_lightData.lightType == GPULIGHTTYPE_POINT, s_lightData.lightType != GPULIGHTTYPE_PROJECTOR_BOX);
                    shadow = min(shadow, shadowP);
                    shadowCount += 1.0f;
                }
            }
        }
    }

    //if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        uint lightCount, lightStart;

    #ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        GetCountAndStart(posInput, LIGHTCATEGORY_AREA, lightStart, lightCount);
    #else
        lightCount = _AreaLightCount;
        lightStart = _PunctualLightCount;
    #endif

        // COMPILER BEHAVIOR WARNING!
        // If rectangle lights are before line lights, the compiler will duplicate light matrices in VGPR because they are used differently between the two types of lights.
        // By keeping line lights first we avoid this behavior and save substantial register pressure.
        // TODO: This is based on the current Lit.shader and can be different for any other way of implementing area lights, how to be generic and ensure performance ?

        uint i;
        if (lightCount > 0)
        {
            i = 0;

            uint      last      = lightCount - 1;
            LightData lightData = FetchLight(lightStart, i);

            while (i <= last && lightData.lightType == GPULIGHTTYPE_TUBE)
            {
                lightData = FetchLight(lightStart, min(++i, last));
            }

            while (i <= last) // GPULIGHTTYPE_RECTANGLE
            {
                lightData.lightType = GPULIGHTTYPE_RECTANGLE; // Enforce constant propagation

                if (IsMatchingLightLayer(lightData.lightLayers, renderLayer))
                {
                    float shadowA = GetAreaLightAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, lightData.shadowIndex, normalize(lightData.positionRWS), length(lightData.positionRWS));
                    shadow = min(shadow, shadowA);
                    shadowCount += 1.0f;
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }
        }
    }
    //if (shadowCount > 0)
    //    shadow /= shadowCount;
    //else
    //    shadow = 1.0f;
}

float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // Not lit here (but emissive is allowed)
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    HDShadowContext shadowContext = InitShadowContext();
    float shadow;
    float3 normalWS = normalize(packedInput.vmesh.interpolators1);
    ShadowLoopEverage(shadowContext, posInput, normalWS, 0xFFFFFFFF, 0xFFFFFFFF, shadow);

    float2 shadowMatteColorMapUv = TRANSFORM_TEX(input.texCoord0.xy, _ShadowTintMap);
    float3 shadowTint = SAMPLE_TEXTURE2D(_ShadowTintMap, sampler_ShadowTintMap, shadowMatteColorMapUv).rgb * _ShadowTint.rgb;
    float shadowTintAlpha = SAMPLE_TEXTURE2D(_ShadowTintMap, sampler_ShadowTintMap, shadowMatteColorMapUv).a * _ShadowTint.a;

    //shadow = (1.0f - shadow)*(1.0f - shadowTintAlpha);
    //shadow *= shadowTintAlpha;

    float3 shadowColor = ComputeShadowColor(shadow, shadowTint.rgb);

    // Note: we must not access bsdfData in shader pass, but for unlit we make an exception and assume it should have a color field
    //float4 outColor = ApplyBlendMode(shadowColor/**bsdfData.color*/ + builtinData.emissiveColor*GetCurrentExposureMultiplier(), builtinData.opacity);
    //outColor = EvaluateAtmosphericScattering(posInput, V, outColor);

#ifdef DEBUG_DISPLAY
    // Same code in ShaderPassForward.shader
    // Reminder: _DebugViewMaterialArray[i]
    //   i==0 -> the size used in the buffer
    //   i>0  -> the index used (0 value means nothing)
    // The index stored in this buffer could either be
    //   - a gBufferIndex (always stored in _DebugViewMaterialArray[1] as only one supported)
    //   - a property index which is different for each kind of material even if reflecting the same thing (see MaterialSharedProperty)
    int bufferSize = int(_DebugViewMaterialArray[0]);
    // Loop through the whole buffer
    // Works because GetSurfaceDataDebug will do nothing if the index is not a known one
    for (int index = 1; index <= bufferSize; index++)
    {
        int indexMaterialProperty = int(_DebugViewMaterialArray[index]);
        if (indexMaterialProperty != 0)
        {
            float3 result = float3(1.0, 0.0, 1.0);
            bool needLinearToSRGB = false;

            GetPropertiesDataDebug(indexMaterialProperty, result, needLinearToSRGB);
            GetVaryingsDataDebug(indexMaterialProperty, input, result, needLinearToSRGB);
            GetBuiltinDataDebug(indexMaterialProperty, builtinData, result, needLinearToSRGB);
            GetSurfaceDataDebug(indexMaterialProperty, surfaceData, result, needLinearToSRGB);
            GetBSDFDataDebug(indexMaterialProperty, bsdfData, result, needLinearToSRGB);
            
            // TEMP!
            // For now, the final blit in the backbuffer performs an sRGB write
            // So in the meantime we apply the inverse transform to linear data to compensate.
            if (!needLinearToSRGB)
                result = SRGBToLinear(max(0, result));

            outColor = float4(result, 1.0);
        }
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
    {
        float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
        outColor = result;
    }
#endif

    return float4(shadowColor.rgb*(1 - shadow)*(shadowTintAlpha), (1 - shadow)*(shadowTintAlpha));
    //return float4(shadowColor.rgb, 1);
}

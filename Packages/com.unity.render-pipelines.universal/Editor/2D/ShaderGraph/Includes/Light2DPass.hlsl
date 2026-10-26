// Post-graph include for UniversalLight2DSubTarget.
//
// Pipeline contract:
//   * User graph outputs a screen-space, unshaded spatial contribution as Color + Alpha.
//   * Vertex Position drives BOTH TransformObjectToHClip and TransformObjectToWorld
//     inside ShaderGraph's BuildVaryings; the OS-position invariant is enforced by port shape.
//   * Everything below the graph output is pipeline-owned.

// Global declarations (_ShadowTex, _NormalMap, _CameraRenderingLayersTexture,
// _InverseHDREmulationScale) are in Light2DGlobals.hlsl (pre-graph) so they are
// visible to node functions emitted before this post-graph include.

PackedVaryings vert(Attributes input)
{
    // Match Hidden/Light2D's vert_shape: extrude the outer-perimeter vertices of
    // shape lights outward by _L2D_FALLOFF_DISTANCE * color.rg so the falloff ring
    // has visible geometry. Freeform and Sprite meshes bake color.rg = 0, so this
    // is a no-op there; Parametric outer-perimeter vertices carry the extrusion
    // direction in color.rg (see LightUtility.GenerateParametricMesh). Point (3) is
    // left alone because Hidden/Light2D's vert_point does not extrude — Point
    // falloff is computed in the fragment shader from the world-space lookup.
    // Provider (5) is left alone so user-supplied meshes are not perturbed.
    if (_L2D_LIGHT_TYPE == 0 || _L2D_LIGHT_TYPE == 1 || _L2D_LIGHT_TYPE == 2)
    {
        input.positionOS.xy += _L2D_FALLOFF_DISTANCE * input.color.rg;
    }

    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

FragmentOutput frag(PackedVaryings packedInput)
{
    Varyings unpacked = UnpackVaryings(packedInput);

#if _LIGHT_LAYERS && !USE_VOLUMETRIC && !defined(SHADERGRAPH_PREVIEW_MAIN) && !defined(SHADERGRAPH_PREVIEW)
    {
        uint2 pixelCoords = uint2(unpacked.positionCS.xy);
        uint lightLayers = _L2D_LIGHT_RENDERING_LAYER;
        uint meshRenderingLayers = LOAD_TEXTURE2D_X(_CameraRenderingLayersTexture, pixelCoords);
        if (!IsMatchingLightLayer(lightLayers, meshRenderingLayers))
            discard;
    }
#endif

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);
    // Gate on LIGHT2D_VOLUMETRIC_PASS, a Predefined keyword injected only into the
    // volumetric PassDescriptor's defines. This is compile-time per pass and independent
    // of the USE_VOLUMETRIC multi_compile keyword (which variants both passes). Each
    // pass's SurfaceDescription contains only its own Color block — driven by the split
    // validPixelBlocks masks — so the gate must match that split exactly. Alpha is not
    // authored by the graph; it starts at 1 and the falloff / attenuation blocks below
    // overwrite (or multiply into) it as required by each light type / blend mode.
#if defined(LIGHT2D_VOLUMETRIC_PASS)
    half4 lightColor = half4(surfaceDescription.Light2DVolumetricColor, 1.0h);
#else
    half4 lightColor = half4(surfaceDescription.Light2DColor, 1.0h);
#endif

    lightColor.rgb *= _L2D_COLOR.rgb;

    // Apply the built-in shape/point falloff so a graph outputting pure white on a
    // Freeform, Parametric or Point (Spot) light renders identically to
    // Hidden/Light2D. Sprite (2) and Provider (5) fall through — those light types
    // have no built-in falloff convention and the graph is responsible.
    //
    // Non-additive, non-volumetric writes the falloff into lightColor.a directly
    // (mirroring Hidden/Light2D exactly — the built-in shader also discards vertex
    // alpha here and replaces it with the falloff sample). Under the runtime blend
    // SrcAlpha,OneMinusSrcAlpha, that alpha is what modulates the RGB contribution
    // into the light buffer, giving the visible falloff. Additive and volumetric
    // use the multiplicative form because their blend modes consume RGB directly.
    //
    // `falloffAttenuation` retains the scalar in [0..1] so the shadow block below
    // can (optionally) attenuate its shadow-color add by the same falloff — otherwise
    // a light with a large falloff radius paints shadow color outside its visible
    // lit area. Defaults to 1 for Sprite (2) and Provider (5), which have no
    // built-in falloff and expect the graph to author it.
    half falloffAttenuation = 1.0h;
    if (_L2D_LIGHT_TYPE == 0 || _L2D_LIGHT_TYPE == 1)
    {
        // Freeform / Parametric: falloff U coord is the interpolated vertex color
        // alpha (1 at the inner path, 0 at the outer perimeter, exactly as baked
        // by LightUtility.GenerateShapeMesh / GenerateParametricMesh).
        half falloff = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup,
            float2(unpacked.color.a, _L2D_FALLOFF_INTENSITY)).r;
        falloffAttenuation = falloff;

    #if USE_ADDITIVE_BLENDING
        lightColor *= falloff;
    #elif USE_VOLUMETRIC
        // Built-in shape volumetric writes `lightColor.a = i.color.a * falloff` in the
        // fragment, where `i.color.a` was pre-baked at vertex to `_L2D_COLOR.a *
        // _L2D_VOLUME_OPACITY` and `lightColor.rgb` remains `_L2D_COLOR.rgb *
        // _InverseHDREmulationScale` — no further scaling. Under the SrcAlpha,One
        // volumetric blend the light-buffer contribution is `src.rgb * src.a`, so the
        // volume opacity and color alpha must land on the alpha channel only. Match
        // that exactly here: apply the full `_L2D_COLOR.a * _L2D_VOLUME_OPACITY *
        // falloff` product to alpha directly, and keep RGB unscaled by volume opacity.
        // The trailing `lightColor *= _L2D_VOLUME_OPACITY` below is gated to Point
        // lights so shape lights don't double-scale.
        lightColor.a = _L2D_COLOR.a * _L2D_VOLUME_OPACITY * falloff;
    #else
        lightColor.a = falloff;
    #endif
    }
    else if (_L2D_LIGHT_TYPE == 2)
    {
        // Sprite: mirrors Hidden/Light2D's frag_shape TYPE 2 branch (Shaders/2D/Light2D.shader:178-186).
        // Sample the cookie sprite at the mesh UV (Attributes.uv0 → Varyings.texCoord0)
        // and modulate both RGB and alpha by it — cookie shape and alpha determine
        // visibility, since Sprite lights have no built-in falloff of their own.
        //
        // The initial alpha must match Hidden/Light2D's `lightColor.a = i.color.a` before the
        // cookie multiply: vertex color alpha for the non-volumetric path (vert_shape_shared
        // line 84: `o.color.a = a.color.a`) and `_L2D_COLOR.a * _L2D_VOLUME_OPACITY` for
        // volumetric (line 86). Without this override, alpha would carry the initial 1.0h
        // set above at line 59, and Sprite meshes with non-uniform authored alpha (or
        // any USE_VOLUMETRIC pass) would diverge from Hidden/Light2D by that factor.
    #if USE_VOLUMETRIC
        lightColor.a = _L2D_COLOR.a * _L2D_VOLUME_OPACITY;
    #else
        lightColor.a = unpacked.color.a;
    #endif
        half4 cookie = SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, unpacked.texCoord0.xy);
    #if USE_ADDITIVE_BLENDING
        // Additive Sprite: cookie.a acts as a premultiplied-style coverage so
        // transparent regions of the cookie don't leak _L2D_COLOR into the buffer
        // under the One,One blend. Matches Hidden/Light2D's `lightColor *= cookie * cookie.a`.
        lightColor *= cookie * cookie.a;
    #else
        lightColor *= cookie;
    #endif
    }
    else if (_L2D_LIGHT_TYPE == 3)
    {
        // Point: mirrors Hidden/Light2D's frag_point exactly. Sample _LightLookup
        // (r=distance, g=angle) at the light-space UV derived from world position
        // via _L2D_INVMATRIX; apply inner-radius and spot-cone attenuation; then
        // remap through _FalloffLookup with the light's authored falloff intensity.
        float4 lightSpacePos = mul(_L2D_INVMATRIX, float4(unpacked.positionWS.xyz, 1));
        float halfTexelOffset = 0.5 * _LightLookup_TexelSize.x;
        float2 lookupUV = 0.5 * (lightSpacePos.xy + 1) + halfTexelOffset;

        half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, lookupUV);
        half innerAtten = saturate(_L2D_INNER_RADIUS_MULT * lookupValue.r);
        half isFullSpotlight = _L2D_INNER_ANGLE == 1.0f;
        half spotAtten = saturate((_L2D_OUTER_ANGLE - lookupValue.g + isFullSpotlight)
                                 * (1.0f / (_L2D_OUTER_ANGLE - _L2D_INNER_ANGLE)));
        half attenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup,
            float2(innerAtten * spotAtten, _L2D_FALLOFF_INTENSITY)).r;
        falloffAttenuation = attenuation;

    #if USE_POINT_LIGHT_COOKIES
        // Mirror Hidden/Light2D's frag_point cookie behavior: sample the cookie at
        // the same light-space UV used for the distance/angle lookup and modulate
        // the light output. The built-in shader does `lightColor = cookie * _L2D_COLOR`
        // because it starts from a pure `_L2D_COLOR`; our lightColor is already
        // `graph_output * _L2D_COLOR.rgb`, so multiplying preserves the graph's
        // authored contribution while adding the cookie mask on top. The property
        // block binds _PointLightCookieTex per-draw when the Light2D has a cookie
        // sprite assigned (RendererLighting.SetCookieShaderProperties).
        half4 cookieColor = SAMPLE_TEXTURE2D(_PointLightCookieTex, sampler_PointLightCookieTex, lookupUV);
        lightColor *= cookieColor;
    #endif

    #if USE_ADDITIVE_BLENDING || USE_VOLUMETRIC
        lightColor *= attenuation;
    #else
        // Built-in frag_point non-additive non-volumetric: lightColor.a = attenuation.
        // Written directly (not multiplied) so a graph outputting Alpha != 1 does
        // not silently suppress the falloff.
        lightColor.a = attenuation;
    #endif
    }

    float4 clipPos = TransformWorldToHClip(unpacked.positionWS.xyz);
    float2 screenUV = ComputeNormalizedDeviceCoordinates(clipPos.xyz / clipPos.w);

#if USE_NORMAL_MAP && !USE_VOLUMETRIC
    {
        half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, screenUV);
        half3 normalUnpacked = UnpackNormalRGBNoScale(normalSample);
        half3 planeNormal = -GetViewForwardDir();
        half3 projLightPos = _L2D_POSITION.xyz - (dot(_L2D_POSITION.xyz - unpacked.positionWS.xyz, planeNormal) - _L2D_POSITION.w) * planeNormal;
        half3 dirToLight = normalize(projLightPos - unpacked.positionWS.xyz);
        lightColor = lightColor * saturate(dot(dirToLight, normalUnpacked));
    }
#endif

    // Vertex color modulates the light output for Provider lights only. Applied here
    // — before the shadow block adds the Light2DShadowColor contribution — so that
    // the Apply-Vertex-Color-To-Shadows toggle below can independently choose whether
    // the shadow-color add is also vertex-color-modulated. (For Provider, this ordering
    // is mathematically equivalent to applying vertex color at the very end so long
    // as the shadow-color add is treated symmetrically.)
    //
    // Gated on the Provider light type (5) because the built-in Freeform / Parametric
    // / Point / Sprite mesh generators bake Color(0, 0, batchColor, alpha) into
    // vertex color — the RGB channels carry internal batching/falloff metadata, not
    // a user-authored tint. Multiplying by that vertex color unconditionally zeroes
    // the red and green channels of the light output on all built-in light types,
    // leaving only the blue batch-index channel and producing an unwanted blue tint.
    // Only Provider meshes carry user-authored vertex colors that make sense as a
    // per-vertex tint.
    if (_L2D_LIGHT_TYPE == 5)
        lightColor *= unpacked.color;

    {
        half intensity = _L2D_SHADOW_INTENSITY;
        if (intensity < 1)
        {
            half4 shadowTex = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, screenUV);
            half shadowStrength = 1 - max(shadowTex.r, shadowTex.g * (1 - shadowTex.b));
            // appliedFactor = the multiplier the shadow step applies to the light:
            //   1 where fully lit, `intensity` where fully shadowed.
            half appliedFactor = shadowStrength + intensity * (1 - shadowStrength);
            lightColor.rgb = lightColor.rgb * appliedFactor;
            // Shadow Color is mixed in by (1 - appliedFactor), so it appears only where
            // the shadow attenuates the light and scales with shadow strength. Only the
            // non-volumetric pass's SurfaceDescription carries Light2DShadowColor (see
            // FragmentVolumetric block mask), so gate on LIGHT2D_VOLUMETRIC_PASS.
        #ifndef LIGHT2D_VOLUMETRIC_PASS
            half3 shadowContribution = surfaceDescription.Light2DShadowColor;
            // Apply the same falloff the light uses so shadow color stays inside the
            // visible lit area — without this, a light with a wide falloff radius paints
            // shadow color outside its visible edge. `falloffAttenuation` defaults to 1
            // for Sprite (2) and Provider (5), so those light types are unaffected.
            shadowContribution *= falloffAttenuation;
            // Apply Provider vertex color to the shadow-color add too, matching the
            // vertex-color multiply applied to lightColor above. Gated on Provider (5)
            // because the built-in Freeform / Parametric / Point / Sprite mesh generators
            // bake batching metadata into vertex RGB and would tint the shadow blue.
            if (_L2D_LIGHT_TYPE == 5)
                shadowContribution *= unpacked.color.rgb;
            lightColor.rgb += shadowContribution * (1 - appliedFactor);
        #endif
        }
    }

#if USE_VOLUMETRIC
    // Point-only: built-in frag_point applies `lightColor *= _L2D_VOLUME_OPACITY`
    // to the full rgba after the attenuation multiply, so both RGB and A carry the
    // volume opacity factor. Shape volumetric (freeform / parametric) already had
    // `_L2D_VOLUME_OPACITY` baked into `lightColor.a` in the branch above and its
    // RGB is intentionally NOT scaled by volume opacity — running this multiply
    // for shape lights would double-scale alpha and darken RGB versus built-in.
    if (_L2D_LIGHT_TYPE == 3)
        lightColor *= _L2D_VOLUME_OPACITY;
#endif

    return ToFragmentOutput(lightColor * _InverseHDREmulationScale);
}

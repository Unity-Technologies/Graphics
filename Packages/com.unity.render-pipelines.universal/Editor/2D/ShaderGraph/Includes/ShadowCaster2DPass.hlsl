// Entry points for all five passes of the ShadowCaster2D SubTarget.
//
// URP resolves a 2D shadow pass by NAME, and the four ShadowCastingOptions between them need all
// five, so every generated shader declares all five regardless of what the graph does. See
// ShadowRendering.GetMissingShadowPasses and the material-override handoff for why a material
// validated against the current casting option breaks silently the moment that option changes.
//
// Caster coverage is REPRODUCED from Hidden/Shadow2D rather than authored: SHADOW_SPRITE_CASTER
// selects sprite alpha coverage (with optional 2D-Animation skinning) versus the geometry variant's
// constant 1, exactly as Shadow2DCasterVertex.hlsl does. The graph contributes one thing on top --
// SurfaceDescription.Alpha, an opacity applied to the visible shadow.
//
// The passes split two ways, both selected by a per-pass define rather than a keyword so that neither
// branch variants the other four passes:
//
//   SHADOWCASTER2D_PROJECTED_PASS   ProjectedSelf / ProjectedUnshadow. Consume the 14-float
//                                   Unity.SoftShadow payload and run the per-vertex geometry program.
//                                   The vertex position comes entirely from SoftShadowProject, so the
//                                   standard BuildVaryings path is bypassed.
//   SHADOWCASTER2D_ALPHA_CLIP_PASS  UnshadowMark / UnshadowUnmark. Alpha-test the caster's coverage
//                                   against _ShadowAlphaCutoff before writing stencil or B.

// Where the graph's Alpha applies, and where it must not.
//
// The light composites the shadow buffer as (LightingUtility.hlsl):
//     shadowIntensity = 1 - max(R, G * (1 - B))
//
// R and G are the visible shadow; B is the caster's own body mask, which SUBTRACTS the projected
// shadow from the caster wherever it is opaque. Scaling R and G but not B gives
//     max(R*a, G*a*(1-B)) == a * max(R, G*(1-B))
// so the composite is exactly LINEAR in Alpha -- the visible shadow scales uniformly for any caster
// opacity. Scaling B would break that identity and make a fading shadow DARKER inside the caster's
// own footprint, because B enters as (1 - B).
//
// The alpha clip also tests raw coverage, never coverage * Alpha. Feeding Alpha into the cutoff would
// let a graph shrink the stencil mark, and ProjectedSelf (Comp NotEqual 1) would then start drawing
// across the caster's own silhouette -- fading a shadow would break the four-phase interlock rather
// than just fade it.

// Caster coverage in [0,1]: sprite-tinted alpha for the sprite variant, constant 1 for geometry.
// Mirrors Shadow2DCasterVertex.hlsl's GetShadowCasterAlpha.
half GetShadowCasterCoverage(Varyings i)
{
#if defined(SHADOW_SPRITE_CASTER)
    return (half)((i.color * tex2D(_MainTex, i.texCoord0.xy)).a);
#else
    return 1;
#endif
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);

#if defined(SHADOWCASTER2D_PROJECTED_PASS)
    // The payload is repacked out of ShaderGraph's Attributes rather than read through the soft shadow
    // header's own struct, because ShaderGraph owns the name `Attributes` in every pass. The semantics
    // line up exactly with what SoftShadowGeometryGenerator emits:
    //   POSITION Float32x3 -> positionOS, TANGENT Float32x4 -> tangentOS,
    //   TEXCOORD1 Float32x3 -> uv1.xyz,   TEXCOORD2 Float32x4 -> uv2
    // uv1 is declared float4 by ShaderGraph against a 3-component stream; only .xyz is read.
    SoftShadowPayload payload;
    payload.positionOS = input.positionOS;
    payload.tangent    = input.tangentOS;
    payload.next       = input.uv1.xyz;
    payload.neighbor2  = input.uv2;

    // ALWAYS from the graph, never conditionally.
    //
    // An earlier version only read the graph when the port was CONNECTED, falling back to a
    // _ShadowLength material property otherwise. That made typing a value into the port's inline field
    // do nothing at all -- the commonest way to author a ShaderGraph port, silently ignored. It was
    // also a pointless optimisation: with nothing wired the vertex graph is a constant, so the five
    // calls below fold away.
    SoftShadowDisplacements disp;
    // The projection has to be the same function of a point regardless of which vertex is asking:
    // hard(prev) computed at v must equal hard(prev) computed at prev, or adjacent edges disagree and
    // the geometry tears at every shared vertex. A ShaderGraph port is evaluated once per vertex, so
    // the only way to honour that is to evaluate it once per POINT -- swapping positionOS in a copy of
    // Attributes, which is what BuildVertexDescriptionInputs derives ObjectSpacePosition from.
    //
    // Consequence worth knowing: the chain feeding this port may read ObjectSpacePosition and uniforms,
    // but nothing else per-vertex. tangentOS / uv1 / uv2 carry the payload here, not geometry, so a
    // node reading them would give each of the five a different answer for reasons unrelated to
    // position -- and the tear comes back.
    // Both vertex ports come out of ONE evaluation per point, so Position displaces the point while
    // ShadowDisplacement is a function of the UNDISPLACED point. Five calls rather than ten.
    //
    // The consequence is real and worth knowing: the throw is computed for where the vertex WAS, then
    // applied to where it IS. Any projection stated as a guarantee about where the far end LANDS is
    // therefore only exact while Position is unwired. A radial reach onto a circle of radius R is the
    // clearest case --
    //     d = (R - |p - light|) * dir(p),  so  |P - light| = |p' - light| + R - |p - light|
    // which is R only when p' == p. Displace the caster and the far end stops landing on the circle, by
    // exactly the amount the displacement changed the point's distance from the light.
    //
    // Deliberately not fixed here. It is not unstable -- the throw stays a pure function of the
    // original point, so all five neighbours agree and nothing tears -- and the graph can compensate
    // exactly, because BOTH ports see the same input point:
    //
    //     Position            = p + q(p)
    //     Shadow Displacement = f(p + q(p))     <- same q(p) reapplied
    //
    // Authoring q(p) as a Subgraph keeps the two chains from drifting apart. Evaluating twice per point
    // would remove the need, at ten vertex-graph evaluations per vertex on already-dense geometry.
    //
    // Only the .xy halves are overwritten. tangent.xy carries role and fanParam, and next.z carries
    // windingSign -- none of which are positions.
    //
    // Evaluated BEFORE SoftShadowPrepare, deliberately. Prepare multiplies the payload by
    // _ShadowModelScale to reach the space the projection runs in, but unity_ObjectToWorld for this draw
    // is shadowCaster.transform.localToWorldMatrix -- which already includes that scale. Evaluating
    // after Prepare would therefore make the stock Position (World) node apply scale TWICE, and Position
    // (Object) would report the scaled point rather than positionOS. Going first keeps both nodes
    // conventional, which is what makes a world-space displacement -- caster height against the
    // Active Shadow2D Light node's world position -- come out right.
    Attributes pointAttributes = input;
    float2 pV, pP, pN, pP2, pN2;

    // Softness is harvested unevenly, because the two halves reach the geometry through different
    // paths. Side is the fin's wedge, which is v's own geometry, so it is taken from ONE evaluation.
    // Back is the band width W, and the fillet budget measures a neighbour's W against the line that
    // neighbour will itself sit on, so it is taken from THREE -- but not five, since SoftFarLimit uses
    // one wFar for both of the far vertex's offsets. The prev2 / next2 evaluations still compute both
    // ports; the results are dropped and the compiler removes them.
    //
    // SCALES on the light's angle, not angles: unwired ports are constant 1 and reproduce the light's
    // own Shadow Softness exactly, so the artist-facing slider keeps working.
    //
    // Saturated to [0, 1], so a port can only narrow the light's penumbra, never widen it.
    //
    // The reason is that the range above 1 was never as wide as it looked. SoftShadowVert clamps the
    // PRODUCT to _SoftShadowMaxAngle (15 degrees), and _SoftShadowAngle is already shadowSoftness * 15,
    // so the effective angle is min(shadowSoftness * scale, 1) * 15 and a scale stops doing anything at
    // 1 / shadowSoftness. That ceiling MOVED with the light's own slider -- 1.0 on a light at full
    // softness, 3.3 on one at 0.3 -- so the same authored number meant different things per light and
    // went inert at a point the graph could not see. Saturating here makes the port mean one thing
    // everywhere: a fraction of whatever the light is set to.
    //
    // The cost is reach on lights below full softness, and it is accepted deliberately: 15 degrees is
    // the validated envelope AND the fan resolution baked into the mesh, so the reach being given up
    // was never usable without a tessellation story anyway. If that ceiling is ever raised, revisit
    // this -- the clamp is the cheap half of the change.
    SoftShadowSoftness softness;

    pointAttributes.positionOS.xy = payload.positionOS.xy;
    { VertexDescription d = BuildVertexDescription(pointAttributes); pV  = d.Position.xy; disp.v     = d.ShadowDisplacement; softness.side = _SoftShadowAngle * saturate(d.ShadowSideSoftness); softness.back     = _SoftShadowAngle * saturate(d.ShadowBackSoftness); }
    pointAttributes.positionOS.xy = payload.tangent.zw;
    { VertexDescription d = BuildVertexDescription(pointAttributes); pP  = d.Position.xy; disp.prev  = d.ShadowDisplacement; softness.backPrev = _SoftShadowAngle * saturate(d.ShadowBackSoftness); }
    pointAttributes.positionOS.xy = payload.next.xy;
    { VertexDescription d = BuildVertexDescription(pointAttributes); pN  = d.Position.xy; disp.next  = d.ShadowDisplacement; softness.backNext = _SoftShadowAngle * saturate(d.ShadowBackSoftness); }
    pointAttributes.positionOS.xy = payload.neighbor2.xy;
    { VertexDescription d = BuildVertexDescription(pointAttributes); pP2 = d.Position.xy; disp.prev2 = d.ShadowDisplacement; }
    pointAttributes.positionOS.xy = payload.neighbor2.zw;
    { VertexDescription d = BuildVertexDescription(pointAttributes); pN2 = d.Position.xy; disp.next2 = d.ShadowDisplacement; }

    payload.positionOS.xy = pV;
    payload.tangent.zw    = pP;
    payload.next.xy       = pN;
    payload.neighbor2.xy  = pP2;
    payload.neighbor2.zw  = pN2;

    // No clamp, and none is possible.
    //
    // The scalar port was clamped to >= 0 here, because a negative DISTANCE threw the point back
    // through the caster toward the light and inverted the shadow -- a value with no legitimate
    // meaning. A displacement has no such value: every direction is one an author might want, and
    // "towards the light" is exactly what a shadow leaning back over its own caster needs. The
    // degenerate-to-nothing safety net the clamp provided is therefore gone, and a graph that throws
    // its casters into themselves will render that, not a blank.
    //
    // The projection stays self-consistent either way -- the back-facing tests read the same
    // displacements, so they agree with whatever field is authored -- but a shadow folded onto its own
    // caster meets the B-channel unshadow interlock, which assumes the projected shadow leaves the
    // caster. That composite is outside what this file can guard.

    // The displacement is read in the CASTER'S OWN FRAME and needs no transform at all.
    //
    // That frame is the space the projection runs in, and it reaches world through _ShadowModelMatrix,
    // which ShadowCaster2D.CacheValues builds as TRS with only a +/-1 flip for scale -- an isometry. So
    // a displacement here is a world-length offset along the caster's local axes, and (0, -2) means
    // "two units along this sprite's own down", not "two units along world -Y".
    //
    // This is what makes a rigid transform of an entire scene -- camera, lights and casters together --
    // leave the rendered shadow unchanged: the caster's frame rotates with the scene, so a constant
    // typed into the port rotates with it too. Reading the port as WORLD instead cannot have that
    // property, because a graph literal does not rotate while _ShadowModelInvMatrix does, and the
    // shadow swings by the scene's own rotation. A graph that computes its displacement from world
    // quantities -- Position (World), the Active Shadow2D Light node -- must therefore convert,
    // either with a Transform (World -> Object) node or by working in this frame to begin with, which
    // is what SoftShadowLightLocal / SoftShadowPointToLocal give a Custom Function node directly.
    //
    // The flip travels with the frame: a flipped sprite mirrors its local axes, so its authored
    // displacement mirrors too, which is the behaviour a flipped sprite's shadow wants.
    float2 light;
    SoftShadowPrepare(payload, light);

    float2 position, shadow;
    SoftShadowVert(payload, disp, softness, position, shadow);

    output.positionCS = SoftShadowToHClip(position);

    // These passes bypass BuildVaryings -- the position comes from SoftShadowProject, not from the
    // graph's vertex stage -- so anything BuildVaryings would normally populate has to be filled here
    // or it stays ZERO. That is not a cosmetic gap: a Voronoi or noise node fed Position (World) returns
    // one constant across the whole shadow, which looks like the node doing nothing rather than like a
    // missing input. Screen Position has the same fault, because ShaderGraph derives it FROM world
    // position (FieldDependencies.cs: ScreenPosition <- WorldSpacePosition).
    //
    // Guarded rather than unconditional so a graph that does not ask for world position pays no
    // interpolator for it. VARYINGS_NEED_POSITION_WS is emitted exactly when Varyings.positionWS is
    // active, which is when the graph requested it.
#if defined(VARYINGS_NEED_POSITION_WS)
    output.positionWS = SoftShadowToWorld(position);
#endif
    // The shading value encoding rides texCoord3, deliberately NOT texCoord0: on these passes
    // Attributes.uv0 is TEXCOORD0, which the SoftShadow generator leaves free, so UV0 reads as a
    // harmless zero. Putting the penumbra there instead would make a UV-driven node in the graph
    // silently sample the wedge coordinate. Handoff v2 section 8 for the encoding itself: .x is signed
    // position across the wedge, .y the width normaliser.
    output.texCoord3 = float4(shadow, 0.0, 0.0);
#else
    // Sprite flip and instancing exactly as the built-in caster does, before the graph's vertex stage
    // sees the position. UNITY_SKINNED_VERTEX_COMPUTE is inside BuildVaryings' own attribute handling
    // for the skinned variant; SetUpSpriteInstanceProperties has to run first so unity_SpriteProps is
    // valid for the flip.
    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    output = BuildVaryings(input);
    #if defined(SHADOW_SPRITE_CASTER)
        // o.color = _Color.a * v.color, matching Shadow2DCasterVertex.hlsl. TRANSFORM_TEX is applied
        // here rather than in the graph because coverage is reproduced, not authored.
        output.texCoord0 = float4(TRANSFORM_TEX(input.uv0.xy, _MainTex), 0.0, 0.0);
        output.color = _Color.a * input.color;
    #endif
#endif

    return PackVaryings(output);
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);
    half alpha = (half)surfaceDescription.Alpha;

#if defined(SHADOWCASTER2D_PROJECTED_PASS)
    // Linear penumbra ramp, matching Test/Shadow2D-SoftShadow rather than Hidden/Shadow2D. The
    // built-in pushes this through _FalloffLookup with _ShadowSoftnessFalloffIntensity; the Python
    // model the geometry is verified against is linear (handoff v2 section 13), so staying linear is
    // what keeps a generated shader comparable to simulation report 29's acceptance sheet.
    float clampedY = clamp(unpacked.texCoord3.y, SOFT_SHADOW_MIN_Y, 1);
    half value = (half)(1.0f - saturate(abs(unpacked.texCoord3.x) / clampedY));
    value *= alpha;
    return half4(value, value, value, value);
#else
    half coverage = GetShadowCasterCoverage(unpacked);

  #if defined(SHADOWCASTER2D_ALPHA_CLIP_PASS)
    // Raw coverage, never coverage * alpha -- see the note at the top of this file. Only the sprite
    // variant is alpha-tested; the geometry variant's coverage is constant 1.
    #if defined(SHADOW_SPRITE_CASTER)
        if (coverage <= _ShadowAlphaCutoff)
            discard;
    #endif

    // UnshadowUnmark writes B, the caster's body mask that removes projected shadow from the caster
    // via (1 - B). It is not a visible shadow contribution, so Alpha does not scale it. UnshadowMark
    // is ColorMask 0 and writes nothing, so this value is discarded for it either way.
    return half4(coverage, coverage, coverage, coverage);
  #else
    // Self: the caster's own self-shadow into R. Visible, so Alpha applies.
    half value = coverage * alpha;
    return half4(value, value, value, value);
  #endif
#endif
}

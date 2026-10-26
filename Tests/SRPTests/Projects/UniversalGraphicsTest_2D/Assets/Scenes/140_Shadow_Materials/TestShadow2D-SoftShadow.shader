// Test fixture for the Unity.SoftShadow geometry generator, and the model for the future
// vertex-only ShaderGraph master node.
//
// A COMPLETE custom shadow shader: it declares all five pass roles URP resolves by name, so a
// Material using it is a valid ShadowCaster2D.material override and appears in the filtered picker.
// All five are needed for the four ShadowCastingOptions between them -- SelfShadow draws Self,
// CastAndSelfShadow adds ProjectedSelf, CastShadow draws the other four, NoShadow draws the two
// unshadow passes -- and ShadowCastingOptions is mutable at runtime, so a material that satisfies
// only the current option would break silently the moment it changed.
//
// The split:
//   - the three CASTER passes are lifted verbatim from Hidden/Shadow2D. They read POSITION alone,
//     so they work off the generator's interior fill, and they are what makes the four-phase stencil
//     handshake work. Not what this fixture is testing.
//   - the two PROJECTED passes are new: they consume the 14-float soft shadow payload and run the
//     per-vertex program that derives the projection, the facing tests, the pole test, the corner
//     classification, the radius budget and the fillet.
//
// It renders visibly differently from Hidden/Shadow2D in two ways, both deliberate. The shadow has a
// finite LENGTH and its far end follows the caster's outline instead of converging on an arc centred
// on the light -- that shortening is how you can tell from the Scene view that the override is
// driving the draw. And the penumbra ramp is LINEAR rather than pushed through _FalloffLookup,
// because the Python model this was ported from is linear (handoff v2 section 13), so keeping it
// linear is what makes the render comparable to simulation report 29's acceptance sheet. A shipping
// version would sample the LUT.
//
// Each pass reproduces the blend / ColorMask / Stencil state of the built-in pass it replaces. That
// contract is not optional -- URP does not set that state from script, so getting it wrong produces
// a wrong-looking shadow rather than a missing one.
Shader "Test/Shadow2D-SoftShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1,1,1,1)

        // Handoff v2 section 3 places both of these per light; nothing in URP supplies them yet, and
        // both are pure shader uniforms -- the mesh depends on neither -- so they live here for now.
        // Promoting them to Light2D later touches only these declarations and whoever sets them.

        // LENGTH: the distance each caster vertex is thrown along its own ray from the light. This
        // replaces the built-in path's shadowRadius / dot(lightDir, avgLightDir) formula, so the
        // shadow's far end follows the outline rather than converging.
        _ShadowLength ("Shadow Length", Range(0.05, 20)) = 2.0

        // R, as a fraction of the derived band width W, so the concave fillet tracks softness rather
        // than fighting it. The radius budget caps it per corner. 0 reproduces square corners exactly.
        _FilletRadiusScale ("Fillet Radius (x band width)", Range(0, 3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"

            // Unity.SoftShadow alone. The projected passes below read the 14-float payload --
            // POSITION, TANGENT, TEXCOORD1, TEXCOORD2 -- that only this generator emits, and they
            // read TANGENT as (role, fanParam, prevPt.xy). Against a Unity.Legacy mesh TEXCOORD1
            // and TEXCOORD2 are absent, so next / prev2 / next2 / windingSign all arrive as zero,
            // and a windingSign of zero degenerates every facing test.
            //
            // This Material is therefore only usable in a project whose 2D Shadow Geometry version
            // is Enhanced. The tag no longer switches a caster onto anything -- the version is a
            // project setting -- but it still records what this shader can read, which is what makes
            // the mismatch reportable rather than quiet.
            "Shadow2DGenerators" = "Unity.SoftShadow"
        }

        Cull   Off
        ZWrite Off
        ZTest  Always

        // No HLSLINCLUDE block, for the same reason Hidden/Shadow2D has none: Shadow2DCasterVertex.hlsl
        // and SoftShadowProjectVertex.hlsl each declare their own Attributes / Varyings with different
        // members, so they cannot share SubShader scope. Keeping the pragmas per-pass also stops the
        // caster keywords multiplying across the projected passes and vice versa.

        // ---- Caster passes: verbatim from Hidden/Shadow2D ----

        Pass
        {
            Name "Self"

            BlendOp Max
            Blend   One One
            ColorMask R

            HLSLPROGRAM
            #pragma vertex   ShadowCasterVert
            #pragma fragment frag
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ SHADOW_SPRITE_CASTER
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DCasterVertex.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                half a = GetShadowCasterAlpha(i);
                return half4(a, a, a, a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "UnshadowMark"

            Stencil
            {
                Ref       1
                Comp      Always
                Pass      Replace
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowCasterVert
            #pragma fragment frag
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ SHADOW_SPRITE_CASTER
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DCasterVertex.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                half a = GetShadowCasterAlpha(i);
                DiscardShadowCasterIfBelowCutoff(a);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "UnshadowUnmark"

            Stencil
            {
                Ref       0
                Comp      Always
                Pass      Replace
            }

            Blend   One One
            BlendOp Add
            ColorMask B

            HLSLPROGRAM
            #pragma vertex   ShadowCasterVert
            #pragma fragment frag
            #pragma multi_compile _ SKINNED_SPRITE
            #pragma multi_compile _ SHADOW_SPRITE_CASTER
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DCasterVertex.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                half a = GetShadowCasterAlpha(i);
                DiscardShadowCasterIfBelowCutoff(a);
                return half4(a, a, a, a);
            }
            ENDHLSL
        }

        // ---- Projected passes: the soft shadow ----
        //
        // Max-blend so overlapping casters compose as "most shadowed wins". Stencil bit 0 is the
        // caster's unshadow-mark from phase 1: ProjectedSelf draws where it is clear, ProjectedUnshadow
        // where it is set. Simulation report 28 modelled the resulting composite,
        // max(R, G * (1 - B)), against a soft shadow and found it linear in the penumbra value.

        Pass
        {
            Name "ProjectedSelf"

            BlendOp Max
            Blend   One One, One One

            Stencil
            {
                Ref       1
                Comp      NotEqual
                Pass      Keep
            }

            ColorMask R

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ NOT_TRANSFORMABLE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SoftShadowProjectVertex.hlsl"

            Varyings vert(Attributes v) { return ProjectSoftShadow(v); }

            half4 frag(Varyings i) : SV_Target
            {
                float clamppedY = clamp(i.shadow.y, SOFT_SHADOW_MIN_Y, 1);
                half value = (half)(1.0f - saturate(abs(i.shadow.x) / clamppedY));
                return half4(value, value, value, value);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ProjectedUnshadow"

            BlendOp Max
            Blend   One One, One One

            Stencil
            {
                Ref       1
                Comp      Equal
                Pass      Keep
            }

            ColorMask G

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ NOT_TRANSFORMABLE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SoftShadowProjectVertex.hlsl"

            Varyings vert(Attributes v) { return ProjectSoftShadow(v); }

            half4 frag(Varyings i) : SV_Target
            {
                float clamppedY = clamp(i.shadow.y, SOFT_SHADOW_MIN_Y, 1);
                half value = (half)(1.0f - saturate(abs(i.shadow.x) / clamppedY));
                return half4(value, value, value, value);
            }
            ENDHLSL
        }
    }
}

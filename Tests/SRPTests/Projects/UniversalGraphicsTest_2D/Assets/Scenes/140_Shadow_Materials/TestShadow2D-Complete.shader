// Test fixture for ShadowCaster2D.material.
//
// A COMPLETE custom shadow shader: it declares all five pass roles URP resolves by name, so a
// Material using it is a valid override and SHOULD appear in the filtered material picker on
// ShadowCaster2D.
//
// It deliberately renders differently from the built-in Hidden/Shadow2D so that "is my custom
// material actually being used?" is answerable by looking at the Scene view: the projected passes
// skip the falloff-lookup ramp and hard-threshold instead, giving a shadow with a crisp edge and no
// soft penumbra. The caster passes match the built-in exactly, since that is what makes the stencil
// handshake work and is not what this fixture is testing.
//
// Each pass reproduces the blend / ColorMask / Stencil state of the built-in pass it replaces. That
// contract is not optional -- URP does not set that state from script, so getting it wrong here
// produces a wrong-looking shadow rather than a missing one.
Shader "Test/Shadow2D-Complete"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1,1,1,1)

        // Where the hard edge falls, as a fraction of the wedge half-width. 0 fills the whole
        // projected wedge; values near 1 collapse it to the centre line.
        _EdgeThreshold ("Edge Threshold", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"

            // Legacy's vertex layout, because the projected passes below read
            // ShadowProjectVertex.hlsl's POSITION / TANGENT / TEXCOORD0. Unity.SoftShadow does not
            // belong on that line: this shader cannot read its 14-float payload.
            //
            // Which layout a project builds is Shadow2DGeometrySettings.geometryVersion, so the tag
            // no longer switches anything -- it records what this shader can read, which is what
            // makes a mismatch reportable instead of silent.
            "Shadow2DGenerators" = "Unity.Legacy"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        // ---- Caster passes: identical in behaviour to the built-in shader ----

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

        // ---- Projected passes: deliberately hard-edged, so the override is visible ----

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
            // Core.hlsl first: ShadowProjectVertex.hlsl calls GetWorldToHClipMatrix() but declares no
            // includes of its own, so it only compiles for an includer that has already pulled in
            // SpaceTransforms. The built-in shader gets that via Shadow2DProjectedPass.hlsl; a shader
            // including the header directly has to do it here.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"

            float _EdgeThreshold;

            Varyings vert(Attributes v)
            {
                return ProjectShadow(v);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Same wedge coordinate the built-in uses, but thresholded instead of fed through
                // _FalloffLookup -- hence the crisp edge that identifies this shader on screen.
                float clamppedY = clamp(i.shadow.y, MIN_SHADOW_Y, 1);
                float value = 1.0f - saturate(abs(i.shadow.x) / clamppedY);
                half hard = (half)step(_EdgeThreshold, value);
                return half4(hard, hard, hard, hard);
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
            // Core.hlsl first: ShadowProjectVertex.hlsl calls GetWorldToHClipMatrix() but declares no
            // includes of its own, so it only compiles for an includer that has already pulled in
            // SpaceTransforms. The built-in shader gets that via Shadow2DProjectedPass.hlsl; a shader
            // including the header directly has to do it here.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"

            float _EdgeThreshold;

            Varyings vert(Attributes v)
            {
                return ProjectShadow(v);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float clamppedY = clamp(i.shadow.y, MIN_SHADOW_Y, 1);
                float value = 1.0f - saturate(abs(i.shadow.x) / clamppedY);
                half hard = (half)step(_EdgeThreshold, value);
                return half4(hard, hard, hard, hard);
            }
            ENDHLSL
        }
    }
}

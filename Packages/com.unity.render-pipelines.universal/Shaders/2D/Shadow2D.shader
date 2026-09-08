Shader "Hidden/Shadow2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"

            // Which shadow geometry generators this shader's vertex programs can read. A
            // generator's id implies its vertex layout, so naming ids is enough to declare
            // compatibility.
            //
            // Both, because this is the shader every caster falls back to when no custom material
            // is assigned -- so it has to be able to read whichever layout the project builds.
            // SHADOW2D_SOFT_GEOMETRY picks between them on the two projected passes; the tag says
            // "either", the keyword says "this one now". ShadowRendering bakes the keyword onto the
            // cached material from Shadow2DGeometrySettings.activeFormat, which is fixed for the
            // life of a project, so no variant is selected per draw.
            //
            // The space-separated list is what makes this expressible without a tag-format
            // migration -- it has been list-capable from the start for exactly this.
            //
            // Kept in step with Shadow2DGeneratorTag.builtInShadowShaderIds by
            // Shadow2DGeneratorTagTests.TheBuiltInShadowShaderDeclaresItsGeometryGenerators.
            "Shadow2DGenerators" = "Unity.SoftShadow Unity.Legacy"
        }

        // Only state common to all five passes lives here. In particular the projected passes'
        // BlendOp/Blend and Stencil are declared per-pass rather than at SubShader scope: hoisting
        // them would leak onto the caster passes, and Self in particular must have no Stencil block.
        Cull Off
        ZWrite Off
        ZTest Always

        // All five shadow pass roles URP resolves by name live in this one shader, so a single
        // custom Material assigned to ShadowCaster2D.material can satisfy every phase. There is
        // deliberately no HLSLINCLUDE block: Shadow2DCasterVertex.hlsl and
        // Shadow2DProjectedPass.hlsl each declare their own Attributes / Varyings with different
        // members, so they cannot share SubShader scope. Each pass includes only the header it
        // needs, which also keeps the keyword pragmas per-pass -- hoisting them would multiply the
        // caster keywords across the projected passes and vice versa, inflating the variant count.

        // ---- Caster passes: the caster silhouette, drawn from the sprite renderer or the mesh ----

        // Self-shadow pass: writes coverage into R (max-blended).
        // Used by self-shadowing casters (selfShadows == true).
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

        // Unshadow "Mark" pass: sets stencil ref 1 wherever the caster covers.
        // No color writes here; B is written in Unmark, which produces the same
        // final texture because nothing reads or writes B between Mark and
        // Unmark (phases 2/3 only touch R and G).
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

        // Unshadow "Unmark" pass: clears stencil ref 0 and additively writes
        // coverage into B (the "gate G" channel consumed by APPLY_SHADOWS).
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

        // ---- Projected passes: the shadow body extruded away from the light ----
        //
        // Max-blend so overlapping casters compose as "most shadowed wins". Stencil bit 0 is the
        // caster's unshadow-mark, set in phase 1 by non-self-shadowing casters: ProjectedSelf draws
        // where it is clear, ProjectedUnshadow where it is set.

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

            // Draw the shadow
            ColorMask R

            HLSLPROGRAM
            #pragma vertex   shadow_vert
            #pragma fragment shadow_frag
            #pragma multi_compile _ NOT_TRANSFORMABLE
            #pragma multi_compile _ SHADOW2D_SOFT_GEOMETRY
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DProjectedPass.hlsl"
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

            // Draw the unshadow
            ColorMask G

            HLSLPROGRAM
            #pragma vertex   shadow_vert
            #pragma fragment shadow_frag
            #pragma multi_compile _ NOT_TRANSFORMABLE
            #pragma multi_compile _ SHADOW2D_SOFT_GEOMETRY
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DProjectedPass.hlsl"
            ENDHLSL
        }
    }
}

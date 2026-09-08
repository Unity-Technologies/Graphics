// Test fixture for ShadowCaster2D.material -- the NEGATIVE control.
//
// This shader declares only the "Self" pass. It is therefore an INVALID override, and exists so the
// picker filter can be shown to actually filter: without a material that must be excluded, a picker
// listing everything is indistinguishable from a picker filtering correctly.
//
// Expected behaviour with TestShadow2D-Incomplete.mat:
//   - it must NOT appear in the filtered picker on ShadowCaster2D.material
//   - assigning it anyway (drag-and-drop onto the field, which bypasses the picker) must raise the
//     inspector's compatibility error, naming the four missing passes: UnshadowMark,
//     UnshadowUnmark, ProjectedSelf, ProjectedUnshadow
//   - at runtime the caster then draws only its self-shadow; the projected body and the unshadow
//     stencil handshake are skipped, which is the silent failure the error exists to surface
//
// Do not "fix" this shader by adding the missing passes -- being incomplete is its entire purpose.
Shader "Test/Shadow2D-Incomplete"
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

            // Tagged even though this fixture is the negative control for pass NAMES: the two
            // checks are independent, and leaving the tag off would make it a negative control for
            // both at once, so a generator-compatibility bug could hide behind the missing passes.
            "Shadow2DGenerators" = "Unity.Legacy"
        }

        Cull Off
        ZWrite Off
        ZTest Always

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
    }
}

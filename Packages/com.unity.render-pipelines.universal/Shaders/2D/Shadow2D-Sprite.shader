Shader "Hidden/Shadow2DSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }

        Cull Off
        ZWrite Off
        ZTest Always

        // Shared code for all three passes: vertex entry point, skinning multi_compile,
        // sprite vertex include, and the _ShadowAlphaCutoff uniform (unused in "Self",
        // sampled in "UnshadowMark" / "UnshadowUnmark"). Each pass only declares its own
        // frag function body below.
        HLSLINCLUDE
        #pragma vertex ShadowSpriteVert
        #pragma fragment frag
        #pragma multi_compile _ SKINNED_SPRITE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Shadow2DSpriteVertex.hlsl"
        float _ShadowAlphaCutoff;
        ENDHLSL

        // Self-shadow pass: draws shadow into R channel.
        // Used by self-shadowing sprite casters (selfShadows == true).
        Pass
        {
            Name "Self"

            BlendOp Max
            Blend One One
            ColorMask R

            HLSLPROGRAM
            half4 frag(Varyings i) : SV_Target
            {
                half4 main = i.color * tex2D(_MainTex, i.uv);
                return half4(main.a, main.a, main.a, main.a);
            }
            ENDHLSL
        }

        // Unshadow "Mark" pass: writes B channel and sets stencil ref 1.
        // First of the two unshadow passes for non-self-shadowing sprite casters.
        Pass
        {
            Name "UnshadowMark"

            Stencil
            {
                Ref       1
                Comp      Always
                Pass      Replace
            }

            Blend   One One
            BlendOp Add
            ColorMask B

            HLSLPROGRAM
            half4 frag(Varyings i) : SV_Target
            {
                half4 main = i.color * tex2D(_MainTex, i.uv);

                if (main.a <= _ShadowAlphaCutoff)
                    discard;

                return half4(0, 0, main.a, 0);
            }
            ENDHLSL
        }

        // Unshadow "Unmark" pass: clears stencil ref 0. No color writes (ColorMask 0).
        // Alpha-test discard is preserved so stencil only clears where the sprite covers.
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
            ColorMask 0

            HLSLPROGRAM
            half4 frag(Varyings i) : SV_Target
            {
                half4 main = i.color * tex2D(_MainTex, i.uv);

                if (main.a <= _ShadowAlphaCutoff)
                    discard;

                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}

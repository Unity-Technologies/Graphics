// Test-only shader: builds the tangent frame like Normals2DCommon.hlsl and passes a tangent-space normal of (0, 1, 0) through
// NormalsRenderingShared, so RGB is the world-space V axis after the sprite flip compensation, encoded as 0.5 * (v + 1).
// Alpha is 1 when unity_SpriteProps.z holds a bone offset (the sprite was drawn from its GPU buffers) and 0.5 otherwise.
// Used by SpriteSkinnedTangentFrameTests.
Shader "Hidden/Test/SpriteSkinnedTangentFrameDebug"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 tangentWS : TEXCOORD1;
                float3 bitangentWS : TEXCOORD2;
                nointerpolation float hasBoneOffset : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.normalWS = TransformObjectToWorldDir(input.normal);
                o.tangentWS = TransformObjectToWorldDir(input.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * input.tangent.w;
                o.hasBoneOffset = asint(unity_SpriteProps.z) >= 0 ? 1.0 : 0.0;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                half4 normalColor = NormalsRenderingShared(half4(1, 1, 1, 1), half3(0, 1, 0), i.tangentWS, i.bitangentWS, i.normalWS);
                return float4(normalColor.rgb, lerp(0.5, 1.0, i.hasBoneOffset));
            }
            ENDHLSL
        }
    }
}

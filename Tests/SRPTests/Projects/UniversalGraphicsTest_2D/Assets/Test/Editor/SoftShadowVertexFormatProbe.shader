// Probe for SoftShadowVertexFormatTests.ShaderReceivesTangentAndBothTexCoordsFromTheInterleavedStream.
//
// Reads the planned Unity.SoftShadow payload semantics and writes four of its components straight
// out as colour, so a test can read back whether the attributes actually arrived. Deliberately has
// no includes and no matrix work: positions are authored in clip space, so the test does not depend
// on a camera, on URP being configured, or on any transform being set up.
//
// Not a fixture for any rendering behaviour -- it proves attribute binding only.
Shader "Hidden/Test/SoftShadowVertexFormatProbe"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Cull   Off
        ZWrite Off
        ZTest  Always
        Blend  Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // Must match CreateLayout() in SoftShadowVertexFormatTests. TEXCOORD0 is absent on
            // purpose: the point of the test is that TEXCOORD1 and TEXCOORD2 bind correctly when
            // channel 0 is not present in the vertex buffer at all.
            struct Attributes
            {
                float3 positionOS : POSITION;    // v.xy, 0
                float4 tangent    : TANGENT;     // role, fanParam, prevPt.xy
                float3 next       : TEXCOORD1;   // nextPt.xy, windingSign
                float4 neighbor2  : TEXCOORD2;   // prev2Pt.xy, next2Pt.xy
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 payload    : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = float4(v.positionOS.xy, 0, 1);
                o.payload    = float4(v.tangent.x, v.next.x, v.neighbor2.x, v.neighbor2.w);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                return i.payload;
            }
            ENDHLSL
        }
    }
}

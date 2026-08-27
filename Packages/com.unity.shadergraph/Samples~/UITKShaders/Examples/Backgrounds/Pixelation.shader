Shader "Unlit/UITKShaders/Pixelation"
{
    // A UI Toolkit custom backdrop-filter: samples the captured backdrop (_MainTex) and quantizes
    // it into square blocks, producing a resolution-independent pixelation of whatever is behind
    // the element. Authored as a post-process shader (NOT UI Shader Graph) per UITK's filter model;
    // conventions mirror the documented "swirl" custom-filter example.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend One Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _UIE_OUTPUT_LINEAR

            #include "UnityCG.cginc"
            #include "UnityUIEFilter.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint rectIndex : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float _PixelSize;

            v2f vert (FilterVertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rectIndex = GetFilterRectIndex(v);
                return o;
            }

            float2 NormalizeUVs(float2 uv, float4 uvRect)
            {
                // Atlas sub-rect -> 0..1 within the filtered element.
                return float2((uv.x - uvRect.x) / uvRect.z, (uv.y - uvRect.y) / uvRect.w);
            }

            float2 MapToUVRect(float2 uv, float4 uvRect)
            {
                // 0..1 -> back into the atlas sub-rect for sampling.
                return float2(uv.x * uvRect.z + uvRect.x, uv.y * uvRect.w + uvRect.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 uvRect = GetFilterUVRect(i.rectIndex);
                float2 uv = NormalizeUVs(i.uv, uvRect);

                // Chunk the element into square blocks of _PixelSize source pixels and snap each
                // fragment to its block centre. Block count derives from the element's pixel size
                // (atlas texel size scaled by the sub-rect), so blocks stay square regardless of
                // the element's aspect or the capture resolution.
                float2 elementPixels = _MainTex_TexelSize.zw * uvRect.zw;
                // Snap to a WHOLE number of blocks so the grid divides the element evenly: no thin
                // partial row at the top/bottom edge, and the grid only re-aligns on integer steps
                // as _PixelSize animates (kills the sub-pixel shimmer).
                float2 cells = max(float2(1.0, 1.0), round(elementPixels / max(1.0, _PixelSize)));
                uv = (floor(uv * cells) + 0.5) / cells;

                uv = MapToUVRect(uv, uvRect);
                half4 col = tex2D(_MainTex, uv);

                // Force-gamma workflow: the last filter pass must output linear.
                #if _UIE_OUTPUT_LINEAR
                col.rgb = GammaToLinearSpace(col.rgb);
                #endif

                return col;
            }
            ENDCG
        }
    }
}

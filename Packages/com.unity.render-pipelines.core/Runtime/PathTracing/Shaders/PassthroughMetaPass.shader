Shader "Hidden/PassthroughMetaPass"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        _EmissionMap("Emission", 2D) = "white" {}
        _TransparencyLM("Transmission", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Name "META"
            Tags { "LightMode" = "Meta" }
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _EMISSION

            #include "UnityCG.cginc"
            #include "UnityMetaPass.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                if (unity_MetaVertexControl.x)
                {
                    o.pos.xy = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                    o.pos.zw = 0;
                }
                else
                {
                    o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1.0));
                }
                o.uv1 = v.uv1;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                if (unity_MetaFragmentControl.x)
                {
                    return tex2Dlod(_BaseMap, float4(TRANSFORM_TEX(i.uv1, _BaseMap), 0, 0));
                }
                if (unity_MetaFragmentControl.y)
                {
                    return tex2Dlod(_EmissionMap, float4(TRANSFORM_TEX(i.uv1, _EmissionMap), 0, 0));
                }
                return 0;
            }
            ENDCG
        }
    }
}

Shader "Hidden/Core/TintedTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Main Color", Color) = (1,1,1,1)
        _Angle("Angle (rad)", float) = 0
    }
    SubShader
    {
        //this is used by GUI, no need for Tags or LOD

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Angle;

            float2 Rotate(float2 v, float cos0, float sin0)
            {
                return float2(v.x * cos0 - v.y * sin0,
                    v.x * sin0 + v.y * cos0);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 rotatedUV = Rotate(v.uv - (0.5, 0.5), cos(_Angle), sin(_Angle)) + (0.5, 0.5);
                o.uv = TRANSFORM_TEX(rotatedUV, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                _Color.a = 1;
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}

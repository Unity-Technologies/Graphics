Shader "Custom/Post-processing Tests/Nan_Shader"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1,0,0,0)
        _Color2 ("Color 2", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                };

                float4 _Color1;
                float4 _Color2;
                
                v2f vert (appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    return o;
                }
                
                half4 frag (v2f i) : SV_Target
                {
                    return _Color1 / _Color2;
                }

            ENDCG
        }
    }
}

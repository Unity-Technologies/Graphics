Shader "TestRenderPipeline/BasicShaderTransparent"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MainColor("MainColor", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque"  "RenderPipeline" = "TestRenderPipeline" "Queue" = "Transparent" }
        LOD 100

        ZWrite Off
        ZTest LEqual
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Forward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float4 _LightDirection;
            uniform float4 _LightColor;


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _MainColor;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normalWS = mul((float3x3)unity_ObjectToWorld, v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

           
            float4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed4 color = texColor * _MainColor;

                float3 result = dot(normalize(i.normalWS), _LightDirection.xyz);

                return float4(result * color.rgb * _LightColor.rgb, color.a);
            }
            ENDCG
        }
    }
}

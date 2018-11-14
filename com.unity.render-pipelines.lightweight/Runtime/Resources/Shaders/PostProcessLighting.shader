Shader "2D Lighting/PostProcessLighting"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_LightingTex("Texture", 2D) = "white" {}
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
			// make fog work
			#pragma multi_compile_fog
			
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


			sampler2D _LightingTex;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform float _Exposure = 1;
			uniform fixed4 _AmbientColor;

			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 main = tex2D(_MainTex, i.uv);
				fixed4 light = tex2D(_LightingTex, i.uv);
				fixed4 finalOutput;
				//finalOutput.rgb = max(light.a * light.rgb * main.rgb, _AmbientColor.rgb);
				finalOutput.rgb = (1-_AmbientColor.a) * light.a * light.rgb * main.rgb + _AmbientColor.a * _AmbientColor.rgb * main.rgb;
				finalOutput.a = 1;

				return finalOutput;
			}
			ENDCG
		}
	}
}

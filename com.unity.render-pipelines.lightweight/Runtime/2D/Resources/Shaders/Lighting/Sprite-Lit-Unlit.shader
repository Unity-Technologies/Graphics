Shader "Lightweight Render Pipeline/2D/Sprite-Unlit"
{
	Properties
	{
		_MainTex ("Diffuse", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		Pass
		{
			Tags { "LightMode" = "CombinedShapeLight" }

			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma vertex SpriteUnlitVertex
			#pragma fragment SpriteUnlitFragment

			uniform sampler2D _MainTex;
			uniform fixed4 _MainTex_ST;

			struct appdata
			{
				float4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				fixed2 uv : TEXCOORD0;
			};

			v2f SpriteUnlitVertex(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}

			fixed4 SpriteUnlitFragment(v2f i) : SV_Target
			{
				fixed4 main = i.color * tex2D(_MainTex, i.uv);
				return main;
			}
			ENDCG
		}
	}
}

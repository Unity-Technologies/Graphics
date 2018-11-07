Shader "2D Lighting/Soft Shadow Volume"
{
	Properties
	{
		_ShadowTex("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100
		Cull Off
		Blend One One
		BlendOp Max
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float2 uv	  : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv	  : TEXCOORD0;
			};

			uniform float4 _LightPos;
			uniform float  _LightMaxRadius;
			uniform float4 _ReferenceVector;
			uniform fixed4 _SolidColor;
			uniform fixed4 _FadeColor;


			sampler2D _ShadowTex;
			float4 _ShadowTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;

				_LightPos.w = 1;
				float4 localLightPos = mul(unity_WorldToObject, _LightPos);
				float  lightDist = length((v.vertex.xy - localLightPos.xy));
				float throwDistance = clamp(_LightMaxRadius - lightDist, 0, _LightMaxRadius);

				o.vertex = UnityObjectToClipPos(v.vertex + throwDistance * v.normal);
				o.uv = TRANSFORM_TEX(v.uv, _ShadowTex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_ShadowTex, i.uv);
				fixed4 finalCol = (1 - color.a) * _FadeColor + color.a * _SolidColor;
				return finalCol;
			}
			ENDCG
		}
	}
}

Shader "Hidden/GraphicTests/ResultDisplay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ResultTex ("Result Texture", 2D) = "white" {}
		_DiffTex ("Diff Texture", 2D) = "white" {}

		_DiffA("Diff A", float ) = 0.4
		_DiffB("Diff B", float ) = 0.6
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

			sampler2D _MainTex, _ResultTex, _DiffTex;
			float _DiffA, _DiffB;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col = fixed4(0,0,0,1);

				if ( i.uv.x < _DiffA )
					col = tex2D(_MainTex, i.uv);
				else if (i.uv.x < _DiffB )
					col = tex2D(_DiffTex, i.uv);
				else
					col = tex2D(_ResultTex, i.uv);

				col = pow(col, 1.0/2.2);

				return col;
			}
			ENDCG
		}
	}
}

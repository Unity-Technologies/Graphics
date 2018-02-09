Shader "Hidden/SRP_Core/TextureCombiner"
{
	Properties
	{
		// Chanels are : r=0, g=1, b=2, a=3, greyscale from rgb = 4

		[NoScaleOffset] _RSource ("R Source", 2D) = "white" {}
		_RChannel ("R Channel", float) = 0
		
		[NoScaleOffset] _GSource ("G Source", 2D) = "white" {}
		_GChannel ("G Channel", float) = 1
		
		[NoScaleOffset] _BSource ("B Source", 2D) = "white" {}
		_BChannel ("B Channel", float) = 2
		
		[NoScaleOffset] _ASource ("A Source", 2D) = "white" {}
		_AChannel ("A Channel", float) = 3
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

			sampler2D _RSource, _GSource, _BSource, _ASource;
			float _RChannel, _GChannel, _BChannel, _AChannel;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float PlotSourcetoChanel(float4 source, float param)
			{
				if (param >= 4)
					return source.r * 0.3 + source.g * 0.59 + source.b * 0.11; // Photoshop desaturation : G*.59+R*.3+B*.11
				else
					return source[param];
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);

				col.r = PlotSourcetoChanel( tex2D(_RSource, i.uv), _RChannel );
				col.g = PlotSourcetoChanel( tex2D(_GSource, i.uv), _GChannel );
				col.b = PlotSourcetoChanel( tex2D(_BSource, i.uv), _BChannel );
				col.a = PlotSourcetoChanel( tex2D(_ASource, i.uv), _AChannel );

				return col;
			}
			ENDCG
		}
	}
}

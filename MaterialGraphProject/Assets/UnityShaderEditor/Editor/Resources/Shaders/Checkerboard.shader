Shader "Hidden/Checkerboard"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform float _X;
			uniform float _Y;

			float4 frag(v2f_img i) : COLOR 
			{
				//float2 size = float2(16, 16);//textureSize2D(Texture0,0);
				float total = floor(i.uv.x * _X) + floor(i.uv.y * _Y);
				bool isEven = total % 2.0 == 0.0;
				float4 col1 = float4(0.0, 0.0, 0.0, 1.0);
				float4 col2 = float4(1.0, 1.0, 1.0, 1.0);
				fixed4 col = (isEven) ? col1 : col2;
				return col;//fixed4(1.0, 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
}

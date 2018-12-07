Shader "Hidden/Light2d-Shape-Volumetric"
{
    Properties
    {
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha One
			BlendOp Add
			ZWrite Off
			ZTest Off 
			Cull Off  // Shape lights have their interiors with the wrong winding order


			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float4 color : COLOR;
				float4 volumeColor : TANGENT;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color * v.volumeColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 color = i.color;
                return color;
            }
            ENDCG
        }
    }
}

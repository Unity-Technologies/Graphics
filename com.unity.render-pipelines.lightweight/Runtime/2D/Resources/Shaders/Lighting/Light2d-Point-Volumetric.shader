Shader "Hidden/Light2d-Point-Volumetric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
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
				float2 lookupUV : TEXCOORD0;
				float2 lookupNoRotUV : TEXCOORD1;
            };

			uniform sampler2D_float	_LightLookup;
			uniform half			_OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
			uniform half4x4			_LightInvMatrix;
			uniform half4x4			_LightNoRotInvMatrix;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float4 worldSpacePos = mul(unity_ObjectToWorld, v.vertex);
				float4 lightSpacePos = mul(_LightInvMatrix, worldSpacePos);
				float4 lightSpaceNoRotPos = mul(_LightNoRotInvMatrix, worldSpacePos);
				o.lookupUV = 0.5 * (lightSpacePos.xy + 1);
				o.lookupNoRotUV = 0.5 * (lightSpaceNoRotPos.xy + 1);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				half4 lookupValueNoRot = tex2D(_LightLookup, i.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
				half4 lookupValue = tex2D(_LightLookup, i.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

				// Inner Radius
				half  attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius
				attenuation = attenuation * attenuation;

				// Spotlight
				half  spotAttenuation = saturate((_OuterAngle - lookupValue.g)*_InnerAngleMult);
				spotAttenuation = spotAttenuation * spotAttenuation;
				attenuation = attenuation * spotAttenuation;

				fixed4 col = i.color * attenuation;
	            return col;
            }
            ENDCG
        }
    }
}

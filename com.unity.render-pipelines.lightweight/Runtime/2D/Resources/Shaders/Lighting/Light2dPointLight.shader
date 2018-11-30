Shader "Hidden/Light2DPointLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
		_Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        //Tags { "RenderType"="Opaque" }
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend One One
		BlendOp Add
		ZWrite Off
		Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma enable_d3d11_debug_symbols

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
				float2 screenUV : TEXCOORD1;
				float2 lookupUV : TEXCOORD2;	   // This is used for light relative direction
				float2 lookupNoRotUV : TEXCOORD3;  // This is used for screen relative direction of a light
				float4 lightDirection: TEXCOORD4;
            };

            sampler2D _MainTex;
            half4 _MainTex_ST;

			uniform half4			_LightColor;
			uniform float4			_LightPosition;
			uniform half4x4			_LightInvMatrix;
			uniform half4x4			_LightNoRotInvMatrix;
			uniform sampler2D_float	_LightLookup;
			uniform half			_OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
			uniform half			_InverseLightIntensityScale;
			;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float4 worldSpacePos = mul(unity_ObjectToWorld, v.vertex);
				float4 lightSpacePos = mul(_LightInvMatrix, worldSpacePos);
				float4 lightSpaceNoRotPos = mul(_LightNoRotInvMatrix, worldSpacePos);
				o.lookupUV = 0.5 * (lightSpacePos.xy + 1);
				o.lookupNoRotUV = 0.5 * (lightSpaceNoRotPos.xy + 1);
				o.lightDirection = worldSpacePos - _LightPosition;

				float4 clipVertex = o.vertex / o.vertex.w;
				o.screenUV = ComputeScreenPos(clipVertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				half4 main = tex2D(_MainTex, i.screenUV);
				half4 lookupValueNoRot = tex2D(_LightLookup, i.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
				half4 lookupValue = tex2D(_LightLookup, i.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction


				float usingDefaultNormalMap = (main.x + main.y + main.z) == 0;  // 1 if using a black normal map, 0 if using a custom normal map
				// Can this be moved to the normal renderer? Is it worth it?
				half3 normalUnpacked = UnpackNormal(main);
				
				normalUnpacked.z = 0;
				normalUnpacked = normalize(normalUnpacked);

				// Inner Radius
				half  attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius
				attenuation = attenuation * attenuation;

				// Spotlight
				half  spotAttenuation = saturate((_OuterAngle-lookupValue.g)*_InnerAngleMult);
				spotAttenuation = spotAttenuation * spotAttenuation;
				attenuation = attenuation * spotAttenuation;

				// Calculate final color
				half2 dirToLight = i.lightDirection.xy;
				half2 normal = half2(normalUnpacked.x, normalUnpacked.y);
				half cosAngle = (1-usingDefaultNormalMap) * saturate(dot(dirToLight, normal)) + usingDefaultNormalMap;
				half4 color = main.a *_LightColor * attenuation;
				fixed4 finalColor = color * cosAngle; /*  +color * main.z * attenuation); */

				return finalColor * _InverseLightIntensityScale;
			}
            ENDCG
        }
    }
}

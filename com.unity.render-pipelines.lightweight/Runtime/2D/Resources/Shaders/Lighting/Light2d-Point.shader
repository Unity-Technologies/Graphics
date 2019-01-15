Shader "Hidden/Light2D-Point"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
			uniform sampler2D		_NormalMap;
			uniform half			_OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
			uniform half			_InverseLightIntensityScale;

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
				o.lightDirection = _LightPosition-worldSpacePos;
				o.lightDirection.z = 3;
				o.lightDirection.w = 0;
				o.lightDirection.xyz = normalize(o.lightDirection.xyz);

				float4 clipVertex = o.vertex / o.vertex.w;
				o.screenUV = ComputeScreenPos(clipVertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				half4 cookie = tex2D(_MainTex, i.lookupUV);
				half4 normal = tex2D(_NormalMap, i.screenUV);
				half4 lookupValueNoRot = tex2D(_LightLookup, i.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
				half4 lookupValue = tex2D(_LightLookup, i.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

				float usingDefaultNormalMap = (normal.x + normal.y + normal.z) == 0;  // 1 if using a black normal map, 0 if using a custom normal map
				float3 normalUnpacked = UnpackNormal(normal);

				// Inner Radius
				half  attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius

				// Spotlight
				half  spotAttenuation = saturate((_OuterAngle-lookupValue.g)*_InnerAngleMult);
				attenuation = attenuation * spotAttenuation;
				//attenuation = attenuation * attenuation;

				// Calculate final color
				float3 dirToLight = i.lightDirection; // half2(lookupValueNoRot.b, lookupValueNoRot.a);
				float cosAngle = (1-usingDefaultNormalMap) * saturate(dot(dirToLight, normalUnpacked)) + usingDefaultNormalMap;
				half4 lightColor = normal.a * _LightColor * attenuation * cosAngle;

				return lightColor * _InverseLightIntensityScale;
			}
            ENDCG
        }
    }
}

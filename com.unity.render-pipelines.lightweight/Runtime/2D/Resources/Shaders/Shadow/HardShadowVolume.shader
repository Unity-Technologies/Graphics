Shader "Hidden/Hard Shadow Volume"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
		LOD 100
		Cull Off
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
				float4 normal : TANGENT;  // May contain two normals x,y and z,w
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
			};

			uniform float4 _LightPos;
			uniform float  _LightMaxRadius;
			uniform float  _LightMinRadius;
			uniform fixed4 _ShadowColor;


			v2f vert(appdata v)
			{
				v2f o;
				_LightPos.w = 1;
				//float4 localLightPos = mul(unity_WorldToObject, _LightPos);
				float4 lightPos = _LightPos;
				float4 localPos = v.vertex;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

				float  lightCenterDist = length((worldPos-lightPos).xy);
				float2 lightCenterDir = normalize((lightPos-worldPos).xy);

				float2 light1Pos = lightPos + _LightMinRadius * float2(lightCenterDir.y, -lightCenterDir.x);
				float2 light2Pos = lightPos + _LightMinRadius * float2(-lightCenterDir.y, lightCenterDir.x);

				float2 light1Dir = normalize((light1Pos - worldPos).xy);
				float2 light2Dir = normalize((light2Pos - worldPos).xy);

				float  light1Angle1 = dot(light1Dir.xy, v.normal.xy);
				float  light1Angle2 = dot(light1Dir.xy, v.normal.zw);
				float  light2Angle1 = dot(light2Dir.xy, v.normal.xy);
				float  light2Angle2 = dot(light2Dir.xy, v.normal.zw);

				float throwDistance = clamp(_LightMaxRadius - lightCenterDist, 0, _LightMaxRadius);

				float normalLen = length(v.normal.xy);
				float castsShadows = ((light1Angle2 > 0 || light2Angle2 > 0) && (light1Angle1 < 0 && light2Angle1 < 0)) || (light1Angle2 < 0 && light2Angle2 < 0 && light1Angle1 < 0 && light2Angle1 <0) || (normalLen == 0);
				//float castsShadows = !(((light1Angle1 > 0 || light2Angle1 > 0) && (light1Angle2 > 0 || light2Angle2 > 0)) || ((light1Angle1 < 0 && light1Angle2 < 0 && light2Angle1 < 0 && light2Angle2 < 0))) || (normalLen == 0);
				//float castsShadows = (((light1Angle1 > 0 || light2Angle1 > 0) && (light1Angle2 > 0 || light2Angle2 > 0)))  || (normalLen == 0);


				float4 shadowPos;
				shadowPos.xy = localPos.xy + castsShadows *  throwDistance * -lightCenterDir;
				shadowPos.z = localPos.z;
				shadowPos.w = 1;
				
				o.vertex = UnityObjectToClipPos(shadowPos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _ShadowColor;
				return col;
			}
			ENDCG
		}
	}
}

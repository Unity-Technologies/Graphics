// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/InfiniteWater"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Size ("size", float) = 3.0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent-101" "RenderPipeline" = "UniversalPipeline" }
		LOD 100

		Pass
		{
			ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "WaterInput.hlsl"
			#include "CommonUtilities.hlsl"
			#include "WaterLighting.hlsl"

			#define EPSILON 0.00001

			// ray-plane intersection test
			// @return side of plane hit
			//    0 : no hit
			//    1 : front
			//    2 : back
			int intersect_plane (float3 ro, float3 rd, float3 po, float3 pd, out float3 hit)
			{   
				float D = dot(po, pd);       // re-parameterize plane to normal + distance
				float tn = D - dot(ro, pd);  // ray pos w.r.t. plane (frnt, back, on)
				float td = dot (rd, pd);     // ray ori w.r.t. plane (towards, away, parallel)
				
				if (td > -EPSILON  &&  td < EPSILON)  return 0;  // parallel to plane
				
				float t = tn / td;          // dist along ray to hit
				if (t < 0.0)  return 0;     // plane lies behind ray
				hit = ro + t * rd;          // got a hit
				return (tn > 0.0) ? 2 : 1;  // which side of the plane?
			}

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD2;//screen position of the verticies after wave distortion
				float4 viewDir : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Size;
			float _CameraRoll;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 posWS = TransformObjectToWorld(v.vertex.xyz);
				o.vertex = TransformWorldToHClip(posWS);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.viewDir.xyz = UNITY_MATRIX_IT_MV[2].xyz;
				float3 viewPos = TransformWorldToView(posWS);// posWS - _WorldSpaceCameraPos;
				o.viewDir.w = length(viewPos / viewPos.z);
				return o;
			}
			
			void frag (v2f i, out half4 outColor:SV_Target, out float outDepth : SV_Depth) //: SV_Target
			{

				float3 viewDir = i.viewDir.xyz; //UNITY_MATRIX_IT_MV[2].xyz;
				float3 pos = _WorldSpaceCameraPos * viewDir;
				i.uv = pos.xy;

				//////
				float4 p = i.screenPos;
				float2 uv = p.xy / p.w; // [0, 1]
				half2 st = 2.0 * uv - half2(1.0, 1.0);  

				float asp =  _ScreenParams.x /  _ScreenParams.y;
				
				float2 st_adj = float2(st.x * asp, st.y);
				
				// camera settings
				//float dist = 2.0 + 0.5*sin(0.5*Time);
				//float theta = 0.1230596391*_Time.y;
				// float cx = dist * sin(theta);
				// float cz = dist * cos(theta);
				float3 cam_ori = float3(-_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z);
				// vec3 cam_look = vec3(0.0, 0.50, 0.0);
				float3 cam_dir = float3(viewDir.x, -viewDir.y, -viewDir.z);
				
				// over, up, norm basis vectors for camera
				half zRot = radians(-_CameraRoll);
				float3 cam_ovr = normalize(cross(cam_dir, half3(0, cos(zRot), sin(zRot))));
				float3 cam_uhp = normalize(cross(cam_ovr, cam_dir));
				
				// scene
				float3 po = 0;
				float3 pd = half3(0.0, 1.0, 0.0);
				
				// ray
				half3 ro = cam_ori;

				float cam_dist = unity_CameraProjection._m11;//80 degrees = 1.2
				float3 rt = cam_ori + cam_dist*cam_dir;
				rt += st_adj.x * cam_ovr;
				rt += st_adj.y * cam_uhp;
				half3 rd = normalize(rt - cam_ori);
				
				float3 hit;
				int side = intersect_plane (ro, rd, po, pd, hit);
				if(side == 0)
					discard;

				//half fog = pow(1-abs(rd.y), 20);
				// plane
				//  - figure out UV on plane to sample texture
				half3 dee = hit;
				float tSize = 0.05;
				float2 p_uv = float2(dot(dee, half3(1, 0, 0)) * tSize, dot(dee, half3(0, 0, 1)) * tSize);
	///
	// sample the texture
				half4 col = tex2D(_MainTex, p_uv);

				//re-construct depth
				half3 camPos = _WorldSpaceCameraPos;
				float a = _ProjectionParams.z / ( _ProjectionParams.z - _ProjectionParams.y );
				float b = _ProjectionParams.z * _ProjectionParams.y / ( _ProjectionParams.y - _ProjectionParams.z );
				float z = length(hit + half3(camPos.x, camPos.y + 1, -camPos.z)) / i.viewDir.w;
				float d =  a + b / z;
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				//return float4(col);
				//outColor = half4(i.viewDir.xyz, 1);
				outColor = half4(frac(p_uv), frac(1-d), 1);
				outDepth = 1-d;
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/InternalErrorShader"
}

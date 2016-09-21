Shader "Hidden/VFX_0"
{
	SubShader
	{
		Pass
		{
			Tags { "LightMode" = "Deferred" }
			ZTest LEqual
			ZWrite On
			Cull Off
			
			CGPROGRAM
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_WORLD_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "..\VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float2 outputUniform0;
				float3 outputUniform1;
				float outputUniform2;
				float outputUniform3;
			CBUFFER_END
			
			struct Attribute1
			{
				float3 position;
				float _PADDING_;
			};
			
			struct Attribute2
			{
				float2 size;
			};
			
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				linear noperspective centroid float4 pos : SV_POSITION;
				nointerpolation float4 col : SV_Target0;
				float2 offsets : TEXCOORD0;
				nointerpolation float3 viewCenterPos : TEXCOORD1;
				float4 viewPosAndSize : TEXCOORD2;
			};
			
			void VFXBlockCameraFade( inout float alpha,float3 position,float2 FadeDistances,inout bool kill)
			{
				float planeDist = VFXNearPlaneDist(position);
	float camFade = smoothstep(FadeDistances.x,FadeDistances.y,planeDist);
	if (camFade == 0.0f)
	    KILL;
	alpha *= camFade;
			}
			
			void VFXBlockSetColorConstant( inout float3 color,float3 Color)
			{
				color = Color;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 16384;
				if (flags[index] == 1)
				{
					bool kill = false;
					
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute2 attrib2 = attribBuffer2[index];
					
					float local_alpha = (float)0;
					float3 local_color = (float3)0;
					
					VFXBlockCameraFade( local_alpha,attrib1.position,outputUniform0,kill);
					VFXBlockSetColorConstant( local_color,outputUniform1);
					
					float2 size = attrib2.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib1.position;
					
					float3 posToCam = VFXCameraPos() - position;
					float camDist = length(posToCam);
					float scale = 1.0f - size.x / camDist;
					float3 front = posToCam / camDist;
					float3 side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
					float3 up = cross(side,front);
					
					o.viewCenterPos = mul(UNITY_MATRIX_V,float4(position,1.0f)).xyz;
					position += side * (o.offsets.x * size.x) * scale;
					position += up * (o.offsets.y * size.y) * scale;
					position += front * size.x;
					
					o.viewPosAndSize = float4(mul(UNITY_MATRIX_V,float4(position,1.0f)).xyz,size.x);
					o.pos = mul (UNITY_MATRIX_VP, float4(position,1.0f));
					o.col = float4(local_color.xyz,local_alpha);
					if (kill)
					{
						o.pos = -1.0;
						o.col = 0;
					}
				}
				else
				{
					o.pos = -1.0;
					o.col = 0;
				}
				
				return o;
			}
			
			struct ps_output
			{
				float4 col : SV_Target0;
				float4 spec_smoothness : SV_Target1;
				float4 normal : SV_Target2;
				float4 emission : SV_Target3;
				float depth : SV_DepthLessEqual;
			};
			
			ps_output frag (ps_input i)
			{
				ps_output o = (ps_output)0;
				
				float4 color = i.col;
				float lsqr = dot(i.offsets, i.offsets);
				if (lsqr > 1.0)
					discard;
				
				float nDepthOffset = 1.0f - sqrt(1.0f - lsqr); // normalized depth offset
				float3 camToPosDir = normalize(i.viewPosAndSize.xyz);
				float3 viewPos = i.viewPosAndSize.xyz + (camToPosDir * (nDepthOffset * i.viewPosAndSize.w));
				o.depth = -(1.0f + viewPos.z * _ZBufferParams.w) / (viewPos.z * _ZBufferParams.z);
				float3 specColor = (float3)0;
				float oneMinusReflectivity = 0;
				float metalness = saturate(outputUniform2);
				color.rgb = DiffuseAndSpecularFromMetallic(color.rgb,metalness,specColor,oneMinusReflectivity);
				color.a = 0.0f;
				float3 normal = normalize(viewPos - i.viewCenterPos) * float3(1,1,-1);
				o.spec_smoothness = float4(specColor,outputUniform3);
				o.normal = mul(unity_CameraToWorld, float4(normal,0.0f)) * 0.5f + 0.5f;
				half3 ambient = color.xyz * 0.0f;//ShadeSHPerPixel(normal, float4(color.xyz, 1) * 0.1, float3(0, 0, 0));
				o.emission = float4(ambient, 0);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

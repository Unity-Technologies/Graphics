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
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float outputUniform0;
				float outputUniform1;
			CBUFFER_END
			
			CBUFFER_START(Uniform)
				float systemIndex;
			CBUFFER_END
			ByteAddressBuffer nbElements;
			
			struct OutputData
			{
				float3 position;
				uint _PADDING_0;
				float3 color;
				uint _PADDING_1;
				float2 size;
				uint2 _PADDING_2;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				nointerpolation float3 viewCenterPos : TEXCOORD1;
				float4 viewPosAndSize : TEXCOORD2;
			};
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				if (index < nbElements.Load(asuint(systemIndex) << 2))
				{
					OutputData outputData = outputBuffer[index];
					
					
					float2 size = outputData.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = outputData.position;
					
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
					o.col = float4(outputData.color.xyz,0.5);
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
				float metalness = saturate(outputUniform0);
				color.rgb = DiffuseAndSpecularFromMetallic(color.rgb,metalness,specColor,oneMinusReflectivity);
				color.a = 0.0f;
				float3 normal = normalize(viewPos - i.viewCenterPos) * float3(1,1,-1);
				o.spec_smoothness = float4(specColor,outputUniform1);
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

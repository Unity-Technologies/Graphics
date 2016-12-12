Shader "Hidden/VFX_0"
{
	SubShader
	{
		Pass
		{
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
				float4 outputUniform1;
			CBUFFER_END
			
			CBUFFER_START(Uniform)
				float systemIndex;
			CBUFFER_END
			ByteAddressBuffer nbElements;
			
			Texture2D outputSampler0Texture;
			SamplerState sampleroutputSampler0Texture;
			
			Texture2D curveTexture;
			SamplerState samplercurveTexture;
			
			struct OutputData
			{
				float3 position;
				float age;
				float3 color;
				float lifetime;
				float2 size;
				uint2 _PADDING_0;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
			};
			
			// Non optimized generic function to allow curve edition without recompiling
			float sampleSignal(float4 curveData,float u) // sample curve
			{
				float uNorm = (u * curveData.x) + curveData.y;
				switch(asuint(curveData.w) >> 2)
				{
					case 1: uNorm = ((0.9921875 * frac(min(1.0f - 1e-5f,uNorm))) + 0.00390625); break; // clamp end
					case 2: uNorm = ((0.9921875 * frac(max(0.0f,uNorm))) + 0.00390625); break; // clamp start
					case 3: uNorm = ((0.9921875 * saturate(uNorm)) + 0.00390625); break; // clamp both
				}
				return curveTexture.SampleLevel(samplercurveTexture,float2(uNorm,curveData.z),0)[asuint(curveData.w) & 0x3];
			}
			
			void VFXBlockAngleConstant( inout float angle,float Value)
			{
				angle += Value;
			}
			
			void VFXBlockSetAlphaCurveOverLifetime( inout float alpha,float age,float lifetime,float4 Curve)
			{
				float ratio = saturate(age / lifetime);
	alpha = sampleSignal(Curve,ratio);
			}
			
			void VFXBlockFaceCameraPlane( inout float3 front,inout float3 side,inout float3 up)
			{
				float4x4 cameraMat = VFXCameraMatrix();
	front = -VFXCameraLook();
	side = cameraMat[0].xyz;
	up = cameraMat[1].xyz;
			}
			
			void VFXBlockSubPixelAA( inout float alpha,float3 position,inout float2 size)
			{
				#ifdef VFX_WORLD_SPACE
	float clipPosW = mul(UNITY_MATRIX_VP,float4(position,1.0f)).w;
	#else
	float clipPosW = mul(UNITY_MATRIX_MVP,float4(position,1.0f)).w;
	#endif
	float minSize = clipPosW / (0.5f * min(UNITY_MATRIX_P[0][0] * _ScreenParams.x,-UNITY_MATRIX_P[1][1] * _ScreenParams.y)); // max size in one pixel
	float2 clampedSize = max(size,minSize);
	float fade = (size.x * size.y) / (clampedSize.x * clampedSize.y);
	alpha *= fade;
	size = clampedSize;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				if (index < nbElements.Load(asuint(systemIndex) << 2))
				{
					OutputData outputData = outputBuffer[index];
					
					float local_angle = (float)0;
					float local_alpha = (float)0;
					float3 local_front = (float3)0;
					float3 local_side = (float3)0;
					float3 local_up = (float3)0;
					
					VFXBlockAngleConstant( local_angle,outputUniform0);
					VFXBlockSetAlphaCurveOverLifetime( local_alpha,outputData.age,outputData.lifetime,outputUniform1);
					VFXBlockFaceCameraPlane( local_front,local_side,local_up);
					VFXBlockSubPixelAA( local_alpha,outputData.position,outputData.size);
					
					float2 size = outputData.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = outputData.position;
					
					float2 posOffsets = o.offsets.xy;
					float3 cameraPos = _WorldSpaceCameraPos.xyz;
					float3 front = local_front;
					float3 side = local_side;
					float3 up = local_up;
					
					float2 sincosA;
					sincos(radians(local_angle), sincosA.x, sincosA.y);
					const float c = sincosA.y;
					const float s = sincosA.x;
					const float t = 1.0 - c;
					const float x = front.x;
					const float y = front.y;
					const float z = front.z;
					
					float3x3 rot = float3x3(t * x * x + c, t * x * y - s * z, t * x * z + s * y,
										t * x * y + s * z, t * y * y + c, t * y * z - s * x,
										t * x * z - s * y, t * y * z + s * x, t * z * z + c);
					
					
					position += mul(rot,side * posOffsets.x * size.x);
					position += mul(rot,up * posOffsets.y * size.y);
					o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
					
					o.pos = mul (UNITY_MATRIX_VP, float4(position,1.0f));
					o.col = float4(outputData.color.xyz,local_alpha);
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
			};
			
			ps_output frag (ps_input i)
			{
				ps_output o = (ps_output)0;
				
				float4 color = i.col;
				color *= outputSampler0Texture.Sample(sampleroutputSampler0Texture,i.offsets);
				if (color.a < 0.33333) discard;
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

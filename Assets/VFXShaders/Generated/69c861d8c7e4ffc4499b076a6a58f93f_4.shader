Shader "Hidden/VFX_4"
{
	SubShader
	{
		Pass
		{
			ZTest LEqual
			ZWrite On
			Cull Off
			
			CGPROGRAM
			#pragma target 4.5
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float3 outputUniform1_kVFXCombine3fOp;
				float outputUniform0_kVFXValueOp;
				
			CBUFFER_END
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			Texture2D gradientTexture;
			SamplerState samplergradientTexture;
			
			struct OutputData
			{
				float3 position;
				float angle;
				float2 size;
				float age;
				float lifetime;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
			};
			
			float4 sampleSignal(float v,float u) // sample gradient
			{
				return gradientTexture.SampleLevel(samplergradientTexture,float2(((0.9921875 * saturate(u)) + 0.00390625),v),0);
			}
			
			void VFXBlockSetColorGradientOverLifetime( inout float3 color,inout float alpha,float age,float lifetime,float Gradient)
			{
				float ratio = saturate(age / lifetime);
	float4 rgba = SAMPLE(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			void VFXBlockSetPivot( inout float3 pivot,float3 Pivot)
			{
				pivot = Pivot;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				OutputData outputData = outputBuffer[index];
				
				float3 local_color = (float3)0;
				float local_alpha = (float)0;
				float3 local_pivot = (float3)0;
				
				VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,outputData.age,outputData.lifetime,outputUniform0_kVFXValueOp);
				VFXBlockSetPivot( local_pivot,outputUniform1_kVFXCombine3fOp);
				
				float2 size = outputData.size * 0.5f;
				o.offsets.x = 2.0 * float(id & 1) - 1.0;
				o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
				
				float3 position = outputData.position;
				
				float2 posOffsets = o.offsets.xy - local_pivot.xy;
				
				float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
				float3 front = -UNITY_MATRIX_MV[2].xyz;
				float3 side = UNITY_MATRIX_IT_MV[0].xyz;
				float3 up = UNITY_MATRIX_IT_MV[1].xyz;
				
				float2 sincosA;
				sincos(radians(outputData.angle), sincosA.x, sincosA.y);
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
				position -= front * local_pivot.z;
				o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
				
				o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
				o.col = float4(local_color.xyz,local_alpha);
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
				color *= outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,i.offsets);
				if (color.a < 0.33333) discard;
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

Shader "Hidden/VFX_4"
{
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass
		{
			Blend SrcAlpha One
			ZTest LEqual
			ZWrite Off
			Cull Off
			
			CGPROGRAM
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float4 outputUniform0;
				float outputUniform1;
				float outputUniform2;
				float outputUniform3;
				float2 outputUniform4;
			CBUFFER_END
			
			Texture2D outputSampler0Texture;
			SamplerState sampleroutputSampler0Texture;
			
			Texture2D curveTexture;
			SamplerState samplercurveTexture;
			
			struct OutputData
			{
				float3 velocity;
				float age;
				float3 position;
				float lifetime;
				float3 color;
				float texIndex;
				float alpha;
				uint3 _PADDING_0;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				nointerpolation float flipbookIndex : TEXCOORD1;
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
			
			void VFXBlockOrientAlongVelocity( inout float3 front,inout float3 side,inout float3 up,float3 velocity,float3 position)
			{
				up = normalize(velocity);
	front = VFXCameraPos() - position;
	side = normalize(cross(front,up));
	front = cross(up,side);
			}
			
			void VFXBlockSetAlphaCurveOverLifetime( inout float alpha,float age,float lifetime,float4 Curve)
			{
				float ratio = saturate(age / lifetime);
	alpha = sampleSignal(Curve,ratio);
			}
			
			void VFXBlockSizeConstantSquare( inout float2 size,float Size)
			{
				size = float2(Size,Size);
			}
			
			void VFXBlockApplyScaleRatioFromVelocity( inout float2 size,float3 velocity,float Multiplier,float MinHeight)
			{
				size.y = max(MinHeight,length(velocity) * Multiplier);
			}
			
			float2 GetSubUV(int flipBookIndex,float2 uv,float2 dim,float2 invDim)
			{
				float2 tile = float2(fmod(flipBookIndex,dim.x),dim.y - 1.0 - floor(flipBookIndex * invDim.x));
				return (tile + uv) * invDim;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				OutputData outputData = outputBuffer[index];
				
				float3 local_front = (float3)0;
				float3 local_side = (float3)0;
				float3 local_up = (float3)0;
				float2 local_size = (float2)0;
				
				VFXBlockOrientAlongVelocity( local_front,local_side,local_up,outputData.velocity,outputData.position);
				VFXBlockSetAlphaCurveOverLifetime( outputData.alpha,outputData.age,outputData.lifetime,outputUniform0);
				VFXBlockSizeConstantSquare( local_size,outputUniform1);
				VFXBlockApplyScaleRatioFromVelocity( local_size,outputData.velocity,outputUniform2,outputUniform3);
				
				float2 size = local_size * 0.5f;
				o.offsets.x = 2.0 * float(id & 1) - 1.0;
				o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
				
				float3 position = outputData.position;
				
				float2 posOffsets = o.offsets.xy;
				float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
				float3 side = local_side;
				float3 up = local_up;
				
				position += side * (posOffsets.x * size.x);
				position += up * (posOffsets.y * size.y);
				o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
				o.flipbookIndex = outputData.texIndex;
				
				o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
				o.col = float4(outputData.color.xyz,outputData.alpha);
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
				float2 dim = outputUniform4;
				float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU
				float ratio = frac(i.flipbookIndex);
				float index = i.flipbookIndex - ratio;
				
				float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);
				float4 col1 = outputSampler0Texture.Sample(sampleroutputSampler0Texture,uv1);
				
				float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);
				float4 col2 = outputSampler0Texture.Sample(sampleroutputSampler0Texture,uv2);
				
				color *= lerp(col1,col2,ratio);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

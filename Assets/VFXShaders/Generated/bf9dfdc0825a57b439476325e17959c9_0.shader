// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/VFX_0"
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
			#pragma target 4.5
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float4 outputUniform1_kVFXValueOp;
				
				float3 outputUniform0_kVFXCombine3fOp;
				float outputUniform2_kVFXValueOp;
				
				float2 outputUniform4_kVFXValueOp;
				float outputUniform3_kVFXValueOp;
				uint outputUniforms_PADDING_0;
			
			CBUFFER_END
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			Texture2D floatTexture;
			SamplerState samplerfloatTexture;
			
			struct OutputData
			{
				float3 position;
				float lifetime;
				float angle;
				float age;
				float texIndex;
				uint _PADDING_0;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				nointerpolation float flipbookIndex : TEXCOORD1;
			};
			
			float4 sampleSignal(float v,float u) // sample gradient
			{
				return floatTexture.SampleLevel(samplerfloatTexture,float2(((0.9921875 * saturate(u)) + 0.00390625),v),0);
			}
			
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
				return floatTexture.SampleLevel(samplerfloatTexture,float2(uNorm,curveData.z),0)[asuint(curveData.w) & 0x3];
			}
			
			float3 sampleSpline(float v,float u)
			{
				return floatTexture.SampleLevel(samplerfloatTexture,float2(((0.9921875 * saturate(u)) + 0.00390625),v),0);
			}
			
			void VFXBlockLookAtPosition( inout float3 front,inout float3 side,inout float3 up,float3 position,float3 Position)
			{
				front = normalize(Position - position);
	side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
	up = cross(side,front);
			}
			
			void VFXBlockSizeOverLifeCurve( inout float2 size,float age,float lifetime,float4 Curve)
			{
				float ratio = saturate(age/lifetime);
	float s = SAMPLE(Curve, ratio);
	size = float2(s,s);
			}
			
			void VFXBlockSetColorGradientOverLifetime( inout float3 color,inout float alpha,float age,float lifetime,float Gradient)
			{
				float ratio = saturate(age / lifetime);
	float4 rgba = SAMPLE(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			void VFXBlockSetColorScale( inout float3 color,float Scale)
			{
				color *= Scale;
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
				float3 local_color = (float3)0;
				float local_alpha = (float)0;
				
				VFXBlockLookAtPosition( local_front,local_side,local_up,outputData.position,outputUniform0_kVFXCombine3fOp);
				VFXBlockSizeOverLifeCurve( local_size,outputData.age,outputData.lifetime,outputUniform1_kVFXValueOp);
				VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,outputData.age,outputData.lifetime,outputUniform2_kVFXValueOp);
				VFXBlockSetColorScale( local_color,outputUniform3_kVFXValueOp);
				
				float2 size = local_size * 0.5f;
				o.offsets.x = 2.0 * float(id & 1) - 1.0;
				o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
				
				float3 position = outputData.position;
				
				float2 posOffsets = o.offsets.xy;
				float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
				float3 front = local_front;
				float3 side = local_side;
				float3 up = local_up;
				
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
				o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
				o.flipbookIndex = outputData.texIndex;
				
				o.pos = UnityObjectToClipPos (float4(position,1.0f));
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
				float2 dim = outputUniform4_kVFXValueOp;
				float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU
				float ratio = frac(i.flipbookIndex);
				float index = i.flipbookIndex - ratio;
				
				float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);
				float4 col1 = outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,uv1);
				
				float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);
				float4 col2 = outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,uv2);
				
				color *= lerp(col1,col2,ratio);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

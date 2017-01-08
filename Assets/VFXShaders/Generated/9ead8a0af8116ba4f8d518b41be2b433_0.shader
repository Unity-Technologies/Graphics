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
				float outputUniform0_kVFXValueOp;
				uint3 outputUniforms_PADDING_0;
			
			CBUFFER_END
			
			Texture2D floatTexture;
			SamplerState samplerfloatTexture;
			
			struct OutputData
			{
				float3 position;
				float lifetime;
				float3 color;
				float age;
				float2 size;
				float alpha;
				uint _PADDING_0;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
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
			
			void VFXBlockSetColorGradientOverLifetime( inout float3 color,inout float alpha,float age,float lifetime,float Gradient)
			{
				float ratio = saturate(age / lifetime);
	float4 rgba = SAMPLE(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			struct VertexInput
			{
				float3 position : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};
			ps_input vert (VertexInput input, uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = instanceID;
				OutputData outputData = outputBuffer[index];
				
				VFXBlockSetColorGradientOverLifetime( outputData.color,outputData.alpha,outputData.age,outputData.lifetime,outputUniform0_kVFXValueOp);
				
				float3 size = outputData.size.xyx * 0.5f;
				float3 pivot = 0.0f;
				float3 worldPos = outputData.position + ((input.position + pivot) * size);
				o.pos = mul(UNITY_MATRIX_MVP, float4(worldPos,1.0f));
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
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

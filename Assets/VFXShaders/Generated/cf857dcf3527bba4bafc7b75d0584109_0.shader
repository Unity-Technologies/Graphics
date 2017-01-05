Shader "Hidden/VFX_0"
{
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
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
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			Texture2D gradientTexture;
			SamplerState samplergradientTexture;
			
			struct Attribute0
			{
				float lifetime;
			};
			
			struct Attribute1
			{
				float3 position;
				float age;
			};
			
			struct Attribute3
			{
				float2 size;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute3> attribBuffer3;
			StructuredBuffer<int> flags;
			
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
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute3 attrib3 = attribBuffer3[index];
					
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib1.age,attrib0.lifetime,outputUniform0_kVFXValueOp);
					
					float2 size = attrib3.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib1.position;
					
					float2 posOffsets = o.offsets.xy;
					float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
					float3 side = UNITY_MATRIX_IT_MV[0].xyz;
					float3 up = UNITY_MATRIX_IT_MV[1].xyz;
					
					position += side * (posOffsets.x * size.x);
					position += up * (posOffsets.y * size.y);
					o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
					
					o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
					o.col = float4(local_color.xyz,local_alpha);
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
				color *= outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,i.offsets);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

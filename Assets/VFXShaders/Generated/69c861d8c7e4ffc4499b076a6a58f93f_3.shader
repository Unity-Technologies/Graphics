Shader "Hidden/VFX_3"
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
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "HLSLSupport.cginc"
			#include "..\VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float outputUniform0;
			CBUFFER_END
			
			sampler2D outputSampler0;
			
			sampler2D gradientTexture;
			
			struct Attribute0
			{
				float3 position;
				float age;
			};
			
			struct Attribute2
			{
				float2 size;
			};
			
			struct Attribute3
			{
				float lifetime;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<Attribute3> attribBuffer3;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
			};
			
			float4 sampleSignal(float v,float u) // sample gradient
			{
				return tex2Dlod(gradientTexture,float4(((0.9921875 * saturate(u)) + 0.00390625),v,0,0));
			}
			
			void VFXBlockSetColorGradientOverLifetime( inout float3 color,inout float alpha,float age,float lifetime,float Gradient)
			{
				float ratio = saturate(age / lifetime);
	float4 rgba = sampleSignal(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 16384;
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute2 attrib2 = attribBuffer2[index];
					Attribute3 attrib3 = attribBuffer3[index];
					
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib0.age,attrib3.lifetime,outputUniform0);
					
					float2 size = attrib2.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib0.position;
					
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
			
			float4 frag (ps_input i) : COLOR
			{
				float4 color = i.col;
				color *= tex2D(outputSampler0,i.offsets);
				if (color.a < 0.33333) discard;
				return color;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

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
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "HLSLSupport.cginc"
			#include "..\VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float3 outputUniform0;
				float4 outputUniform1;
			CBUFFER_END
			
			sampler2D outputSampler0;
			
			sampler2D curveTexture;
			
			struct Attribute0
			{
				float3 position;
				float age;
			};
			
			struct Attribute1
			{
				float2 size;
			};
			
			struct Attribute2
			{
				float lifetime;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				float4 pos : SV_POSITION;
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
				return tex2Dlod(curveTexture,float4(uNorm,curveData.z,0,0))[asuint(curveData.w) & 0x3];
			}
			
			void VFXBlockFaceCameraPosition( inout float3 front,inout float3 side,inout float3 up,float3 position)
			{
				front = normalize(VFXCameraPos() - position);
	side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
	up = cross(side,front);
			}
			
			void VFXBlockSetColorConstant( inout float3 color,float3 Color)
			{
				color = Color;
			}
			
			void VFXBlockSetAlphaCurveOverLifetime( inout float alpha,float age,float lifetime,float4 Curve)
			{
				float ratio = saturate(age / lifetime);
	alpha = sampleSignal(Curve,ratio);
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 16384;
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute2 attrib2 = attribBuffer2[index];
					
					float3 local_front = (float3)0;
					float3 local_side = (float3)0;
					float3 local_up = (float3)0;
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					
					VFXBlockFaceCameraPosition( local_front,local_side,local_up,attrib0.position);
					VFXBlockSetColorConstant( local_color,outputUniform0);
					VFXBlockSetAlphaCurveOverLifetime( local_alpha,attrib0.age,attrib2.lifetime,outputUniform1);
					
					float2 size = attrib1.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib0.position;
					
					float2 posOffsets = o.offsets.xy;
					float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
					float3 side = local_side;
					float3 up = local_up;
					
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
				return color;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

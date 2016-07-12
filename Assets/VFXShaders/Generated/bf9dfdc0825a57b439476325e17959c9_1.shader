Shader "Hidden/VFX_1"
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
				float outputUniform1;
				float outputUniform2;
				float3 outputUniform3;
				float2 outputUniform4;
			CBUFFER_END
			
			sampler2D outputSampler0;
			
			sampler2D gradientTexture;
			
			struct Attribute0
			{
				float lifetime;
			};
			
			struct Attribute1
			{
				float3 position;
				float _PADDING_;
			};
			
			struct Attribute2
			{
				float2 size;
			};
			
			struct Attribute3
			{
				float age;
				float texIndex;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<Attribute3> attribBuffer3;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				nointerpolation float flipbookIndex : TEXCOORD1;
			};
			
			float4 sampleSignal(float v,float u) // sample gradient
			{
				return tex2Dlod(gradientTexture,float4(((0.9921875 * saturate(u)) + 0.00390625),v,0,0));
			}
			
			void VFXBlockFixedAxis( inout float3 front,inout float3 side,inout float3 up,float3 position,float3 Axis)
			{
				up = Axis;
	front = VFXCameraPos() - position;
	side = normalize(cross(front,up));
	front = cross(up,side);
			}
			
			void VFXBlockSetColorGradientOverLifetime( inout float3 color,inout float alpha,float age,float lifetime,float Gradient)
			{
				float ratio = saturate(age / lifetime);
	float4 rgba = sampleSignal(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			void VFXBlockSetColorScale( inout float3 color,float Scale)
			{
				color *= Scale;
			}
			
			void VFXBlockSetPivot( inout float3 pivot,float3 Pivot)
			{
				pivot = Pivot;
			}
			
			float2 GetSubUV(int flipBookIndex,float2 uv,float2 dim,float2 invDim)
			{
				float2 tile = float2(fmod(flipBookIndex,dim.x),dim.y - 1.0 - floor(flipBookIndex * invDim.x));
				return (tile + uv) * invDim;
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
					Attribute3 attrib3 = attribBuffer3[index];
					
					float3 local_front = (float3)0;
					float3 local_side = (float3)0;
					float3 local_up = (float3)0;
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					float3 local_pivot = (float3)0;
					
					VFXBlockFixedAxis( local_front,local_side,local_up,attrib1.position,outputUniform0);
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib3.age,attrib0.lifetime,outputUniform1);
					VFXBlockSetColorScale( local_color,outputUniform2);
					VFXBlockSetPivot( local_pivot,outputUniform3);
					
					float2 size = attrib2.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib1.position;
					
					float2 posOffsets = o.offsets.xy - local_pivot.xy;
					
					float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
					float3 front = local_front;
					float3 side = local_side;
					float3 up = local_up;
					
					position += side * (posOffsets.x * size.x);
					position += up * (posOffsets.y * size.y);
					position -= front * local_pivot.z;
					o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
					o.flipbookIndex = attrib3.texIndex;
					
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
				float2 dim = outputUniform4;
				float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU
				float ratio = frac(i.flipbookIndex);
				float index = i.flipbookIndex - ratio;
				
				float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);
				float4 col1 = tex2D(outputSampler0,uv1);
				
				float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);
				float4 col2 = tex2D(outputSampler0,uv2);
				
				color *= lerp(col1,col2,ratio);
				return color;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

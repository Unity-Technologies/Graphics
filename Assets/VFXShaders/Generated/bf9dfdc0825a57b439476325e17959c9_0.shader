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
				float outputUniform2;
				float outputUniform3;
				float2 outputUniform4;
			CBUFFER_END
			
			sampler2D outputSampler0;
			
			sampler2D gradientTexture;
			
			sampler2D curveTexture;
			
			struct Attribute0
			{
				float lifetime;
			};
			
			struct Attribute1
			{
				float angle;
			};
			
			struct Attribute2
			{
				float3 position;
				float age;
			};
			
			struct Attribute3
			{
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
			
			void VFXBlockLookAtPosition( inout float3 front,inout float3 side,inout float3 up,float3 position,float3 Position)
			{
				front = normalize(Position - position);
	side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
	up = cross(side,front);
			}
			
			void VFXBlockSizeOverLifeCurve( inout float2 size,float age,float lifetime,float4 Curve)
			{
				float ratio = saturate(age/lifetime);
	float s = sampleSignal(Curve, ratio);
	size = float2(s,s);
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
					float2 local_size = (float2)0;
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					
					VFXBlockLookAtPosition( local_front,local_side,local_up,attrib2.position,outputUniform0);
					VFXBlockSizeOverLifeCurve( local_size,attrib2.age,attrib0.lifetime,outputUniform1);
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib2.age,attrib0.lifetime,outputUniform2);
					VFXBlockSetColorScale( local_color,outputUniform3);
					
					float2 size = local_size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib2.position;
					
					float2 posOffsets = o.offsets.xy;
					float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
					float3 front = local_front;
					float3 side = local_side;
					float3 up = local_up;
					
					float2 sincosA;
					sincos(radians(attrib1.angle), sincosA.x, sincosA.y);
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

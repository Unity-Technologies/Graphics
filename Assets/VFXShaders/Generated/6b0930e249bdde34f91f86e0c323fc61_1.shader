Shader "Hidden/VFX_1"
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
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float3 outputUniform0;
				float4 outputUniform1;
				float outputUniform2;
				float4 outputUniform3;
				float2 outputUniform4;
			CBUFFER_END
			
			Texture2D outputSampler0Texture;
			SamplerState sampleroutputSampler0Texture;
			
			Texture2D gradientTexture;
			SamplerState samplergradientTexture;
			
			Texture2D curveTexture;
			SamplerState samplercurveTexture;
			
			sampler2D_float _CameraDepthTexture;
			
			struct Attribute0
			{
				float3 position;
				float age;
			};
			
			struct Attribute1
			{
				float angle;
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
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				nointerpolation float flipbookIndex : TEXCOORD1;
				float4 projPos : TEXCOORD2;
			};
			
			float4 sampleSignal(float v,float u) // sample gradient
			{
				return gradientTexture.SampleLevel(samplergradientTexture,float2(((0.9921875 * saturate(u)) + 0.00390625),v),0);
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
				return curveTexture.SampleLevel(samplercurveTexture,float2(uNorm,curveData.z),0)[asuint(curveData.w) & 0x3];
			}
			
			void VFXBlockFaceCameraPosition( inout float3 front,inout float3 side,inout float3 up,float3 position)
			{
				front = normalize(VFXCameraPos() - position);
	side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
	up = cross(side,front);
			}
			
			void VFXBlockSetPivot( inout float3 pivot,float3 Pivot)
			{
				pivot = Pivot;
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
			
			void VFXBlockSubUVAnimateIndexCurve( float age,float lifetime,inout float texIndex,float4 Curve)
			{
				float r = saturate(age/lifetime);
	texIndex = sampleSignal(Curve, r);
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
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute2 attrib2 = attribBuffer2[index];
					
					float3 local_front = (float3)0;
					float3 local_side = (float3)0;
					float3 local_up = (float3)0;
					float3 local_pivot = (float3)0;
					float2 local_size = (float2)0;
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					float local_texIndex = (float)0;
					
					VFXBlockFaceCameraPosition( local_front,local_side,local_up,attrib0.position);
					VFXBlockSetPivot( local_pivot,outputUniform0);
					VFXBlockSizeOverLifeCurve( local_size,attrib0.age,attrib2.lifetime,outputUniform1);
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib0.age,attrib2.lifetime,outputUniform2);
					VFXBlockSubUVAnimateIndexCurve( attrib0.age,attrib2.lifetime,local_texIndex,outputUniform3);
					
					float2 size = local_size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib0.position;
					
					float2 posOffsets = o.offsets.xy - local_pivot.xy;
					
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
					position -= front * local_pivot.z;
					o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
					o.flipbookIndex = local_texIndex;
					
					o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
					o.projPos = ComputeScreenPos(o.pos); // For depth texture fetch
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
				float2 dim = outputUniform4;
				float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU
				float ratio = frac(i.flipbookIndex);
				float index = i.flipbookIndex - ratio;
				
				float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);
				float4 col1 = outputSampler0Texture.Sample(sampleroutputSampler0Texture,uv1);
				
				float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);
				float4 col2 = outputSampler0Texture.Sample(sampleroutputSampler0Texture,uv2);
				
				color *= lerp(col1,col2,ratio);
				
				// Soft particles
				const float INV_FADE_DISTANCE = 1;
				float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				float fade = saturate(INV_FADE_DISTANCE * (sceneZ - i.projPos.w));
				fade = fade * fade * (3.0 - (2.0 * fade)); // Smoothsteping the fade
				color.a *= fade;
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

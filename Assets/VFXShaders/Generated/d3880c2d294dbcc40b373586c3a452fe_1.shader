Shader "Hidden/VFX_1"
{
	SubShader
	{
		Tags { "Queue"="Transparent-1" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
				float2 outputUniform1_kVFXCombine2fOp;
				float2 outputUniform2_kVFXValueOp;
				float outputUniform0_kVFXValueOp;
				
			CBUFFER_END
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			Texture2D gradientTexture;
			SamplerState samplergradientTexture;
			
			sampler2D_float _CameraDepthTexture;
			
			struct Attribute0
			{
				float3 position;
				float age;
			};
			
			struct Attribute1
			{
				float texIndex;
			};
			
			struct Attribute2
			{
				float lifetime;
			};
			
			struct Attribute4
			{
				float2 size;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<Attribute4> attribBuffer4;
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
			
			void VFXBlockSetColorGradientOverLifetime( inout float3 color,inout float alpha,float age,float lifetime,float Gradient)
			{
				float ratio = saturate(age / lifetime);
	float4 rgba = sampleSignal(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			void VFXBlockCameraFade( inout float alpha,float3 position,float2 FadeDistances,inout bool kill)
			{
				float planeDist = VFXNearPlaneDist(position);
	float camFade = smoothstep(FadeDistances.x,FadeDistances.y,planeDist);
	if (camFade == 0.0f)
	    KILL;
	alpha *= camFade;
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
					bool kill = false;
					
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute2 attrib2 = attribBuffer2[index];
					Attribute4 attrib4 = attribBuffer4[index];
					
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib0.age,attrib2.lifetime,outputUniform0_kVFXValueOp);
					VFXBlockCameraFade( local_alpha,attrib0.position,outputUniform1_kVFXCombine2fOp,kill);
					
					float2 size = attrib4.size * 0.5f;
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
					o.flipbookIndex = attrib1.texIndex;
					
					o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
					o.projPos = ComputeScreenPos(o.pos); // For depth texture fetch
					o.col = float4(local_color.xyz,local_alpha);
					if (kill)
					{
						o.pos = -1.0;
						o.col = 0;
					}
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
				float2 dim = outputUniform2_kVFXValueOp;
				float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU
				float ratio = frac(i.flipbookIndex);
				float index = i.flipbookIndex - ratio;
				
				float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);
				float4 col1 = outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,uv1);
				
				float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);
				float4 col2 = outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,uv2);
				
				color *= lerp(col1,col2,ratio);
				
				// Soft particles
				const float INV_FADE_DISTANCE = 0.2;
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

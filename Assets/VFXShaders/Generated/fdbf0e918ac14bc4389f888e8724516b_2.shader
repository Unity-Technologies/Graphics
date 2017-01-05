Shader "Hidden/VFX_2"
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
			
			#define VFX_WORLD_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float3 outputUniform0_kVFXTransformDirOp;
				float outputUniform1_kVFXValueOp;
				
				float outputUniform2_kVFXValueOp;
				float outputUniform3_kVFXValueOp;
				uint2 outputUniforms_PADDING_0;
			
			CBUFFER_END
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			Texture2D gradientTexture;
			SamplerState samplergradientTexture;
			
			sampler2D_float _CameraDepthTexture;
			
			struct OutputData
			{
				float3 position;
				float age;
				float2 size;
				float lifetime;
				uint _PADDING_0;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				float4 projPos : TEXCOORD2;
			};
			
			float4 sampleSignal(float v,float u) // sample gradient
			{
				return gradientTexture.SampleLevel(samplergradientTexture,float2(((0.9921875 * saturate(u)) + 0.00390625),v),0);
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
	float4 rgba = SAMPLE(Gradient,ratio);
	color = rgba.rgb;
	alpha = rgba.a;
			}
			
			void VFXBlockSetAlphaOverLifetime( inout float alpha,float age,float lifetime,float StartAlpha,float EndAlpha)
			{
				float ratio = saturate(age / lifetime);
	alpha = lerp(StartAlpha,EndAlpha,ratio);
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				OutputData outputData = outputBuffer[index];
				
				float3 local_front = (float3)0;
				float3 local_side = (float3)0;
				float3 local_up = (float3)0;
				float3 local_color = (float3)0;
				float local_alpha = (float)0;
				
				VFXBlockFixedAxis( local_front,local_side,local_up,outputData.position,outputUniform0_kVFXTransformDirOp);
				VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,outputData.age,outputData.lifetime,outputUniform1_kVFXValueOp);
				VFXBlockSetAlphaOverLifetime( local_alpha,outputData.age,outputData.lifetime,outputUniform2_kVFXValueOp,outputUniform3_kVFXValueOp);
				
				float2 size = outputData.size * 0.5f;
				o.offsets.x = 2.0 * float(id & 1) - 1.0;
				o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
				
				float3 position = outputData.position;
				
				float2 posOffsets = o.offsets.xy;
				float3 cameraPos = _WorldSpaceCameraPos.xyz;
				float3 side = local_side;
				float3 up = local_up;
				
				position += side * (posOffsets.x * size.x);
				position += up * (posOffsets.y * size.y);
				o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
				
				o.pos = mul (UNITY_MATRIX_VP, float4(position,1.0f));
				o.projPos = ComputeScreenPos(o.pos); // For depth texture fetch
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
				color *= outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,i.offsets);
				
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

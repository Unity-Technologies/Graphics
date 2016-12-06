Shader "Hidden/VFX_3"
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
			
			#define VFX_WORLD_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float outputUniform0;
				float3 outputUniform1;
			CBUFFER_END
			
			Texture2D outputSampler0Texture;
			SamplerState sampleroutputSampler0Texture;
			
			Texture2D gradientTexture;
			SamplerState samplergradientTexture;
			
			struct Attribute0
			{
				float3 velocity;
				float age;
			};
			
			struct Attribute1
			{
				float3 position;
				uint _PADDING_1;
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
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
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
			
			void VFXBlockOrientAlongVelocity( inout float3 front,inout float3 side,inout float3 up,float3 velocity,float3 position)
			{
				up = normalize(velocity);
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
			
			void VFXBlockSubPixelAA( inout float alpha,float3 position,inout float2 size)
			{
				#ifdef VFX_WORLD_SPACE
	float clipPosW = mul(UNITY_MATRIX_VP,float4(position,1.0f)).w;
	#else
	float clipPosW = mul(UNITY_MATRIX_MVP,float4(position,1.0f)).w;
	#endif
	float minSize = clipPosW / (0.5f * min(UNITY_MATRIX_P[0][0] * _ScreenParams.x,-UNITY_MATRIX_P[1][1] * _ScreenParams.y)); // max size in one pixel
	float2 clampedSize = max(size,minSize);
	float fade = (size.x * size.y) / (clampedSize.x * clampedSize.y);
	alpha *= fade;
	size = clampedSize;
			}
			
			void VFXBlockSetPivot( inout float3 pivot,float3 Pivot)
			{
				pivot = Pivot;
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
					
					VFXBlockOrientAlongVelocity( local_front,local_side,local_up,attrib0.velocity,attrib1.position);
					VFXBlockSetColorGradientOverLifetime( local_color,local_alpha,attrib0.age,attrib3.lifetime,outputUniform0);
					VFXBlockSubPixelAA( local_alpha,attrib1.position,attrib2.size);
					VFXBlockSetPivot( local_pivot,outputUniform1);
					
					float2 size = attrib2.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib1.position;
					
					float2 posOffsets = o.offsets.xy - local_pivot.xy;
					
					float3 cameraPos = _WorldSpaceCameraPos.xyz;
					float3 front = local_front;
					float3 side = local_side;
					float3 up = local_up;
					
					position += side * (posOffsets.x * size.x);
					position += up * (posOffsets.y * size.y);
					position -= front * local_pivot.z;
					o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
					
					o.pos = mul (UNITY_MATRIX_VP, float4(position,1.0f));
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
				color *= outputSampler0Texture.Sample(sampleroutputSampler0Texture,i.offsets);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

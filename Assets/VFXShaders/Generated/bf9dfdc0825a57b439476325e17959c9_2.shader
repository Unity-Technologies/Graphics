Shader "Hidden/VFX_2"
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
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "..\VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float outputUniform0;
				float3 outputUniform1;
			CBUFFER_END
			
			Texture2D outputSampler0Texture;
			SamplerState sampleroutputSampler0Texture;
			
			struct Attribute0
			{
				float3 position;
				float _PADDING_;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			
			struct ps_input
			{
				linear noperspective centroid float4 pos : SV_POSITION;
				nointerpolation float4 col : SV_Target0;
				float2 offsets : TEXCOORD0;
			};
			
			void VFXBlockSizeConstantSquare( inout float2 size,float Size)
			{
				size = float2(Size,Size);
			}
			
			void VFXBlockSetColorConstant( inout float3 color,float3 Color)
			{
				color = Color;
			}
			
			void VFXBlockFaceCameraPlane( inout float3 front,inout float3 side,inout float3 up)
			{
				float4x4 cameraMat = VFXCameraMatrix();
	front = -VFXCameraLook();
	side = cameraMat[0].xyz;
	up = cameraMat[1].xyz;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 16384;
				Attribute0 attrib0 = attribBuffer0[index];
				
				float2 local_size = (float2)0;
				float3 local_color = (float3)0;
				float3 local_front = (float3)0;
				float3 local_side = (float3)0;
				float3 local_up = (float3)0;
				
				VFXBlockSizeConstantSquare( local_size,outputUniform0);
				VFXBlockSetColorConstant( local_color,outputUniform1);
				VFXBlockFaceCameraPlane( local_front,local_side,local_up);
				
				float2 size = local_size * 0.5f;
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
				o.col = float4(local_color.xyz,0.5);
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
				if (color.a < 0.33333) discard;
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

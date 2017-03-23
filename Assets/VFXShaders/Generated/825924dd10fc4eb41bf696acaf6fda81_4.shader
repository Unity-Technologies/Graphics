// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/VFX_4"
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
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float3 outputUniform0_kVFXCombine3fOp;
				float outputUniform1_kVFXValueOp;
				
				float outputUniform2_kVFXValueOp;
				uint3 outputUniforms_PADDING_0;
			
			CBUFFER_END
			
			struct OutputData
			{
				float3 position;
				float lifetime;
				float age;
				uint3 _PADDING_0;
			};
			
			StructuredBuffer<OutputData> outputBuffer;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
			};
			
			void VFXBlockSetColorConstant( inout float3 color,float3 Color)
			{
				color = Color;
			}
			
			void VFXBlockSetAlphaOverLifetime( inout float alpha,float age,float lifetime,float StartAlpha,float EndAlpha)
			{
				float ratio = saturate(age / lifetime);
	alpha = lerp(StartAlpha,EndAlpha,ratio);
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = id;
				OutputData outputData = outputBuffer[index];
				
				float3 local_color = (float3)0;
				float local_alpha = (float)0;
				
				VFXBlockSetColorConstant( local_color,outputUniform0_kVFXCombine3fOp);
				VFXBlockSetAlphaOverLifetime( local_alpha,outputData.age,outputData.lifetime,outputUniform1_kVFXValueOp,outputUniform2_kVFXValueOp);
				
				float3 worldPos = outputData.position;
				o.pos = UnityObjectToClipPos(float4(worldPos,1.0f));
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
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

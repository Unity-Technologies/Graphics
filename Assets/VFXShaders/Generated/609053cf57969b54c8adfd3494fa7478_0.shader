// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/VFX_0"
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
			#pragma target 4.5
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			struct Attribute0
			{
				float2 size;
			};
			
			struct Attribute2
			{
				float3 position;
				float alpha;
			};
			
			struct Attribute3
			{
				float3 color;
				uint _PADDING_0;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<Attribute3> attribBuffer3;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
			};
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 2048;
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute2 attrib2 = attribBuffer2[index];
					Attribute3 attrib3 = attribBuffer3[index];
					
					VFXSampler2D outputSampler0_kVFXValueOp = InitSampler(outputSampler0_kVFXValueOpTexture,sampleroutputSampler0_kVFXValueOpTexture);
					
					float2 size = attrib0.size * 0.5f;
					o.offsets.x = 2.0 * float(id & 1) - 1.0;
					o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;
					
					float3 position = attrib2.position;
					
					float2 posOffsets = o.offsets.xy;
					float3 cameraPos = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; // TODO Put that in a uniform!
					float3 side = UNITY_MATRIX_IT_MV[0].xyz;
					float3 up = UNITY_MATRIX_IT_MV[1].xyz;
					
					position += side * (posOffsets.x * size.x);
					position += up * (posOffsets.y * size.y);
					o.offsets.xy = o.offsets.xy * 0.5 + 0.5;
					
					o.pos = UnityObjectToClipPos (float4(position,1.0f));
					o.col = float4(attrib3.color.xyz,attrib2.alpha);
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
				color *= outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,i.offsets);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

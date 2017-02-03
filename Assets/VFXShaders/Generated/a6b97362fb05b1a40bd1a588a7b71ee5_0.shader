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
			
			#define VFX_WORLD_SPACE
			
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "HLSLSupport.cginc"
			#include "../VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float outputUniform0_kVFXValueOp;
				uint3 outputUniforms_PADDING_0;
			
			CBUFFER_END
			
			Texture2D outputSampler0_kVFXValueOpTexture;
			SamplerState sampleroutputSampler0_kVFXValueOpTexture;
			
			sampler2D_float _CameraDepthTexture;
			
			struct Attribute0
			{
				float3 front;
				float lifetime;
			};
			
			struct Attribute1
			{
				float3 up;
				uint _PADDING_0;
			};
			
			struct Attribute2
			{
				float3 side;
				uint _PADDING_0;
			};
			
			struct Attribute3
			{
				float3 position;
				uint _PADDING_0;
			};
			
			struct Attribute4
			{
				float3 color;
				float alpha;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<Attribute3> attribBuffer3;
			StructuredBuffer<Attribute4> attribBuffer4;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float4 color : TEXCOORD0;
				nointerpolation float4 screenToDecal0 : TEXCOORD1;
				nointerpolation float4 screenToDecal1 : TEXCOORD2;
				nointerpolation float4 screenToDecal2 : TEXCOORD3;
				float4 projPos : TEXCOORD7;
			};
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 3) + instanceID * 2018;
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute2 attrib2 = attribBuffer2[index];
					Attribute3 attrib3 = attribBuffer3[index];
					Attribute4 attrib4 = attribBuffer4[index];
					
					
					float3 offsets;
					offsets.x = 2.0 * float(id & 1) - 1.0;
					offsets.y = 2.0 * float((id & 3) >> 1) - 1.0;
					offsets.z = 2.0 * float((id & 7) >> 2) - 1.0;
					
					const float size = 0.01f;
					float maxProjDist = outputUniform0_kVFXValueOp;
					offsets.xy *= size;
					offsets.z *= maxProjDist;
					
					float3 side = attrib2.side;
					float3 up = attrib1.up;
					float3 front = attrib0.front;
					float3 position = attrib3.position;
					
					float3x3 decalRot;
					decalRot[0] = float3(side) * 2.0f / size;
					decalRot[1] = float3(up) * 2.0f / size;
					decalRot[2] = float3(front) * 2.0f / maxProjDist;
					float3 tPos = mul(decalRot, position) + 1.0f;
					
					float4x4 screenToDecal;
					screenToDecal[0] = float4(decalRot[0],-tPos.x);
					screenToDecal[1] = float4(decalRot[1],-tPos.y);
					screenToDecal[2] = float4(decalRot[2],-tPos.z);
					screenToDecal[3] = float4(0,0,0,1);
					
					screenToDecal = mul(screenToDecal, UNITY_MATRIX_I_V);
					
					o.screenToDecal0 = screenToDecal[0];
					o.screenToDecal1 = screenToDecal[1];
					o.screenToDecal2 = screenToDecal[2];
					
					position += side * offsets.x;
					position += up * offsets.y;
					position += front * offsets.z;
					o.pos = mul(UNITY_MATRIX_VP, float4(position,1.0f));
					o.color = float4(offsets * 0.5f + 0.5f,0.5f);
					o.projPos = ComputeScreenPos(o.pos); // For depth texture fetch
					o.col = float4(attrib4.color.xyz,attrib4.alpha);
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
				
				float4x4 screenToDecal;
				screenToDecal[0] = i.screenToDecal0;
				screenToDecal[1] = i.screenToDecal1;
				screenToDecal[2] = i.screenToDecal2;
				screenToDecal[3] = float4(0,0,0,1);
				
				float4 screenPos = i.pos;
				screenPos.xy = screenPos.xy / _ScreenParams.xy * 2.0f - 1.0f;
				screenPos.z = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos));
				screenPos.w = LinearEyeDepth(screenPos.z);
				screenPos.xyz *= screenPos.w;
				
				// Needs inverse P. This works only for standard projections
				screenPos.x /= UNITY_MATRIX_P[0][0];
				screenPos.y /= -UNITY_MATRIX_P[1][1];
				screenPos.z = (screenPos.z - UNITY_MATRIX_P[2][3]) / UNITY_MATRIX_P[2][2];
				screenPos.w = 1.0f;
				
				screenPos = mul(screenToDecal, screenPos) * 0.25f + 0.75f;
				clip(0.5f - abs(screenPos - 0.5f));
				
				color *= outputSampler0_kVFXValueOpTexture.Sample(sampleroutputSampler0_kVFXValueOpTexture,screenPos);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

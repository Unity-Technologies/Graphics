Shader "Hidden/VFX_7"
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
			#include "HLSLSupport.cginc"
			#include "..\VFXCommon.cginc"
			
			CBUFFER_START(outputUniforms)
				float3 outputUniform0;
				float outputUniform1;
				float outputUniform2;
			CBUFFER_END
			
			struct Attribute0
			{
				float3 position;
				float _PADDING_;
			};
			
			struct Attribute1
			{
				float lifetime;
			};
			
			struct Attribute2
			{
				float age;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			StructuredBuffer<int> flags;
			
			struct ps_input
			{
				float4 pos : SV_POSITION;
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
				if (flags[index] == 1)
				{
					Attribute0 attrib0 = attribBuffer0[index];
					Attribute1 attrib1 = attribBuffer1[index];
					Attribute2 attrib2 = attribBuffer2[index];
					
					float3 local_color = (float3)0;
					float local_alpha = (float)0;
					
					VFXBlockSetColorConstant( local_color,outputUniform0);
					VFXBlockSetAlphaOverLifetime( local_alpha,attrib2.age,attrib1.lifetime,outputUniform1,outputUniform2);
					
					float3 worldPos = attrib0.position;
					o.pos = mul(UNITY_MATRIX_MVP, float4(worldPos,1.0f));
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
				return color;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

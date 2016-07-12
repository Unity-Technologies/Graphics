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
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma fragment frag
			
			#define VFX_LOCAL_SPACE
			
			#include "UnityCG.cginc"
			#include "HLSLSupport.cginc"
			#include "..\VFXCommon.cginc"
			
			sampler2D outputSampler0;
			
			struct Attribute0
			{
				float3 position;
				float _PADDING_;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			
			struct ps_input
			{
				float4 pos : SV_POSITION;
				float2 offsets : TEXCOORD0;
			};
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 16384;
				Attribute0 attrib0 = attribBuffer0[index];
				
				
				float2 size = float2(0.005,0.005);
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
				
				o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
				return o;
			}
			
			float4 frag (ps_input i) : COLOR
			{
				float4 color = float4(1.0,1.0,1.0,0.5);
				color *= tex2D(outputSampler0,i.offsets);
				return color;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

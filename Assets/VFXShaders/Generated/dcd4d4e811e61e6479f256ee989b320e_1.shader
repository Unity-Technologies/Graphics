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
				float outputUniform1;
				float2 outputUniform2;
				float outputUniform3;
			CBUFFER_END
			
			Texture2D outputSampler0Texture;
			SamplerState sampleroutputSampler0Texture;
			
			Texture2D outputSampler1Texture;
			SamplerState sampleroutputSampler1Texture;
			
			struct Attribute0
			{
				float3 position;
				float _PADDING_;
			};
			
			struct Attribute1
			{
				float2 size;
			};
			
			struct Attribute2
			{
				float texIndex;
			};
			
			StructuredBuffer<Attribute0> attribBuffer0;
			StructuredBuffer<Attribute1> attribBuffer1;
			StructuredBuffer<Attribute2> attribBuffer2;
			
			struct ps_input
			{
				/*linear noperspective centroid*/ float4 pos : SV_POSITION;
				nointerpolation float4 col : COLOR0;
				float2 offsets : TEXCOORD0;
				nointerpolation float flipbookIndex : TEXCOORD1;
			};
			
			void VFXBlockFaceCameraPlane( inout float3 front,inout float3 side,inout float3 up)
			{
				float4x4 cameraMat = VFXCameraMatrix();
	front = -VFXCameraLook();
	side = cameraMat[0].xyz;
	up = cameraMat[1].xyz;
			}
			
			void VFXBlockSetColorAlphaConstant( inout float3 color,inout float alpha,float3 Color,float Alpha)
			{
				color = Color;
	alpha = Alpha;
			}
			
			float2 GetSubUV(int flipBookIndex,float2 uv,float2 dim,float2 invDim)
			{
				float2 tile = float2(fmod(flipBookIndex,dim.x),dim.y - 1.0 - floor(flipBookIndex * invDim.x));
				return (tile + uv) * invDim;
			}
			
			ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				ps_input o;
				uint index = (id >> 2) + instanceID * 16384;
				Attribute0 attrib0 = attribBuffer0[index];
				Attribute1 attrib1 = attribBuffer1[index];
				Attribute2 attrib2 = attribBuffer2[index];
				
				float3 local_front = (float3)0;
				float3 local_side = (float3)0;
				float3 local_up = (float3)0;
				float3 local_color = (float3)0;
				float local_alpha = (float)0;
				
				VFXBlockFaceCameraPlane( local_front,local_side,local_up);
				VFXBlockSetColorAlphaConstant( local_color,local_alpha,outputUniform0,outputUniform1);
				
				float2 size = attrib1.size * 0.5f;
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
				o.flipbookIndex = attrib2.texIndex;
				
				o.pos = mul (UNITY_MATRIX_MVP, float4(position,1.0f));
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
				float2 dim = outputUniform2;
				float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU
				float ratio = frac(i.flipbookIndex);
				float index = i.flipbookIndex - ratio;
				
				float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);
				float2 duv1 = outputSampler1Texture.Sample(sampleroutputSampler1Texture,uv1).rg - 0.5;
				
				float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);
				float2 duv2 = outputSampler1Texture.Sample(sampleroutputSampler1Texture,uv2).rg - 0.5;
				
				float morphIntensity = outputUniform3;
				duv1 *= morphIntensity * ratio;
				duv2 *= morphIntensity * (ratio - 1.0);
				
				float4 col1 = outputSampler0Texture.Sample(sampleroutputSampler0Texture,uv1 - duv1);
				float4 col2 = outputSampler0Texture.Sample(sampleroutputSampler0Texture,uv2 - duv2);
				
				color *= lerp(col1,col2,ratio);
				
				o.col = color;
				return o;
			}
			
			ENDCG
		}
	}
	FallBack Off
}

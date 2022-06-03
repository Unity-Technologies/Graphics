Shader "Hidden/HDRP/MaterialDepth"
{
    HLSLINCLUDE
		
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
			Cull Back
			ZWrite On
			ZTest Off 
		

			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "GPUDrivenCommon.hlsl"

			//#pragma target 4.5
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan
			#pragma enable_d3d11_debug_symbols

			float2 _ViewportSize;
			Texture2D<uint4> _VisibilityBuffer;

			struct Attributes
			{
				uint vertexID : SV_VertexID;
				DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct FragOut
			{
				float materialID : SV_Depth;
			};


			Varyings Vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
				output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
				return output;
			}

			void Frag(Varyings input, out float materialDepth : SV_Depth)
			{
				//uint2 uv = uint2(input.texcoord.xy * _ViewportSize);
				uint2 uv = input.positionCS.xy;
				uint clusterID = GetClusterID(LOAD_TEXTURE2D(_VisibilityBuffer, uv).r);
				
				uint materialID = GetMaterialID(clusterID);
				materialDepth = asfloat(materialID);
				//materialDepth = 1.0f / (float)materialID;
				//materialDepth = asfloat(0x00F1000F);
				//if (materialID != 0)
				//{
				//	//materialDepth = 1.0f / (float)materialID;
				//	materialDepth = asfloat(materialID);
				//}
				//else
				//{
				//	materialDepth = 0.0f;
				//}
			}

			
            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
    Fallback Off
}

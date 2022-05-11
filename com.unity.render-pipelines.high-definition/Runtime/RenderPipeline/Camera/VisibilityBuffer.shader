Shader "Hidden/HDRP/VisibilityBuffer"
{
    HLSLINCLUDE
		
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {

			//ZWrite On ZTest LEqual Blend Off Cull Off

			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "GPUDrivenCommon.hlsl"	

			#pragma target 4.5
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup
			#pragma enable_d3d11_debug_symbols

			struct AttributesVisibility
			{
				uint vertexID : SV_VertexID;
				DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VaryingsVisibility
			{
				float4 positionCS : SV_POSITION;
				uint2 clusterID : TEXCOORD0;
				float deviceZ : TEXCOORD1;
				uint4 debug : TEXCOORD2;
			};

			struct FragOut
			{
				uint3 visibility : SV_Target0;
				//float actualDepth : SV_Depth;
			};

			Buffer<uint> _BufferSize;

#if defined(PROCEDURAL_INSTANCING_ON)
			void setup()
			{

			}
#endif

			VaryingsVisibility Vert(AttributesVisibility input)
			{
				VaryingsVisibility output = (VaryingsVisibility)0;
				UNITY_SETUP_INSTANCE_ID(input);
#ifdef PROCEDURAL_INSTANCING_ON
                uint vertexID = input.vertexID;
#if defined(SHADER_API_VULKAN)
                vertexID -= 1;
#endif
				VertexData vertexData = GetVertexData(vertexID, input.instanceID);
				//output.positionCS = mul(UNITY_MATRIX_VP, vertexData.worldPos);
				output.positionCS = vertexData.clipPos;
				//output.clusterID.x = vertexData.clusterID;
				output.clusterID.x = vertexData.clusterID + 1 + _BufferSize[kBufferSize_ClusterCW];
				output.clusterID.y = vertexData.indexID + 1;

				output.debug = vertexData.debug;

#if UNITY_UV_STARTS_AT_TOP
				output.positionCS.y = -output.positionCS.y;
#endif
				output.deviceZ = output.positionCS.z / output.positionCS.w;
#else
				output.positionCS = float4(0, 0, -1, 1);
				output.clusterID.x = 23;
				output.clusterID.y = 24;
#endif
				return output;
			}

			FragOut Frag(VaryingsVisibility input)
			{
				FragOut output;

				// start from 1, because 0 is invalid in the visibility buffer
				output.visibility.r = input.clusterID.x;		
				output.visibility.g = input.clusterID.y;
				output.visibility.b = asuint(input.deviceZ);
				return output;
			}

			
            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
    Fallback Off
}

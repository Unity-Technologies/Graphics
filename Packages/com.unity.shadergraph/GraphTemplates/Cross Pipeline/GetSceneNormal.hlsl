#ifndef GET_SCENE_NORMAL
#define GET_SCENE_NORMAL

	#if  !defined(SHADERGRAPH_PREVIEW)
		#if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED) //URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            void GetSceneNormal_float(float2 ScreenPos, out float3 WorldNormal)
            {
                WorldNormal = SampleSceneNormals(ScreenPos);
            }

            void GetSceneNormal_half(float2 ScreenPos, out float3 WorldNormal)
            {
                WorldNormal = SampleSceneNormals(ScreenPos);
            }
			
		#else //HDRP
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
            void GetSceneNormal_float(float2 ScreenPos, out float3 WorldNormal)
            {
                uint2 positionSS = ScreenPos * _ScreenSize.xy;
                NormalData normalData;
                DecodeFromNormalBuffer(positionSS, normalData);
                WorldNormal = normalData.normalWS;
            }
			
		#endif
	#else
		void GetSceneNormal_float(float2 ScreenPos, UnityTexture2D _CameraNormalsTexture, out float3 WorldNormal)
		{
			WorldNormal = float3(0,1,0);
		}
		
		void GetSceneNormal_half(float2 ScreenPos, UnityTexture2D _CameraNormalsTexture, out float3 WorldNormal)
		{
			WorldNormal = float3(0,1,0);
		}
		
	#endif

#endif

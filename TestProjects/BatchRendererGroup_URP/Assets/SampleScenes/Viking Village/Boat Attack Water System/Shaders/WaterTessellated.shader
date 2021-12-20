Shader "BoatAttack/WaterTessellated"
{
	Properties
	{
		_TessellationEdgeLength ("Tessellation Edge Length", Range(5, 100)) = 50
		_BumpScale("Detail Wave Amount", Range(0, 1)) = 0.2//fine detail multiplier
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent-100" "RenderPipeline" = "LightweightPipeline" }
		LOD 300
		ZWrite Off

		Pass
		{
			Name "WaterShading"

			HLSLPROGRAM
			#pragma require tessellation tessHW
            #pragma prefer_hlslcc gles
			/////////////////SHADER FEATURES//////////////////
			#pragma shader_feature _ _TESSELLATION
			#define _TESSELLATION 1
						
			#pragma multi_compile _REFLECTION_CUBEMAP _REFLECTION_PROBES _REFLECTION_PLANARREFLECTION
			#pragma multi_compile _ USE_STRUCTURED_BUFFER
            
            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            
			////////////////////INCLUDES//////////////////////
			#include "WaterCommon.hlsl"
			#include "WaterTessellation.hlsl"

			#pragma vertex TessellationVertex
			#pragma hull Hull
			#pragma domain Domain
			#pragma fragment WaterFragment

			ENDHLSL
		}
	}
	fallback "BoatAttack/Water"
}

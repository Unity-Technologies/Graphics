Shader "Unity/DisneyGGX"
{
	// TODO: Following set of parameters represent the parameters node inside the MaterialGraph. 
	// They are use to fill a SurfaceData. With a MaterialGraph these parameters will not be write here (?).
	Properties
	{
		// Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
		_DiffuseColor("Diffuse", Color) = (1,1,1,1)
		_DiffuseMap("Diffuse", 2D) = "white" {}

		_SpecColor("Specular", Color) = (0.04,0.04,0.04)
		_SpecMap("Specular", 2D) = "white" {}

		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SmoothnessMap("Smoothness", 2D) = "white" {}

		_NormalMap("Normal Map", 2D) = "bump" {}
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5		

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	CGINCLUDE	

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300

		// ------------------------------------------------------------------
		//  forward pass
		Pass
		{
			Name "Forward" // Name is not used
			Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 5.0
			#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev
			
			#pragma vertex VertDefault
			#pragma fragment FragForward
	
			#include "TemplateDisneyGGX.hlsl"
			

			float4 FragForward(PackedVaryings packedInput) : SV_Target
			{
				Varyings input = UnpackVaryings(packedInput);
				float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
				float3 positionWS = input.positionWS;
				SurfaceData surfaceData = GetSurfaceData(input);

				BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

				float4 diffuseLighting;
				float4 specularLighting;
				ForwardLighting(V, positionWS, bsdfData, diffuseLighting, specularLighting);

				return float4(diffuseLighting.rgb + specularLighting.rgb, 1.0);
			}

			ENDCG
		}

		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "GBuffer"  // Name is not used
			Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

			CGPROGRAM
			#pragma target 5.0
			#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev
			
			#pragma vertex VertDefault
			#pragma fragment FragDeferred

			#include "TemplateDisneyGGX.hlsl"

			void FragDeferred(	PackedVaryings packedInput,
								OUTPUT_GBUFFER(outGBuffer)
								)
			{
				Varyings input = UnpackVaryings(packedInput);
				SurfaceData surfaceData = GetSurfaceData(input);
				ENCODE_INTO_GBUFFER(surfaceData, outGBuffer);
			}

			ENDCG
		}
	}
}

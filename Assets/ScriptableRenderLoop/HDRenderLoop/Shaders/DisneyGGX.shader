Shader "Unity/DisneyGGX"
{
	Properties
	{
		// Following set of parameters represent the parameters node inside the MaterialGraph.
		// They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

		// Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
		_BaseColor("BaseColor", Color) = (1,1,1,1) 
		_BaseColorMap("BaseColorMap", 2D) = "white" {}
		_AmbientOcclusionMap("AmbientOcclusion", 2D) = "white" {}

		_Mettalic("Mettalic", Range(0.0, 1.0)) = 0
		_MettalicMap("MettalicMap", 2D) = "white" {}
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SmoothnessMap("SmoothnessMap", 2D) = "white" {}
		_SpecularOcclusionMap("SpecularOcclusion", 2D) = "white" {}

		_NormalMap("NormalMap", 2D) = "bump" {}
		[Enum(TangentSpace, 0, ObjectSpace, 1)] _MaterialID("NormalMap space", Float) = 0

		_DiffuseLightingMap("DiffuseLightingMap", 2D) = "black" {}
		_EmissiveColor("Emissive", Color) = (0, 0, 0)
		_EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
		_EmissiveIntensity("EmissiveIntensity", Float) = 0

		_SubSurfaceRadius("SubSurfaceRadius", Range(0.0, 1.0)) = 0
		//_SubSurfaceRadiusMap("SubSurfaceRadiusMap", 2D) = "white" {}
		//_Thickness("Thickness", Range(0.0, 1.0)) = 0
		//_ThicknessMap("ThicknessMap", 2D) = "white" {}
		//_SubSurfaceProfile("SubSurfaceProfile", Float) = 0
		
		//_CoatCoverage("CoatCoverage", Range(0.0, 1.0)) = 0
		//_CoatCoverageMap("CoatCoverageMapMap", 2D) = "white" {}

		//_CoatRoughness("CoatRoughness", Range(0.0, 1.0)) = 0
		//_CoatRoughnessMap("CoatRoughnessMap", 2D) = "white" {}
				
		// _DistortionVectorMap("DistortionVectorMap", 2D) = "white" {}
		// _DistortionBlur("DistortionBlur", Range(0.0, 1.0)) = 0

		// Following options are for the GUI inspector and different from the input parameters above
		// These option below will cause different compilation flag.		

		[ToggleOff] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[ToggleOff] _DoubleSided("Double Sided", Float) = 1.0
		[ToggleOff] _DoubleSidedLigthing("Double Sided Lighting", Float) = 1.0		

		// Blending state
		[HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
		[HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
		// Material Id
		[HideInInspector] _MaterialId("_MaterialId", FLoat) = 0
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

	//CustomEditor "DisneyGGXGUI"
}

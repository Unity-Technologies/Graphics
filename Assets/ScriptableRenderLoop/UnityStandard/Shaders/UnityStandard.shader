Shader "Unity/UnityStandard"
{
	Properties
	{
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
	
	#include "Material/Material.hlsl"
	#include "ShaderVariables.hlsl"

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "Forward" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 5.0
			#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev
			
			#pragma vertex MainVS
			#pragma fragment MainPS
			
			float4 _DiffuseColor;
			float4 _SpecColor;
			float _Smoothness;
			sampler2D _DiffuseMap;
			sampler2D _NormalMap;

			PunctualLightData _lightData[4];
			float _LightCount; 

			//-------------------------------------------------------------------------------------
			// Input functions

			struct VSInput
			{
				float4 positionOS	: POSITION; // TODO: why do we provide w here ? putting constant to 1 will save a float
				half3 normalOS		: NORMAL;
				float2 uv0			: TEXCOORD0;
				half4 tangentOS		: TANGENT;
			};
			
			struct VSOutput
			{
				float4 positionHS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float2 texCoord0 : TEXCOORD1;
				float4 tangentToWorld[3] : TEXCOORD2; // [3x3:tangentToWorld | 1x3:viewDirForParallax]
			};

			VSOutput MainVS(VSInput input)
			{
				// TODO : here we must support anykind of vertex animation (GPU skinning, morphing) or better do it in compute.
				VSOutput output;

				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				// TODO deal with camera center rendering and instancing (This is the reason why we always perform tow step transform to clip space + instancing matrix)
				output.positionHS = TransformWorldToHClip(output.positionWS);

				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
	
				output.texCoord0 = input.uv0;

			//	#ifdef _TANGENT_TO_WORLD
				float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

				float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
				output.tangentToWorld[0].xyz = tangentToWorld[0];
				output.tangentToWorld[1].xyz = tangentToWorld[1];
				output.tangentToWorld[2].xyz = tangentToWorld[2];
			//	#else
			//	output.tangentToWorld[0].xyz = 0;
			//	output.tangentToWorld[1].xyz = 0;
			//	output.tangentToWorld[2].xyz = normalWS;
			//	#endif

				output.tangentToWorld[0].w = 0;
				output.tangentToWorld[1].w = 0;
				output.tangentToWorld[2].w = 0;

				return output;
			}

			//-------------------------------------------------------------------------------------

			// This function is either hand written or generate by the material graph
			DisneyGGXSurfaceData GetSurfaceData(VSOutput input)
			{
				DisneyGGXSurfaceData data;

				data.diffuseColor = tex2D(_DiffuseMap, input.texCoord0) * _DiffuseColor;
				data.occlusion = 1.0;

				data.specularColor = _SpecColor;
				data.smoothness = _Smoothness;	

				data.normal = UnpackNormalDXT5nm(tex2D(_NormalMap, input.texCoord0));

				return data;
			}

			//-------------------------------------------------------------------------------------

			float4 MainPS(VSOutput input) : SV_Target
			{
				float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

				DisneyGGXSurfaceData surfaceData = GetSurfaceData(input);
				DisneyGGXBSDFData BSDFData = ConvertSurfaceDataToBSDFData(surfaceData);

				float4 outDiffuseLighting;
				float4 outSpecularLighting;

				for (int i = 0; i < _LightCount; ++i)
				{
					float4 diffuseLighting;
					float4 specularLighting;
					EvaluateBSDF_Punctual_DisneyGGX(V, input.positionWS, _lightData[i], BSDFData, diffuseLighting, specularLighting);

					outDiffuseLighting += diffuseLighting;
					outSpecularLighting += specularLighting;
				}

				return outDiffuseLighting + outSpecularLighting;
			}

			ENDCG
		}
	}
}

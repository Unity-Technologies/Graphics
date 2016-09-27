Shader "Hidden/Unity/LightingDeferred" 
{
	Properties
	{
		_SrcBlend("", Float) = 1
		_DstBlend("", Float) = 1
	}
		
	SubShader
	{

		Pass
		{
			ZWrite Off
			Blend[_SrcBlend][_DstBlend]

			CGPROGRAM
			#pragma target 5.0
			#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

			#pragma vertex VertDeferred
			#pragma fragment FragDeferred

			#define UNITY_SHADERRENDERPASS UNITY_SHADERRENDERPASS_DEFERRED
			// CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_DISNEYGXX
			#define UNITY_MATERIAL_DISNEYGXX // Need to be define before including Material.hlsl
			#include "Lighting.hlsl" // This include Material.hlsl
			#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderVariables.hlsl"

			DECLARE_GBUFFER_TEXTURE(_CameraGBufferTexture);
			Texture2D _CameraDepthTexture;
			float4 _ScreenSize;

			float4x4 _InvProjMatrix;

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionHS : SV_POSITION;
			};

			Varyings VertDeferred(Attributes input)
			{
				// TODO: implement SV_vertexID full screen quad
				// Lights are draw as one fullscreen quad
				Varyings output;
				float3 positionWS = TransformObjectToWorld(input.positionOS);
				output.positionHS = TransformWorldToHClip(positionWS);

				return output;
			}

			float4 FragDeferred(Varyings input) : SV_Target
			{
				Coordinate coord = GetCoordinate(input.positionHS.xy, _ScreenSize.zw);

				// No need to manage inverse depth, this is handled by the projection matrix
				float depth = _CameraDepthTexture.Load(uint3(coord.unPositionSS, 0)).x;
				//#if UNITY_REVERSED_Z
				//depth = 1.0 - depth; // This should be in the proj matrix ?
				//#endif
				float3 positionWS = UnprojectToWorld(depth, coord.positionSS, _InvProjMatrix);
				float3 V = GetWorldSpaceNormalizeViewDir(positionWS);

				FETCH_GBUFFER(gbuffer, _CameraGBufferTexture, coord.unPositionSS);
				BSDFData bsdfData = DECODE_FROM_GBUFFER(gbuffer);

				// NOTE: Currently calling the forward loop, same code... :)
				float4 diffuseLighting;
				float4 specularLighting;
				ForwardLighting(V, positionWS, bsdfData, diffuseLighting, specularLighting);

				//return float4(diffuseLighting.rgb + specularLighting.rgb, 1.0);
				return float4(saturate(positionWS), 1.0f);
			}

		ENDCG
		}

	}
	Fallback Off
}

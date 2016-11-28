Shader "Hidden/HDRenderLoop/DebugViewTiles"
{
		SubShader
	{

		Pass
	{
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		HLSLPROGRAM
#pragma target 5.0
#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

#pragma vertex VertViewTiles
#pragma fragment FragViewTiles

#define LIGHTLOOP_TILE_PASS 1
#define USE_FPTL_LIGHTLIST	1		//TODO: make it also work with clustered
#define LIGHTLOOP_TILE_ALL	1


		//-------------------------------------------------------------------------------------
		// Include
		//-------------------------------------------------------------------------------------

#include "Common.hlsl"

		// Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
		// deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem), 
		// the deferred shader will require to use multicompile.
#define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
#include "Assets/ScriptableRenderLoop/HDRenderLoop/ShaderConfig.cs.hlsl"
#include "Assets/ScriptableRenderLoop/HDRenderLoop/ShaderVariables.hlsl"
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/Lighting.hlsl" // This include Material.hlsl

		//-------------------------------------------------------------------------------------
		// variable declaration
		//-------------------------------------------------------------------------------------

	uint _ViewTilesFlags;

	TEXTURE2D(_CameraDepthTexture);
	SAMPLER2D(sampler_CameraDepthTexture);

	float4 VertViewTiles(float3 positionOS : POSITION): SV_POSITION
	{
		return TransformWorldToHClip(TransformObjectToWorld(positionOS));
	}

	float4 FragViewTiles(float4 positionCS : SV_POSITION) : SV_Target
	{
		Coordinate coord = GetCoordinate(positionCS.xy, _ScreenSize.zw);

#if USE_FPTL_LIGHTLIST
		float linearDepth = 0.0f;
#else
		float depth = LOAD_TEXTURE2D(_CameraDepthTexture, coord.unPositionSS).x;
		float linearDepth = GetLinearDepth(depth);
#endif
		int n = 0;
		if(_ViewTilesFlags & DEBUGVIEWTILESFLAGS_DIRECT_LIGHTING)
		{
			uint punctualLightStart;
			uint punctualLightCount;
			GetCountAndStart(coord, DIRECT_LIGHT, linearDepth, punctualLightStart, punctualLightCount);
			n += punctualLightCount;
		}

		if(_ViewTilesFlags & DEBUGVIEWTILESFLAGS_REFLECTION)
		{
			uint envLightStart;
			uint envLightCount;
			GetCountAndStart(coord, REFLECTION_LIGHT, linearDepth, envLightStart, envLightCount);
			n += envLightCount;
		}

		if(n > 0)
		{
			return OverlayHeatMap(int2(coord.unPositionSS.xy) & 15, n);
		}
		else
		{
			return 0.0f;
		}
	}

		ENDHLSL
	}

	}
		Fallback Off
}
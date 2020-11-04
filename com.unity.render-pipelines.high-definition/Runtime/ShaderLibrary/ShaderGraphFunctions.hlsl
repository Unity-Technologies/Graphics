#ifndef UNITY_GRAPHFUNCTIONS_HD_INCLUDED
#define UNITY_GRAPHFUNCTIONS_HD_INCLUDED

// Due to order of includes (Gradient struct need to be define before the declaration of $splice(GraphProperties))
// And HDRP require that Material.hlsl and BuiltInGI are after it, we have two files to defines shader graph functions, one header and one where we setup HDRP functions

float shadergraph_HDSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
    return SampleCameraDepth(uv);
#endif
    return 0;
}

float3 shadergraph_HDSampleSceneColor(float2 uv)
{
#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
    // We always remove the pre-exposure when we sample the scene color
	return SampleCameraColor(uv) * GetInverseCurrentExposureMultiplier();
#endif
    return float3(0, 0, 0);
}

float3 shadergraph_HDBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);
    return SampleBakedGI(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap);
}


// If we already defined the Macro, now we need to redefine them given that HDRP functions are now defined.
#ifdef SHADERGRAPH_SAMPLE_SCENE_DEPTH
#undef SHADERGRAPH_SAMPLE_SCENE_DEPTH
#endif
#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_HDSampleSceneDepth(uv)


#ifdef SHADERGRAPH_SAMPLE_SCENE_COLOR
#undef SHADERGRAPH_SAMPLE_SCENE_COLOR
#endif
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_HDSampleSceneColor(uv)

#ifdef SHADERGRAPH_BAKED_GI
#undef SHADERGRAPH_BAKED_GI
#endif
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_HDBakedGI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling)


#endif // UNITY_GRAPHFUNCTIONS_HD_INCLUDED

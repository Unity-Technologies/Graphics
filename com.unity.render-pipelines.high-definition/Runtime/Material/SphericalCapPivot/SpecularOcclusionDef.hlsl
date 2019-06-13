#ifndef UNITY_SPECULAR_OCCLUSION_DEF_INCLUDED
#define UNITY_SPECULAR_OCCLUSION_DEF_INCLUDED

// Specular occlusion methods:
#define SPECULAR_OCCLUSION_DISABLED -1
#define SPECULAR_OCCLUSION_FROM_AO 0
#define SPECULAR_OCCLUSION_CONECONE 1
#define SPECULAR_OCCLUSION_SPTD 2

// Choice of formulas to infer bent visibility: see SPTDistribution.hlsl : GetBentVisibility()
#define BENT_VISIBILITY_FROM_AO_UNIFORM 0
#define BENT_VISIBILITY_FROM_AO_COS 1
// see SPTDistribution.hlsl:ApplyBentSpecularOcclusionFixups() if other methods are added
#define BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION 2

// See StackLit.hlsl: direction to use for bent visibility cone
#define BENT_VISIBILITY_DIR_GEOM_NORMAL 0
#define BENT_VISIBILITY_DIR_BENT_NORMAL 1
#define BENT_VISIBILITY_DIR_SHADING_NORMAL 2

// Specular occlusion fixup methods to handle noisy bent normal maps
// (flags)
#define BENT_VISIBILITY_FIXUP_FLAGS_NONE 0
#define BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS    (1<<0)
#define BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM (1<<1)

#endif // #define UNITY_SPECULAR_OCCLUSION_DEF_INCLUDED

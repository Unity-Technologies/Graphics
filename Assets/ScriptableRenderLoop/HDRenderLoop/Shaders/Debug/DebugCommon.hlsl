
// List of material debug modes. Keep in sync with HDRenderLoop.MaterialDebugMode
#define MaterialDebugNone 0

#define MaterialDebugDepth 1
#define MaterialDebugTexCoord0 2
#define MaterialDebugVertexNormalWS 3
#define MaterialDebugVertexTangentWS 4
#define MaterialDebugVertexBitangentWS 5

#define MaterialDebugBakeDiffuseLighting 100
#define MaterialDebugEmissiveColor 101
#define MaterialDebugEmissiveIntensity 102
#define MaterialDebugVelocity 103
#define MaterialDebugDistortion 104
#define MaterialDebugDistortionBlur 105

#define MaterialDebugBaseColor 1001
#define MaterialDebugSpecularOcclusion 1002
#define MaterialDebugNormalWS 1003
#define MaterialDebugPerceptualSmoothness 1004
#define MaterialDebugMaterialId 1005
#define MaterialDebugAmbientOcclusion 1006
#define MaterialDebugTangentWS 1007
#define MaterialDebugAnisotropy 1008
#define MaterialDebugMetalic 1009
#define MaterialDebugSpecular 1010
#define MaterialDebugSubSurfaceRadius 1011
#define MaterialDebugThickness 1012
#define MaterialDebugSubSurfaceProfile 1013
#define MaterialDebugCoatNormalWS 1014
#define MaterialDebugCoatPerceptualSmoothness 1015
#define MaterialDebugSpecularColor 1016

// List of GBuffer debug modes. Keep in sync with HDRenderLoop.GBufferDebugMode
#define GBufferDebugNone 0
#define GBufferDebugDiffuseColor 1
#define GBufferDebugNormal 2
#define GBufferDebugDepth 3
#define GBufferDebugBakedDiffuse 4
#define GBufferDebugSpecularColor 5
#define GBufferDebugSpecularOcclustion 6
#define GBufferDebugSmoothness 7
#define GBufferDebugMaterialId 8

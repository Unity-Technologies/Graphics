#if (SHADERPASS != SHADERPASS_DEPTHONLY) && (SHADERPASS != SHADERPASS_DBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DBUFFER_MESH) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_MESH) && (SHADERPASS != SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) && (SHADERPASS != SHADERPASS_DECAL_SCREEN_SPACE_MESH) && (SHADERPASS != SHADERPASS_DECAL_GBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DECAL_GBUFFER_MESH)
#error SHADERPASS_is_not_correctly_define
#endif

#if !defined(SHADERPASS)
#error SHADERPASS_is_not_define
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
#define DECAL_PROJECTOR
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
#define DECAL_MESH
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
#define DECAL_DBUFFER
#endif

#if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
#define DECAL_SCREEN_SPACE
#endif

#if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
#define DECAL_GBUFFER
#endif

#if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
#define DECAL_FORWARD_EMISSIVE
#endif

//#define DECAL_ANGLE_FADE

#if defined(DECAL_SCREEN_SPACE) || defined(DECAL_ANGLE_FADE)
#define DECAL_RECONSTRUCT_NORMAL
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl" // VertMesh

#if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // Load Scene Depth
#endif

#ifdef DECAL_PROJECTOR // TODO
#include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/DecalPrepassBuffer.hlsl"
#endif
#ifdef DECAL_MESH
#include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/DecalMeshBiasTypeEnum.cs.hlsl"
#endif
#ifdef DECAL_SCREEN_SPACE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif
#ifdef DECAL_GBUFFER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#endif

#ifdef DECAL_RECONSTRUCT_NORMAL

#if defined(USING_STEREO_MATRICES)
#define unity_eyeIndex unity_StereoEyeIndex
#else
#define unity_eyeIndex 0
#endif

float4 _SourceSize;
float4 _ProjectionParams2;
float4x4 _CameraViewProjections[2]; // This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.
float4 _CameraViewTopLeftCorner[2]; // TODO: check if we can use half type
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

#ifdef DECALS_NORMAL_BLEND_LOW
    #define _RECONSTRUCT_NORMAL_LOW
#endif

#ifdef DECALS_NORMAL_BLEND_MEDIUM
    #define _RECONSTRUCT_NORMAL_MEDIUM
#endif

float RawToLinearDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
#if UNITY_REVERSED_Z
    return ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
#else
    return ((_ProjectionParams.z - _ProjectionParams.y) * (rawDepth)+_ProjectionParams.y);
#endif
#else
    return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float SampleAndGetLinearDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv.xy).r;
    return RawToLinearDepth(rawDepth);
}

// This returns a vector in world unit (not a position), from camera to the given point described by uv screen coordinate and depth (in absolute world unit).
float3 ReconstructViewPos(float2 uv, float depth)
{
    // Screen is y-inverted.
    uv.y = 1.0 - uv.y;

    // view pos in world space
#if defined(_ORTHOGRAPHIC)
    float zScale = depth * _ProjectionParams.w; // divide by far plane
    float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
        + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
        + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y
        + _CameraViewZExtent[unity_eyeIndex].xyz * zScale;
#else
    float zScale = depth * _ProjectionParams2.x; // divide by near plane
    float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
        + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
        + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y;
    viewPos *= zScale;
#endif

    return viewPos;
}

// Try reconstructing normal accurately from depth buffer.
// Low:    DDX/DDY on the current pixel
// Medium: 3 taps on each direction | x | * | y |
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormal(float2 uv, float depth, float3 vpos)
{
#if defined(_RECONSTRUCT_NORMAL_LOW)
    return normalize(cross(ddy(vpos), ddx(vpos)));
#else
    float2 delta = _SourceSize.zw * 2.0;

    // Sample the neighbour fragments
    float2 lUV = float2(-delta.x, 0.0);
    float2 rUV = float2(delta.x, 0.0);
    float2 uUV = float2(0.0, delta.y);
    float2 dUV = float2(0.0, -delta.y);

    float3 l1 = float3(uv + lUV, 0.0); l1.z = SampleAndGetLinearDepth(l1.xy); // Left1
    float3 r1 = float3(uv + rUV, 0.0); r1.z = SampleAndGetLinearDepth(r1.xy); // Right1
    float3 u1 = float3(uv + uUV, 0.0); u1.z = SampleAndGetLinearDepth(u1.xy); // Up1
    float3 d1 = float3(uv + dUV, 0.0); d1.z = SampleAndGetLinearDepth(d1.xy); // Down1

    // Determine the closest horizontal and vertical pixels...
    // horizontal: left = 0.0 right = 1.0
    // vertical  : down = 0.0    up = 1.0
#if defined(_RECONSTRUCT_NORMAL_MEDIUM)
    uint closest_horizontal = l1.z > r1.z ? 0 : 1;
    uint closest_vertical = d1.z > u1.z ? 0 : 1;
#else
    float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearDepth(l2.xy); // Left2
    float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearDepth(r2.xy); // Right2
    float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearDepth(u2.xy); // Up2
    float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearDepth(d2.xy); // Down2

    const uint closest_horizontal = abs((2.0 * l1.z - l2.z) - depth) < abs((2.0 * r1.z - r2.z) - depth) ? 0 : 1;
    const uint closest_vertical = abs((2.0 * d1.z - d2.z) - depth) < abs((2.0 * u1.z - u2.z) - depth) ? 0 : 1;
#endif


    // Calculate the triangle, in a counter-clockwize order, to
    // use based on the closest horizontal and vertical depths.
    // h == 0.0 && v == 0.0: p1 = left,  p2 = down
    // h == 1.0 && v == 0.0: p1 = down,  p2 = right
    // h == 1.0 && v == 1.0: p1 = right, p2 = up
    // h == 0.0 && v == 1.0: p1 = up,    p2 = left
    // Calculate the view space positions for the three points...
    float3 P1;
    float3 P2;
    if (closest_vertical == 0)
    {
        P1 = closest_horizontal == 0 ? l1 : d1;
        P2 = closest_horizontal == 0 ? d1 : r1;
    }
    else
    {
        P1 = closest_horizontal == 0 ? u1 : r1;
        P2 = closest_horizontal == 0 ? l1 : u1;
    }

    P1 = ReconstructViewPos(P1.xy, P1.z);
    P2 = ReconstructViewPos(P2.xy, P2.z);

    // Use the cross product to calculate the normal...
    return normalize(cross(P2 - vpos, P1 - vpos));
#endif
}
#endif

void MeshDecalsPositionZBias(inout Varyings input)
{
#if UNITY_REVERSED_Z
    input.positionCS.z -= _DecalMeshDepthBias;
#else
    input.positionCS.z += _DecalMeshDepthBias;
#endif
}

PackedVaryings Vert(Attributes inputMesh)
{
    Varyings varyingsType;
#ifdef DECAL_MESH

    float3 worldSpaceBias = 0.0f;
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
    {
        float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
        float3 V = GetWorldSpaceNormalizeViewDir(positionRWS);

        worldSpaceBias = V * (_DecalMeshViewBias);
    }
    varyingsType = BuildVaryings(inputMesh);
    varyingsType.positionWS += worldSpaceBias;
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
    {
        MeshDecalsPositionZBias(varyingsType);
    }
#else
    varyingsType = BuildVaryings(inputMesh);
#endif
    return PackVaryings(varyingsType);
}

#ifdef SCENEPICKINGPASS
float4 FragSelection() : SV_Target0
{
    return _SelectionID;
}
#endif

void Frag(PackedVaryings packedInput,
#if defined(DECAL_DBUFFER)
    OUTPUT_DBUFFER(outDBuffer)
#elif defined(DECAL_SCREEN_SPACE)
    out float4 outColor : SV_Target0
#elif defined(DECAL_GBUFFER)
    out FragmentOutput fragmentOutput
#elif defined(DECAL_FORWARD_EMISSIVE)
    out float4 outEmissive : SV_Target0
#elif defined(SCENEPICKINGPASS)
    out float4 outColor : SV_Target0
#else
#error SHADERPASS_is_not_correctly_define
#endif
)
{
#ifdef SCENEPICKINGPASS
    outColor = _SelectionID;
#else
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    UNITY_SETUP_INSTANCE_ID(packedInput)
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);
    DecalSurfaceData surfaceData;
    float clipValue = 1.0;
    float angleFadeFactor = 1.0;

#ifdef DECAL_PROJECTOR
    float depth = LoadSceneDepth(input.positionSS.xy);
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

    // Decal layer mask accepted by the receiving material
    DecalPrepassData material;
    ZERO_INITIALIZE(DecalPrepassData, material);
    if (false) //if (_EnableDecalLayers)
    {
        // Clip the decal if it does not pass the decal layer mask of the receiving material.
        // Decal layer of the decal
        uint decalLayerMask = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal).x);

        DecodeFromDecalPrepass(posInput.positionSS, material);

        if ((decalLayerMask & material.decalLayerMask) == 0)
            clipValue -= 2.0;
    }

    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(posInput.positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5, 0.5);
    if (!(all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
    {
        clipValue -= 2.0; // helper lanes will be clipped
    }

    // call clip as early as possible
#ifndef SHADER_API_METAL
    // Calling clip here instead of inside the condition above shouldn't make any performance difference
    // but case save one clip call.
    clip(clipValue);
#else
    // Metal Shading Language declares that fragment discard invalidates
    // derivatives for the rest of the quad, so we need to reorder when
    // we discard during decal projection, or we get artifacts along the
    // edges of the projection(any partial quads get bad partial derivatives
    //regardless of whether they are computed implicitly or explicitly).
    if (clipValue > 0.0)
    {
#endif
    input.texCoord0.xy = positionDS.xz;
    input.texCoord1.xy = positionDS.xz;
    input.texCoord2.xy = positionDS.xz;
    input.texCoord3.xy = positionDS.xz;

    float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

    // For now we only allow angle fading when decal layers are enabled
    // TODO: Reconstructing normal from depth buffer result in poor result.
    // We may revisit it in the future
    // This is example code:  float3 vtxNormal = normalize(cross(ddy(posInput.positionWS), ddx(posInput.positionWS)));
    // But better implementation (and costlier) can be find here: https://atyuwen.github.io/posts/normal-reconstruction/
    if (false) //if (_EnableDecalLayers)
    {
        // Check if this decal projector require angle fading
        float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
        float2 angleFade = float2(normalToWorld[1][3], normalToWorld[2][3]);

        if (angleFade.y < 0.0f) // if angle fade is enabled
        {
            float3 decalNormal = float3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
            float dotAngle = dot(material.geomNormalWS, decalNormal);
            // See equation in DecalSystem.cs - simplified to a madd mul add here
            angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
        }
    }

#ifdef DECAL_RECONSTRUCT_NORMAL
    float linearDepth = RawToLinearDepth(depth);
    float3 vpos = ReconstructViewPos(posInput.positionNDC, linearDepth);
    float3 normalWS = ReconstructNormal(posInput.positionNDC, linearDepth, vpos);
#endif

#ifdef DECAL_ANGLE_FADE
    // Check if this decal projector require angle fading
    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
    float2 angleFade = float2(normalToWorld[1][3], normalToWorld[2][3]);

    if (angleFade.y < 0.0f) // if angle fade is enabled
    {
        float3 decalNormal = float3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
        float dotAngle = dot(normalWS, decalNormal);
        // See equation in DecalSystem.cs - simplified to a madd mul add here
        angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
    }
#endif

#else // Decal mesh
    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, uint2(0, 0));

#ifdef DECAL_RECONSTRUCT_NORMAL
    float depth = LoadSceneDepth(input.positionSS.xy);

    float linearDepth = RawToLinearDepth(depth);
    float3 vpos = ReconstructViewPos(posInput.positionNDC, linearDepth);
    float3 normalWS = ReconstructNormal(posInput.positionNDC, linearDepth, vpos);
#endif

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
    #else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
    #endif
#endif

    GetSurfaceData(input, V, posInput, angleFadeFactor, surfaceData);

#if defined(DECAL_PROJECTOR) && defined(SHADER_API_METAL)
    } // if (clipValue > 0.0)

    clip(clipValue);
#endif

#if defined(DECAL_DBUFFER)
    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
#elif defined(DECAL_SCREEN_SPACE)
    InputData inputData;
    inputData.positionWS = posInput.positionWS;

#if defined(DECALS_NORMAL_BLEND_LOW) || defined(DECALS_NORMAL_BLEND_MEDIUM) || defined(DECALS_NORMAL_BLEND_HIGH)

    //float linearDepth = RawToLinearDepth(depth);
    //float3 vpos = ReconstructViewPos(posInput.positionNDC, linearDepth);
    //float3 normalWS = ReconstructNormal(posInput.positionNDC, linearDepth, vpos);

    //inputData.normalWS = lerp(normalWS, BlendNormalWorldspaceRNM(normalWS, surfaceData.normalWS.xyz, decalNormal2), surfaceData.normalWS.w); // todo: detailMask should lerp the angle of the quaternion rotation, not the normals
    float normalAlpha = 1.0 - surfaceData.normalWS.w;
    float3 decalNormal = surfaceData.normalWS.xyz * surfaceData.normalWS.w;// ((surfaceData.normalWS.xyz * 0.5 + 0.5) * surfaceData.normalWS.w) * 2 - (254.0 / 255.0); // TODO
    inputData.normalWS = normalize(normalWS * normalAlpha + decalNormal);
    //inputData.normalWS = normalize(lerp(normalWS, decalNormal, normalAlpha));
#else
    inputData.normalWS = surfaceData.normalWS;
#endif

    inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS); // TODO: Normalize?

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = 0;// input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = 0;// input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = 0;// SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = 0;// GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = 0;// SAMPLE_SHADOWMASK(input.lightmapUV);

    SurfaceData surface = (SurfaceData)0;
    surface.albedo = surfaceData.baseColor.rgb;
    surface.metallic = saturate(surfaceData.mask.x);
    surface.specular = 0;
    surface.smoothness = saturate(surfaceData.mask.z);
    surface.occlusion = surfaceData.mask.y;
    surface.emission = surfaceData.emissive;
    surface.alpha = saturate(surfaceData.baseColor.w);
    surface.clearCoatMask = 0;
    surface.clearCoatSmoothness = 1;

    half4 color = UniversalFragmentPBR(inputData, surface);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    outColor = color;
#elif defined(DECAL_GBUFFER)
    half3 packedNormalWS = PackNormal(surfaceData.normalWS.xyz);

    fragmentOutput.GBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
    fragmentOutput.GBuffer1 = 0;
    fragmentOutput.GBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
    fragmentOutput.GBuffer3 = half4(surfaceData.emissive, surfaceData.baseColor.a);
#if OUTPUT_SHADOWMASK
    fragmentOutput.GBuffer4 = 0; // TODO: Does shader and pipeline target count must match?
#endif
#elif defined(DECAL_FORWARD_EMISSIVE)
    // Emissive need to be pre-exposed
    outEmissive.rgb = surfaceData.emissive;// *GetCurrentExposureMultiplier();
    outEmissive.a = 1.0;
#else
#endif
#endif
}

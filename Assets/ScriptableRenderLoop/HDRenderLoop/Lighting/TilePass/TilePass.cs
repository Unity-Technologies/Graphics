using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    namespace TilePass
    {
        //-----------------------------------------------------------------------------
        // structure definition
        //-----------------------------------------------------------------------------

        [GenerateHLSL]
        public enum LightVolumeType
        {
            Cone,
            Sphere,
            Box,
            Count
        }

        [GenerateHLSL]
        public enum LightCategory
        {
            Punctual,
            Area,
            Env,
            Count
        }

        [GenerateHLSL]
        public class LightDefinitions
        {
            public static int MAX_NR_LIGHTS_PER_CAMERA = 1024;
            public static int MAX_NR_BIGTILE_LIGHTS_PLUSONE = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
            public static float VIEWPORT_SCALE_Z = 1.0f;

            // enable unity's original left-hand shader camera space (right-hand internally in unity).
            public static int USE_LEFTHAND_CAMERASPACE = 0;

            // flags
            public static int IS_CIRCULAR_SPOT_SHAPE = 1;
            public static int HAS_COOKIE_TEXTURE = 2;
            public static int IS_BOX_PROJECTED = 4;
            public static int HAS_SHADOW = 8;
        }

        [GenerateHLSL]
        public struct SFiniteLightBound
        {
            public Vector3 boxAxisX;
            public Vector3 boxAxisY;
            public Vector3 boxAxisZ;
            public Vector3 center;        // a center in camera space inside the bounding volume of the light source.
            public Vector2 scaleXY;
            public float radius;
        };

        [GenerateHLSL]
        public struct LightVolumeData
        {
            public Vector3 lightPos;
            public uint lightVolume;

            public Vector3 lightAxisX;
            public uint lightCategory;

            public Vector3 lightAxisY;
            public float radiusSq;

            public Vector3 lightAxisZ;      // spot +Z axis
            public float cotan;

            public Vector3 boxInnerDist;
            public float unused;

            public Vector3 boxInvRange;
            public float unused2;
        };

        public class LightLoop : BaseLightLoop
        {
            public const int k_MaxDirectionalLightsOnSCreen = 10;
            public const int k_MaxPunctualLightsOnSCreen = 512;
            public const int k_MaxAreaLightsOnSCreen = 128;
            public const int k_MaxLightsOnSCreen = k_MaxDirectionalLightsOnSCreen + k_MaxPunctualLightsOnSCreen + k_MaxAreaLightsOnSCreen;
            public const int k_MaxEnvLightsOnSCreen = 64;
            public const int k_MaxShadowOnScreen = 16;
            public const int k_MaxCascadeCount = 4; //Should be not less than m_Settings.directionalLightCascadeCount;

            // Static keyword is required here else we get a "DestroyBuffer can only be call in main thread"
            static ComputeBuffer s_DirectionalLightDatas = null;
            static ComputeBuffer s_LightDatas = null;
            static ComputeBuffer s_EnvLightDatas = null;
            static ComputeBuffer s_shadowDatas = null;

            TextureCacheCubemap m_CubeReflTexArray;
            TextureCache2D m_CookieTexArray;
            TextureCacheCubemap m_CubeCookieTexArray;

            public class LightList
            {
                public List<DirectionalLightData> directionalLights;
                public List<LightData> lights;
                public List<EnvLightData> envLights;
                public List<ShadowData> shadows;
                public Vector4[] directionalShadowSplitSphereSqr;

                public List<SFiniteLightBound> bounds;
                public List<LightVolumeData> lightVolumes;

                public void Clear()
                {
                    directionalLights.Clear();
                    lights.Clear();
                    envLights.Clear();
                    shadows.Clear();

                    bounds.Clear();
                    lightVolumes.Clear();
                }

                public void Allocate()
                {
                    directionalLights = new List<DirectionalLightData>();
                    lights = new List<LightData>();
                    envLights = new List<EnvLightData>();
                    shadows = new List<ShadowData>();
                    directionalShadowSplitSphereSqr = new Vector4[k_MaxCascadeCount];

                    bounds = new List<SFiniteLightBound>();
                    lightVolumes = new List<LightVolumeData>();
                }
            }

            LightList m_lightList;
            int m_punctualLightCount = 0;
            int m_areaLightCount = 0;
            int m_lightCount = 0;

            static ComputeShader buildScreenAABBShader = null;
            static ComputeShader buildPerTileLightListShader = null;     // FPTL
            static ComputeShader buildPerBigTileLightListShader = null;
            static ComputeShader buildPerVoxelLightListShader = null;    // clustered

            static int s_GenAABBKernel;
            static int s_GenListPerTileKernel;
            static int s_GenListPerVoxelKernel;
            static int s_ClearVoxelAtomicKernel;
            static ComputeBuffer s_LightVolumeDataBuffer = null;
            static ComputeBuffer s_ConvexBoundsBuffer = null;
            static ComputeBuffer s_AABBBoundsBuffer = null;
            static ComputeBuffer s_LightList = null;

            static ComputeBuffer s_BigTileLightList = null;        // used for pre-pass coarse culling on 64x64 tiles
            static int s_GenListPerBigTileKernel;

            public bool enableDrawLightBoundsDebug = false;
            public bool disableTileAndCluster = true; // For debug / test
            public bool enableSplitLightEvaluation = true;
            public bool enableComputeLightEvaluation = false;

            // clustered light list specific buffers and data begin
            public int debugViewTilesFlags = 0;
            public bool enableClustered = false;
            public bool disableFptlWhenClustered = true;    // still useful on opaques. Should be false by default to force tile on opaque.
            public bool enableBigTilePrepass = true;
            const bool k_UseDepthBuffer = true;      // only has an impact when EnableClustered is true (requires a depth-prepass)
            const bool k_UseAsyncCompute = true;        // should not use on mobile

            const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
            const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
            float m_ClustScale;
            static ComputeBuffer s_PerVoxelLightLists = null;
            static ComputeBuffer s_PerVoxelOffset = null;
            static ComputeBuffer s_PerTileLogBaseTweak = null;
            static ComputeBuffer s_GlobalLightListAtomic = null;
            // clustered light list specific buffers and data end

            bool usingFptl
            {
                get
                {
                    bool isEnabledMSAA = false;
                    Debug.Assert(!isEnabledMSAA || enableClustered);
                    bool disableFptl = (disableFptlWhenClustered && enableClustered) || isEnabledMSAA;
                    return !disableFptl;
                }
            }

            Material m_DeferredDirectMaterial = null;
            Material m_DeferredIndirectMaterial = null;
            Material m_DeferredAllMaterial = null;
            Material m_DebugViewTilesMaterial = null;

            Material m_SingleDeferredMaterial = null;

            const int k_TileSize = 16;

            int GetNumTileX(Camera camera)
            {
                return (camera.pixelWidth + (k_TileSize - 1)) / k_TileSize;
            }

            int GetNumTileY(Camera camera)
            {
                return (camera.pixelHeight + (k_TileSize - 1)) / k_TileSize;
            }

            public override void Rebuild(TextureSettings textureSettings)
            {
                m_lightList = new LightList();
                m_lightList.Allocate();

                s_DirectionalLightDatas = new ComputeBuffer(k_MaxDirectionalLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
                s_LightDatas = new ComputeBuffer(k_MaxPunctualLightsOnSCreen + k_MaxAreaLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
                s_EnvLightDatas = new ComputeBuffer(k_MaxEnvLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
                s_shadowDatas = new ComputeBuffer(k_MaxCascadeCount + k_MaxShadowOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowData)));

                m_CookieTexArray = new TextureCache2D();
                m_CookieTexArray.AllocTextureArray(8, textureSettings.spotCookieSize, textureSettings.spotCookieSize, TextureFormat.RGBA32, true);
                m_CubeCookieTexArray = new TextureCacheCubemap();
                m_CubeCookieTexArray.AllocTextureArray(4, textureSettings.pointCookieSize, TextureFormat.RGBA32, true);
                m_CubeReflTexArray = new TextureCacheCubemap();
                m_CubeReflTexArray.AllocTextureArray(32, textureSettings.reflectionCubemapSize, TextureFormat.BC6H, true);

                buildScreenAABBShader = Resources.Load<ComputeShader>("scrbound");
                buildPerTileLightListShader = Resources.Load<ComputeShader>("lightlistbuild");
                buildPerBigTileLightListShader = Resources.Load<ComputeShader>("lightlistbuild-bigtile");
                buildPerVoxelLightListShader = Resources.Load<ComputeShader>("lightlistbuild-clustered");

                s_GenAABBKernel = buildScreenAABBShader.FindKernel("ScreenBoundsAABB");
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(enableBigTilePrepass ? "TileLightListGen_SrcBigTile" : "TileLightListGen");
                s_AABBBoundsBuffer = new ComputeBuffer(2 * k_MaxLightsOnSCreen, 3 * sizeof(float));
                s_ConvexBoundsBuffer = new ComputeBuffer(k_MaxLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
                s_LightVolumeDataBuffer = new ComputeBuffer(k_MaxLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));

                buildScreenAABBShader.SetBuffer(s_GenAABBKernel, "g_data", s_ConvexBoundsBuffer);
                buildPerTileLightListShader.SetBuffer(s_GenListPerTileKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                buildPerTileLightListShader.SetBuffer(s_GenListPerTileKernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                buildPerTileLightListShader.SetBuffer(s_GenListPerTileKernel, "g_data", s_ConvexBoundsBuffer);

                if (enableClustered)
                {
                    var kernelName = enableBigTilePrepass ? (k_UseDepthBuffer ? "TileLightListGen_DepthRT_SrcBigTile" : "TileLightListGen_NoDepthRT_SrcBigTile") : (k_UseDepthBuffer ? "TileLightListGen_DepthRT" : "TileLightListGen_NoDepthRT");
                    s_GenListPerVoxelKernel = buildPerVoxelLightListShader.FindKernel(kernelName);
                    s_ClearVoxelAtomicKernel = buildPerVoxelLightListShader.FindKernel("ClearAtomic");
                    buildPerVoxelLightListShader.SetBuffer(s_GenListPerVoxelKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                    buildPerVoxelLightListShader.SetBuffer(s_GenListPerVoxelKernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                    buildPerVoxelLightListShader.SetBuffer(s_GenListPerVoxelKernel, "g_data", s_ConvexBoundsBuffer);

                    s_GlobalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
                }

                if (enableBigTilePrepass)
                {
                    s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");
                    buildPerBigTileLightListShader.SetBuffer(s_GenListPerBigTileKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                    buildPerBigTileLightListShader.SetBuffer(s_GenListPerBigTileKernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                    buildPerBigTileLightListShader.SetBuffer(s_GenListPerBigTileKernel, "g_data", s_ConvexBoundsBuffer);
                }

                s_LightList = null;

                m_DeferredDirectMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
                m_DeferredDirectMaterial.EnableKeyword("LIGHTLOOP_TILE_PASS");
                m_DeferredDirectMaterial.EnableKeyword("LIGHTLOOP_TILE_DIRECT");
                m_DeferredDirectMaterial.DisableKeyword("LIGHTLOOP_TILE_INDIRECT");
                m_DeferredDirectMaterial.DisableKeyword("LIGHTLOOP_TILE_ALL");

                m_DeferredIndirectMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
                m_DeferredIndirectMaterial.EnableKeyword("LIGHTLOOP_TILE_PASS");
                m_DeferredIndirectMaterial.DisableKeyword("LIGHTLOOP_TILE_DIRECT");
                m_DeferredIndirectMaterial.EnableKeyword("LIGHTLOOP_TILE_INDIRECT");
                m_DeferredIndirectMaterial.DisableKeyword("LIGHTLOOP_TILE_ALL");

                m_DeferredAllMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
                m_DeferredAllMaterial.EnableKeyword("LIGHTLOOP_TILE_PASS");
                m_DeferredAllMaterial.DisableKeyword("LIGHTLOOP_TILE_DIRECT");
                m_DeferredAllMaterial.DisableKeyword("LIGHTLOOP_TILE_INDIRECT");
                m_DeferredAllMaterial.EnableKeyword("LIGHTLOOP_TILE_ALL");

                m_DebugViewTilesMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/DebugViewTiles");

                m_SingleDeferredMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
                m_SingleDeferredMaterial.EnableKeyword("LIGHTLOOP_SINGLE_PASS");
            }

            public override void Cleanup()
            {
                Utilities.SafeRelease(s_DirectionalLightDatas);
                Utilities.SafeRelease(s_LightDatas);
                Utilities.SafeRelease(s_EnvLightDatas);
                Utilities.SafeRelease(s_shadowDatas);

                if (m_CubeReflTexArray != null)
                {
                    m_CubeReflTexArray.Release();
                    m_CubeReflTexArray = null;
                }
                if (m_CookieTexArray != null)
                {
                    m_CookieTexArray.Release();
                    m_CookieTexArray = null;
                }
                if (m_CubeCookieTexArray != null)
                {
                    m_CubeCookieTexArray.Release();
                    m_CubeCookieTexArray = null;
                }

                ReleaseResolutionDependentBuffers();

                Utilities.SafeRelease(s_AABBBoundsBuffer);
                Utilities.SafeRelease(s_ConvexBoundsBuffer);
                Utilities.SafeRelease(s_LightVolumeDataBuffer);

                // enableClustered
                Utilities.SafeRelease(s_GlobalLightListAtomic);

                Utilities.Destroy(m_DeferredDirectMaterial);
                Utilities.Destroy(m_DeferredIndirectMaterial);
                Utilities.Destroy(m_DeferredAllMaterial);
                Utilities.Destroy(m_DebugViewTilesMaterial);

                Utilities.Destroy(m_SingleDeferredMaterial);
            }

            public override void NewFrame()
            {
                m_CookieTexArray.NewFrame();
                m_CubeCookieTexArray.NewFrame();
                m_CubeReflTexArray.NewFrame();
            }

            public override bool NeedResize()
            {
                return s_LightList == null ||
                        (s_BigTileLightList == null && enableBigTilePrepass) ||
                        (s_PerVoxelLightLists == null && enableClustered);
            }

            public override void ReleaseResolutionDependentBuffers()
            {
                Utilities.SafeRelease(s_LightList);

                // enableClustered
                Utilities.SafeRelease(s_PerVoxelLightLists);
                Utilities.SafeRelease(s_PerVoxelOffset);
                Utilities.SafeRelease(s_PerTileLogBaseTweak);

                // enableBigTilePrepass
                Utilities.SafeRelease(s_BigTileLightList);
            }

            int NumLightIndicesPerClusteredTile()
            {
                return 8 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
            }

            public override void AllocResolutionDependentBuffers(int width, int height)
            {
                var nrTilesX = (width + k_TileSize - 1) / k_TileSize;
                var nrTilesY = (height + k_TileSize - 1) / k_TileSize;
                var nrTiles = nrTilesX * nrTilesY;
                const int capacityUShortsPerTile = 32;
                const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

                s_LightList = new ComputeBuffer((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display

                if (enableClustered)
                {
                    s_PerVoxelOffset = new ComputeBuffer((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrTiles, sizeof(uint));
                    s_PerVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrTiles, sizeof(uint));

                    if (k_UseDepthBuffer)
                    {
                        s_PerTileLogBaseTweak = new ComputeBuffer(nrTiles, sizeof(float));
                    }
                }

                if (enableBigTilePrepass)
                {
                    var nrBigTilesX = (width + 63) / 64;
                    var nrBigTilesY = (height + 63) / 64;
                    var nrBigTiles = nrBigTilesX * nrBigTilesY;
                    s_BigTileLightList = new ComputeBuffer(LightDefinitions.MAX_NR_BIGTILE_LIGHTS_PLUSONE * nrBigTiles, sizeof(uint));
                }
            }

            static Matrix4x4 GetFlipMatrix()
            {
                Matrix4x4 flip = Matrix4x4.identity;
                bool isLeftHand = ((int)LightDefinitions.USE_LEFTHAND_CAMERASPACE) != 0;
                if (isLeftHand) flip.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
                return flip;
            }

            static Matrix4x4 WorldToCamera(Camera camera)
            {
                return GetFlipMatrix() * camera.worldToCameraMatrix;
            }

            static Matrix4x4 CameraProjection(Camera camera)
            {
                return camera.projectionMatrix * GetFlipMatrix();
            }

            public Vector3 GetLightColor(VisibleLight light)
            {
                // Linear intensity calculation (different from legacy Unity - match LinearLighting new option in 5.6)
                var lightColorR = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.r);
                var lightColorG = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.g);
                var lightColorB = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.b);

                return new Vector3(lightColorR, lightColorG, lightColorB);
            }

            // Return number of added shadow
            public int GetShadows(VisibleLight light, int lightIndex, ref ShadowOutput shadowOutput)
            {
                for (int sliceIndex = 0; sliceIndex < shadowOutput.GetShadowSliceCountLightIndex(lightIndex); ++sliceIndex)
                {
                    ShadowData shadowData = new ShadowData();

                    int shadowSliceIndex = shadowOutput.GetShadowSliceIndex(lightIndex, sliceIndex);
                    shadowData.worldToShadow = shadowOutput.shadowSlices[shadowSliceIndex].shadowTransform.transpose; // Transpose for hlsl reading ?

                    shadowData.bias = light.light.shadowBias;

                    m_lightList.shadows.Add(shadowData);
                }

                return shadowOutput.GetShadowSliceCountLightIndex(lightIndex);
            }

            public void GetDirectionalLightData(GPULightType gpuLightType, VisibleLight light, AdditionalLightData additionalData, int lightIndex, ref ShadowOutput shadowOutput, ref int directionalShadowcount)
            {
                var directionalLightData = new DirectionalLightData();

                // Light direction for directional is opposite to the forward direction
                directionalLightData.forward = light.light.transform.forward;
                directionalLightData.up = light.light.transform.up;
                directionalLightData.right = light.light.transform.right;
                directionalLightData.positionWS = light.light.transform.position;
                directionalLightData.color = GetLightColor(light);
                directionalLightData.diffuseScale = additionalData.affectDiffuse ? 1.0f : 0.0f;
                directionalLightData.specularScale = additionalData.affectSpecular ? 1.0f : 0.0f;
                directionalLightData.invScaleX = 1.0f / light.light.transform.localScale.x;
                directionalLightData.invScaleY = 1.0f / light.light.transform.localScale.y;
                directionalLightData.cosAngle = 0.0f;
                directionalLightData.sinAngle = 0.0f;
                directionalLightData.shadowIndex = -1;
                directionalLightData.cookieIndex = -1;

                if (light.light.cookie != null)
                {
                    directionalLightData.tileCookie = (light.light.cookie.wrapMode == TextureWrapMode.Repeat);
                    directionalLightData.cookieIndex = m_CookieTexArray.FetchSlice(light.light.cookie);
                }

                bool hasDirectionalShadows = light.light.shadows != LightShadows.None && shadowOutput.GetShadowSliceCountLightIndex(lightIndex) != 0;
                bool hasDirectionalNotReachMaxLimit = directionalShadowcount == 0; // Only one cascade shadow allowed

                if (hasDirectionalShadows && hasDirectionalNotReachMaxLimit) // Note  < MaxShadows should be check at shadowOutput creation
                {
                    directionalLightData.shadowIndex = m_lightList.shadows.Count;
                    directionalShadowcount += GetShadows(light, lightIndex, ref shadowOutput);

                    // Fill split information for shaders
                    for (int s = 0; s < k_MaxCascadeCount; ++s)
                    {
                        m_lightList.directionalShadowSplitSphereSqr[s] = shadowOutput.directionalShadowSplitSphereSqr[s];
                    }
                }

                m_lightList.directionalLights.Add(directionalLightData);
            }

            public void GetLightData(GPULightType gpuLightType, VisibleLight light, AdditionalLightData additionalData, int lightIndex, ref ShadowOutput shadowOutput, ref int shadowCount)
            {
                var lightData = new LightData();

                lightData.lightType = gpuLightType;

                lightData.positionWS = light.light.transform.position;
                lightData.invSqrAttenuationRadius = 1.0f / (light.range * light.range);
                lightData.color = GetLightColor(light);

                lightData.forward = light.light.transform.forward; // Note: Light direction is oriented backward (-Z)
                lightData.up = light.light.transform.up;
                lightData.right = light.light.transform.right;

                if (lightData.lightType == GPULightType.Spot)
                {
                    var spotAngle = light.spotAngle;

                    var innerConePercent = additionalData.GetInnerSpotPercent01();
                    var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                    var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                    var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                    var val = Mathf.Max(0.001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                    lightData.angleScale = 1.0f / val;
                    lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;

                    // TODO: Currently the spot cookie code use the cotangent, either we fix the spot cookie code to not use cotangent
                    // or we clean the name here, store it in size.x for now
                    lightData.size.x = cosSpotOuterHalfAngle / sinSpotOuterHalfAngle;
                }
                else
                {
                    // 1.0f, 2.0f are neutral value allowing GetAngleAnttenuation in shader code to return 1.0
                    lightData.angleScale = 1.0f;
                    lightData.angleOffset = 2.0f;
                }

                lightData.diffuseScale = additionalData.affectDiffuse ? 1.0f : 0.0f;
                lightData.specularScale = additionalData.affectSpecular ? 1.0f : 0.0f;
                lightData.shadowDimmer = additionalData.shadowDimmer;

                lightData.IESIndex = -1;
                lightData.cookieIndex = -1;
                lightData.shadowIndex = -1;

                if (light.light.cookie != null)
                {
                    // TODO: add texture atlas support for cookie textures.
                    switch (light.lightType)
                    {
                        case LightType.Spot:
                            lightData.cookieIndex = m_CookieTexArray.FetchSlice(light.light.cookie);
                            break;
                        case LightType.Point:
                            lightData.cookieIndex = m_CubeCookieTexArray.FetchSlice(light.light.cookie);
                            break;
                    }
                }

                // Setup shadow data arrays
                bool hasShadows = light.light.shadows != LightShadows.None && shadowOutput.GetShadowSliceCountLightIndex(lightIndex) != 0;
                bool hasNotReachMaxLimit = shadowCount + (lightData.lightType == GPULightType.Point ? 6 : 1) <= k_MaxShadowOnScreen;

                // TODO: Read the comment about shadow limit/management at the beginning of this loop
                if (hasShadows && hasNotReachMaxLimit)
                {
                    // When we have a point light, we assumed that there is 6 consecutive PunctualShadowData
                    lightData.shadowIndex = m_lightList.shadows.Count;
                    shadowCount += GetShadows(light, lightIndex, ref shadowOutput);
                }

                if (additionalData.archetype != LightArchetype.Punctual)
                {
                    lightData.twoSided = additionalData.isDoubleSided;
                    lightData.size = new Vector2(additionalData.areaLightLength, additionalData.areaLightWidth);
                }

                m_lightList.lights.Add(lightData);
            }

            // TODO: we should be able to do this calculation only with LightData without VisibleLight light, but for now pass both
            public void GetLightVolumeDataAndBound(LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType, VisibleLight light, LightData lightData, Matrix4x4 worldToView)
            {
                // Then Culling side
                var range = light.range;
                var lightToWorld = light.localToWorld;
                Vector3 lightPos = lightToWorld.GetColumn(3);

                // Fill bounds
                var bound = new SFiniteLightBound();
                var ligthVolumeData = new LightVolumeData();

                ligthVolumeData.lightCategory = (uint)lightCategory;
                ligthVolumeData.lightVolume = (uint)lightVolumeType;

                if (gpuLightType == GPULightType.Spot)
                {
                    Vector3 lightDir = lightToWorld.GetColumn(2);   // Z axis in world space

                    // represents a left hand coordinate system in world space
                    Vector3 vx = lightToWorld.GetColumn(0);     // X axis in world space
                    Vector3 vy = lightToWorld.GetColumn(1);     // Y axis in world space
                    var vz = lightDir;                      // Z axis in world space

                    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    vx = worldToView.MultiplyVector(vx);
                    vy = worldToView.MultiplyVector(vy);
                    vz = worldToView.MultiplyVector(vz);

                    const float pi = 3.1415926535897932384626433832795f;
                    const float degToRad = (float)(pi / 180.0);

                    var sa = light.light.spotAngle;

                    var cs = Mathf.Cos(0.5f * sa * degToRad);
                    var si = Mathf.Sin(0.5f * sa * degToRad);

                    const float FltMax = 3.402823466e+38F;
                    var ta = cs > 0.0f ? (si / cs) : FltMax;
                    var cota = si > 0.0f ? (cs / si) : FltMax;

                    //const float cotasa = l.GetCotanHalfSpotAngle();

                    // apply nonuniform scale to OBB of spot light
                    var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                    var fS = squeeze ? ta : si;
                    bound.center = worldToView.MultiplyPoint(lightPos + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                    // scale axis to match box or base of pyramid
                    bound.boxAxisX = (fS * range) * vx;
                    bound.boxAxisY = (fS * range) * vy;
                    bound.boxAxisZ = (0.5f * range) * vz;

                    // generate bounding sphere radius
                    var fAltDx = si;
                    var fAltDy = cs;
                    fAltDy = fAltDy - 0.5f;
                    //if(fAltDy<0) fAltDy=-fAltDy;

                    fAltDx *= range; fAltDy *= range;

                    // Handle case of pyramid with this select
                    var altDist = Mathf.Sqrt(fAltDy * fAltDy + (gpuLightType == GPULightType.Spot ? 1.0f : 2.0f) * fAltDx * fAltDx);
                    bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                    bound.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                    ligthVolumeData.lightAxisX = vx;
                    ligthVolumeData.lightAxisY = vy;
                    ligthVolumeData.lightAxisZ = vz;
                    ligthVolumeData.lightPos = worldToView.MultiplyPoint(lightPos);
                    ligthVolumeData.radiusSq = range * range;
                    ligthVolumeData.cotan = cota;
                }
                else if (gpuLightType == GPULightType.Point)
                {
                    bool isNegDeterminant = Vector3.Dot(worldToView.GetColumn(0), Vector3.Cross(worldToView.GetColumn(1), worldToView.GetColumn(2))) < 0.0f; // 3x3 Determinant.

                    bound.center = worldToView.MultiplyPoint(lightPos);
                    bound.boxAxisX.Set(range, 0, 0);
                    bound.boxAxisY.Set(0, range, 0);
                    bound.boxAxisZ.Set(0, 0, isNegDeterminant ? (-range) : range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = range;

                    // represents a left hand coordinate system in world space since det(worldToView)<0
                    var lightToView = worldToView * lightToWorld;
                    Vector3 vx = lightToView.GetColumn(0);
                    Vector3 vy = lightToView.GetColumn(1);
                    Vector3 vz = lightToView.GetColumn(2);

                    // fill up ldata
                    ligthVolumeData.lightAxisX = vx;
                    ligthVolumeData.lightAxisY = vy;
                    ligthVolumeData.lightAxisZ = vz;
                    ligthVolumeData.lightPos = bound.center;
                    ligthVolumeData.radiusSq = range * range;
                }
                else if (gpuLightType == GPULightType.Rectangle)
                {
                    Vector3 centerVS = worldToView.MultiplyPoint(lightData.positionWS);
                    Vector3 xAxisVS = worldToView.MultiplyVector(lightData.right);
                    Vector3 yAxisVS = worldToView.MultiplyVector(lightData.up);
                    Vector3 zAxisVS = worldToView.MultiplyVector(lightData.forward);
                    float radius = 1.0f / Mathf.Sqrt(lightData.invSqrAttenuationRadius);

                    Vector3 dimensions = new Vector3(lightData.size.x * 0.5f + radius, lightData.size.y * 0.5f + radius, radius);

                    if (!lightData.twoSided)
                    {
                        centerVS -= zAxisVS * radius * 0.5f;
                        dimensions.z *= 0.5f;
                    }

                    bound.center = centerVS;
                    bound.boxAxisX = dimensions.x * xAxisVS;
                    bound.boxAxisY = dimensions.y * yAxisVS;
                    bound.boxAxisZ = dimensions.z * zAxisVS;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = dimensions.magnitude;

                    ligthVolumeData.lightPos = centerVS;
                    ligthVolumeData.lightAxisX = xAxisVS;
                    ligthVolumeData.lightAxisY = yAxisVS;
                    ligthVolumeData.lightAxisZ = zAxisVS;
                    ligthVolumeData.boxInnerDist = dimensions;
                    ligthVolumeData.boxInvRange.Set(1e5f, 1e5f, 1e5f);
                }
                else if (gpuLightType == GPULightType.Line)
                {
                    Vector3 centerVS = worldToView.MultiplyPoint(lightData.positionWS);
                    Vector3 xAxisVS = worldToView.MultiplyVector(lightData.right);
                    Vector3 yAxisVS = worldToView.MultiplyVector(lightData.up);
                    Vector3 zAxisVS = worldToView.MultiplyVector(lightData.forward);
                    float radius = 1.0f / Mathf.Sqrt(lightData.invSqrAttenuationRadius);

                    Vector3 dimensions = new Vector3(lightData.size.x * 0.5f + radius, radius, radius);

                    bound.center = centerVS;
                    bound.boxAxisX = dimensions.x * xAxisVS;
                    bound.boxAxisY = dimensions.y * yAxisVS;
                    bound.boxAxisZ = dimensions.z * zAxisVS;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = dimensions.magnitude;

                    ligthVolumeData.lightPos = centerVS;
                    ligthVolumeData.lightAxisX = xAxisVS;
                    ligthVolumeData.lightAxisY = yAxisVS;
                    ligthVolumeData.lightAxisZ = zAxisVS;
                    ligthVolumeData.boxInnerDist = new Vector3(lightData.size.x * 0.5f, 0.01f, 0.01f);
                    ligthVolumeData.boxInvRange.Set(1.0f / radius, 1.0f / radius, 1.0f / radius);
                }
                else
                {
                    // TODO implement unsupported type
                    Debug.Assert(false);
                }

                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(ligthVolumeData);
            }

            public void GetEnvLightData(VisibleReflectionProbe probe)
            {
                var envLightData = new EnvLightData();

                // CAUTION: localToWorld is the transform for the widget of the reflection probe. i.e the world position of the point use to do the cubemap capture (mean it include the local offset)
                envLightData.positionWS = probe.localToWorld.GetColumn(3);

                envLightData.envShapeType = EnvShapeType.None;

                // TODO: Support sphere influence in UI
                if (probe.boxProjection != 0)
                {
                    envLightData.envShapeType = EnvShapeType.Box;
                }

                // remove scale from the matrix (Scale in this matrix is use to scale the widget)
                envLightData.right = probe.localToWorld.GetColumn(0);
                envLightData.right.Normalize();
                envLightData.up = probe.localToWorld.GetColumn(1);
                envLightData.up.Normalize();
                envLightData.forward = probe.localToWorld.GetColumn(2);
                envLightData.forward.Normalize();

                // Artists prefer to have blend distance inside the volume!
                // So we let the current UI but we assume blendDistance is an inside factor instead
                // Blend distance can't be larger than the max radius
                // probe.bounds.extents is BoxSize / 2
                float maxBlendDist = Mathf.Min(probe.bounds.extents.x, Mathf.Min(probe.bounds.extents.y, probe.bounds.extents.z));
                float blendDistance = Mathf.Min(maxBlendDist, probe.blendDistance);
                envLightData.innerDistance = probe.bounds.extents - new Vector3(blendDistance, blendDistance, blendDistance);

                envLightData.envIndex = m_CubeReflTexArray.FetchSlice(probe.texture);

                envLightData.offsetLS = probe.center; // center is misnamed, it is the offset (in local space) from center of the bounding box to the cubemap capture point
                envLightData.blendDistance = blendDistance;

                m_lightList.envLights.Add(envLightData);
            }

            public void GetEnvLightVolumeDataAndBound(VisibleReflectionProbe probe, LightVolumeType lightVolumeType, Matrix4x4 worldToView)
            {
                var bound = new SFiniteLightBound();
                var ligthVolumeData = new LightVolumeData();

                var bnds = probe.bounds;
                var boxOffset = probe.center;                  // reflection volume offset relative to cube map capture point
                var blendDistance = probe.blendDistance;

                var mat = probe.localToWorld;

                // C is reflection volume center in world space (NOT same as cube map capture point)
                var e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
                //Vector3 C = bnds.center;        // P + boxOffset;
                var C = mat.MultiplyPoint(boxOffset);       // same as commented out line above when rot is identity

                var combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

                Vector3 vx = mat.GetColumn(0);
                Vector3 vy = mat.GetColumn(1);
                Vector3 vz = mat.GetColumn(2);

                // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                vx = worldToView.MultiplyVector(vx);
                vy = worldToView.MultiplyVector(vy);
                vz = worldToView.MultiplyVector(vz);

                var Cw = worldToView.MultiplyPoint(C);

                bound.center = Cw;
                bound.boxAxisX = combinedExtent.x * vx;
                bound.boxAxisY = combinedExtent.y * vy;
                bound.boxAxisZ = combinedExtent.z * vz;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = combinedExtent.magnitude;


                ligthVolumeData.lightCategory = (uint)LightCategory.Env;
                ligthVolumeData.lightVolume = (uint)lightVolumeType;

                ligthVolumeData.lightPos = Cw;
                ligthVolumeData.lightAxisX = vx;
                ligthVolumeData.lightAxisY = vy;
                ligthVolumeData.lightAxisZ = vz;
                var delta = combinedExtent - e;
                ligthVolumeData.boxInnerDist = e;
                ligthVolumeData.boxInvRange.Set(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(ligthVolumeData);
            }

            public override void PrepareLightsForGPU(CullResults cullResults, Camera camera, ref ShadowOutput shadowOutput)
            {
                m_lightList.Clear();

                if (cullResults.visibleLights.Length == 0)
                    return;

                // 1. Count the number of lights and sort all light by category, type and volume
                int directionalLightcount = 0;
                int punctualLightcount = 0;
                int areaLightCount = 0;

                var sortKeys = new uint[Math.Min(cullResults.visibleLights.Length, k_MaxLightsOnSCreen)];
                int sortCount = 0;

                for (int lightIndex = 0, numLights = cullResults.visibleLights.Length; lightIndex < numLights; ++lightIndex)
                {
                    var light = cullResults.visibleLights[lightIndex];

                    // We only process light with additional data
                    var additionalData = light.light.GetComponent<AdditionalLightData>();

                    if (additionalData == null)
                    {
                        Debug.LogWarning("Light entity detected without additional data, will not be taken into account " + light.light.name);
                        continue;
                    }

                    LightCategory lightCategory = LightCategory.Count;
                    GPULightType gpuLightType = GPULightType.Point;
                    LightVolumeType lightVolumeType = LightVolumeType.Count;

                    // Note: LightType.Area is offline only, use for baking, no need to test it
                    if (additionalData.archetype == LightArchetype.Punctual)
                    {
                        switch (light.lightType)
                        {
                            case LightType.Point:
                                if (punctualLightcount >= k_MaxPunctualLightsOnSCreen)
                                    continue;
                                lightCategory = LightCategory.Punctual;
                                gpuLightType = GPULightType.Point;
                                lightVolumeType = LightVolumeType.Sphere;
                                ++punctualLightcount;
                                break;

                            case LightType.Spot:
                                if (punctualLightcount >= k_MaxPunctualLightsOnSCreen)
                                    continue;
                                lightCategory = LightCategory.Punctual;
                                gpuLightType = GPULightType.Spot;
                                lightVolumeType = LightVolumeType.Cone;
                                ++punctualLightcount;
                                break;

                            case LightType.Directional:
                                if (directionalLightcount >= k_MaxDirectionalLightsOnSCreen)
                                    continue;
                                lightCategory = LightCategory.Punctual;
                                gpuLightType = GPULightType.Directional;
                                // No need to add volume, always visible
                                lightVolumeType = LightVolumeType.Count; // Count is none
                                ++directionalLightcount;
                                break;

                            default:
                                continue;
                        }
                    }
                    else
                    {
                        switch (additionalData.archetype)
                        {
                            case LightArchetype.Rectangle:
                                if (areaLightCount >= k_MaxAreaLightsOnSCreen)
                                    continue;
                                lightCategory = LightCategory.Area;
                                gpuLightType = GPULightType.Rectangle;
                                lightVolumeType = LightVolumeType.Box;
                                ++areaLightCount;
                                break;

                            case LightArchetype.Line:
                                if (areaLightCount >= k_MaxAreaLightsOnSCreen)
                                    continue;
                                lightCategory = LightCategory.Area;
                                gpuLightType = GPULightType.Line;
                                lightVolumeType = LightVolumeType.Box;
                                ++areaLightCount;
                                break;

                            default:
                                continue;
                        }
                    }

                    // 5 bit (0x1F) light category, 5 bit (0x1F) GPULightType, 6 bit (0x3F) lightVolume, 16 bit index
                    sortKeys[sortCount++] = (uint)lightCategory << 27 | (uint)gpuLightType << 22 | (uint)lightVolumeType << 16 | (uint)lightIndex;
                }

                Array.Sort(sortKeys);

                // TODO: Refactor shadow management
                // The good way of managing shadow:
                // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
                // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
                // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
                // will be use...)
                // The lightLoop is in charge, not the shadow pass.
                // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet
                int directionalShadowcount = 0;
                int shadowCount = 0;

                // 2. Go thought all lights, convert them to GPU format.
                // Create simultaneously data for culling (LigthVolumeData and rendering)
                var worldToView = WorldToCamera(camera);

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the light, we need to use this sorted order here
                    uint sortKey = sortKeys[sortIndex];
                    LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                    GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                    LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 16) & 0x3F);
                    int lightIndex = (int)(sortKey & 0xFFFF);

                    var light = cullResults.visibleLights[lightIndex];
                    var additionalData = light.light.GetComponent<AdditionalLightData>();

                    // Directional rendering side, it is separated as it is always visible so no volume to handle here
                    if (gpuLightType == GPULightType.Directional)
                    {
                        GetDirectionalLightData(gpuLightType, light, additionalData, lightIndex, ref shadowOutput, ref directionalShadowcount);

                        continue;
                    }

                    // Spot, point, rect, line light - Rendering side
                    GetLightData(gpuLightType, light, additionalData, lightIndex, ref shadowOutput, ref shadowCount);
                    // Then culling side. Must be call in this order as we pass the created Light data to the function
                    GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], worldToView);
                }

                // Sanity check
                Debug.Assert(m_lightList.directionalLights.Count == directionalLightcount);
                Debug.Assert(m_lightList.lights.Count == areaLightCount + punctualLightcount);
                m_areaLightCount = areaLightCount;
                m_punctualLightCount = punctualLightcount;

                // Redo everything but this time with envLights
                int envLightCount = 0;

                sortKeys = new uint[Math.Min(cullResults.visibleReflectionProbes.Length, k_MaxEnvLightsOnSCreen)];
                sortCount = 0;

                for (int probeIndex = 0, numProbes = cullResults.visibleReflectionProbes.Length; probeIndex < numProbes; probeIndex++)
                {
                    var probe = cullResults.visibleReflectionProbes[probeIndex];

                    if (envLightCount >= k_MaxEnvLightsOnSCreen)
                        continue;

                    // TODO: Support LightVolumeType.Sphere, currently in UI there is no way to specify a sphere influence volume                    
                    LightVolumeType lightVolumeType = probe.boxProjection != 0 ? LightVolumeType.Box : LightVolumeType.Box;
                    ++envLightCount;

                    // 16 bit lightVolume, 16 bit index
                    sortKeys[sortCount++] = (uint)lightVolumeType << 16 | (uint)probeIndex;
                }

                // Not necessary yet but call it for future modification with sphere influence volume
                Array.Sort(sortKeys);

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the light, we need to use this sorted order here
                    uint sortKey = sortKeys[sortIndex];
                    LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 16) & 0xFFFF);
                    int probeIndex = (int)(sortKey & 0xFFFF);

                    VisibleReflectionProbe probe = cullResults.visibleReflectionProbes[probeIndex];

                    GetEnvLightData(probe);

                    GetEnvLightVolumeDataAndBound(probe, lightVolumeType, worldToView);
                }

                // Sanity check
                Debug.Assert(m_lightList.envLights.Count == envLightCount);

                m_lightCount = m_lightList.lights.Count + m_lightList.envLights.Count;
                Debug.Assert(m_lightList.bounds.Count == m_lightCount);
                Debug.Assert(m_lightList.lightVolumes.Count == m_lightCount);
            }

            void VoxelLightListGeneration(CommandBuffer cmd, Camera camera, Matrix4x4 projscr, Matrix4x4 invProjscr, RenderTargetIdentifier cameraDepthBufferRT)
            {
                // clear atomic offset index
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, "g_LayeredSingleIdxBuffer", s_GlobalLightListAtomic);
                cmd.DispatchCompute(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

                cmd.SetComputeIntParam(buildPerVoxelLightListShader, "_EnvLightIndexShift", m_lightList.lights.Count);    
                cmd.SetComputeIntParam(buildPerVoxelLightListShader, "g_iNrVisibLights", m_lightCount);
                Utilities.SetMatrixCS(cmd, buildPerVoxelLightListShader, "g_mScrProjection", projscr);
                Utilities.SetMatrixCS(cmd, buildPerVoxelLightListShader, "g_mInvScrProjection", invProjscr);

                cmd.SetComputeIntParam(buildPerVoxelLightListShader, "g_iLog2NumClusters", k_Log2NumClusters);

                //Vector4 v2_near = invProjscr * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                //Vector4 v2_far = invProjscr * new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
                //float nearPlane2 = -(v2_near.z/v2_near.w);
                //float farPlane2 = -(v2_far.z/v2_far.w);
                var nearPlane = camera.nearClipPlane;
                var farPlane = camera.farClipPlane;
                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fNearPlane", nearPlane);
                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fFarPlane", farPlane);

                const float C = (float)(1 << k_Log2NumClusters);
                var geomSeries = (1.0 - Mathf.Pow(k_ClustLogBase, C)) / (1 - k_ClustLogBase);        // geometric series: sum_k=0^{C-1} base^k
                m_ClustScale = (float)(geomSeries / (farPlane - nearPlane));

                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fClustScale", m_ClustScale);
                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fClustBase", k_ClustLogBase);

                cmd.SetComputeTextureParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_depth_tex", cameraDepthBufferRT);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vLayeredLightList", s_PerVoxelLightLists);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_LayeredOffset", s_PerVoxelOffset);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_LayeredSingleIdxBuffer", s_GlobalLightListAtomic);
                if (enableBigTilePrepass)
                    cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vBigTileLightList", s_BigTileLightList);

                if (k_UseDepthBuffer)
                {
                    cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_logBaseBuffer", s_PerTileLogBaseTweak);
                }

                var numTilesX = GetNumTileX(camera);
                var numTilesY = GetNumTileY(camera);
                cmd.DispatchCompute(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, numTilesX, numTilesY, 1);
            }

            public override void BuildGPULightLists(Camera camera, RenderLoop loop, RenderTargetIdentifier cameraDepthBufferRT)
            {
                var w = camera.pixelWidth;
                var h = camera.pixelHeight;
                var numTilesX = GetNumTileX(camera);
                var numTilesY = GetNumTileY(camera);
                var numBigTilesX = (w + 63) / 64;
                var numBigTilesY = (h + 63) / 64;

                // camera to screen matrix (and it's inverse)
                var proj = CameraProjection(camera);
                var temp = new Matrix4x4();
                temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
                temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                var projscr = temp * proj;
                var invProjscr = projscr.inverse;

                var cmd = new CommandBuffer() { name = "" };

                // generate screen-space AABBs (used for both fptl and clustered).
                {
                    temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                    temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                    temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                    temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    var projh = temp * proj;
                    var invProjh = projh.inverse;

                    cmd.SetComputeIntParam(buildScreenAABBShader, "g_iNrVisibLights", m_lightCount);
                    Utilities.SetMatrixCS(cmd, buildScreenAABBShader, "g_mProjection", projh);
                    Utilities.SetMatrixCS(cmd, buildScreenAABBShader, "g_mInvProjection", invProjh);
                    cmd.SetComputeBufferParam(buildScreenAABBShader, s_GenAABBKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                    cmd.DispatchCompute(buildScreenAABBShader, s_GenAABBKernel, (m_lightCount + 7) / 8, 1, 1);
                }

                // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
                if (enableBigTilePrepass)
                {
                    cmd.SetComputeIntParams(buildPerBigTileLightListShader, "g_viDimensions", new int[2] { w, h });
                    cmd.SetComputeIntParam(buildPerBigTileLightListShader, "_EnvLightIndexShift", m_lightList.lights.Count);
                    cmd.SetComputeIntParam(buildPerBigTileLightListShader, "g_iNrVisibLights", m_lightCount);
                    Utilities.SetMatrixCS(cmd, buildPerBigTileLightListShader, "g_mScrProjection", projscr);
                    Utilities.SetMatrixCS(cmd, buildPerBigTileLightListShader, "g_mInvScrProjection", invProjscr);
                    cmd.SetComputeFloatParam(buildPerBigTileLightListShader, "g_fNearPlane", camera.nearClipPlane);
                    cmd.SetComputeFloatParam(buildPerBigTileLightListShader, "g_fFarPlane", camera.farClipPlane);
                    cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, "g_vLightList", s_BigTileLightList);
                    cmd.DispatchCompute(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, numBigTilesX, numBigTilesY, 1);
                }

                if (usingFptl)       // optimized for opaques only
                {
                    cmd.SetComputeIntParams(buildPerTileLightListShader, "g_viDimensions", new int[2] { w, h });
                    cmd.SetComputeIntParam(buildPerTileLightListShader, "_EnvLightIndexShift", m_lightList.lights.Count);
                    cmd.SetComputeIntParam(buildPerTileLightListShader, "g_iNrVisibLights", m_lightCount);
                    Utilities.SetMatrixCS(cmd, buildPerTileLightListShader, "g_mScrProjection", projscr);
                    Utilities.SetMatrixCS(cmd, buildPerTileLightListShader, "g_mInvScrProjection", invProjscr);
                    cmd.SetComputeTextureParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_depth_tex", cameraDepthBufferRT);
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vLightList", s_LightList);
                    if (enableBigTilePrepass)
                        cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vBigTileLightList", s_BigTileLightList);
                    cmd.DispatchCompute(buildPerTileLightListShader, s_GenListPerTileKernel, numTilesX, numTilesY, 1);
                }

                if (enableClustered)        // works for transparencies too.
                {
                    VoxelLightListGeneration(cmd, camera, projscr, invProjscr, cameraDepthBufferRT);
                }

                loop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            public override void PushGlobalParams(Camera camera, RenderLoop loop)
            {
                Shader.SetGlobalTexture("_CookieTextures", m_CookieTexArray.GetTexCache());
                Shader.SetGlobalTexture("_CookieCubeTextures", m_CubeCookieTexArray.GetTexCache());
                Shader.SetGlobalTexture("_EnvTextures", m_CubeReflTexArray.GetTexCache());

                s_DirectionalLightDatas.SetData(m_lightList.directionalLights.ToArray());
                s_LightDatas.SetData(m_lightList.lights.ToArray());
                s_EnvLightDatas.SetData(m_lightList.envLights.ToArray());
                s_shadowDatas.SetData(m_lightList.shadows.ToArray());

                // These two buffers have been set in Rebuild()
                s_ConvexBoundsBuffer.SetData(m_lightList.bounds.ToArray());
                s_LightVolumeDataBuffer.SetData(m_lightList.lightVolumes.ToArray());

                Shader.SetGlobalBuffer("_DirectionalLightDatas", s_DirectionalLightDatas);
                Shader.SetGlobalInt("_DirectionalLightCount", m_lightList.directionalLights.Count);
                Shader.SetGlobalBuffer("_LightDatas", s_LightDatas);
                Shader.SetGlobalInt("_PunctualLightCount", m_punctualLightCount);
                Shader.SetGlobalInt("_AreaLightCount", m_areaLightCount);
                Shader.SetGlobalBuffer("_EnvLightDatas", s_EnvLightDatas);
                Shader.SetGlobalInt("_EnvLightCount", m_lightList.envLights.Count);
                Shader.SetGlobalBuffer("_ShadowDatas", s_shadowDatas);
                Shader.SetGlobalVectorArray("_DirShadowSplitSpheres", m_lightList.directionalShadowSplitSphereSqr);

                var cmd = new CommandBuffer { name = "Push Global Parameters" };

                cmd.SetGlobalFloat("_NumTileX", (float)GetNumTileX(camera));
                cmd.SetGlobalFloat("_NumTileY", (float)GetNumTileY(camera));

                if (enableBigTilePrepass)
                    cmd.SetGlobalBuffer("g_vBigTileLightList", s_BigTileLightList);

                if (enableClustered)
                {
                    cmd.SetGlobalFloat("g_fClustScale", m_ClustScale);
                    cmd.SetGlobalFloat("g_fClustBase", k_ClustLogBase);
                    cmd.SetGlobalFloat("g_fNearPlane", camera.nearClipPlane);
                    cmd.SetGlobalFloat("g_fFarPlane", camera.farClipPlane);
                    cmd.SetGlobalFloat("g_iLog2NumClusters", k_Log2NumClusters);

                    cmd.SetGlobalFloat("g_isLogBaseBufferEnabled", k_UseDepthBuffer ? 1 : 0);

                    cmd.SetGlobalBuffer("g_vLayeredOffsetsBuffer", s_PerVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        cmd.SetGlobalBuffer("g_logBaseBuffer", s_PerTileLogBaseTweak);
                    }
                }

                loop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            public override void RenderDeferredLighting(Camera camera, RenderLoop renderLoop, RenderTargetIdentifier cameraColorBufferRT)
            {
                var bUseClusteredForDeferred = !usingFptl;

                var invViewProj = Utilities.GetViewProjectionMatrix(camera).inverse;
                var screenSize = Utilities.ComputeScreenSize(camera);

                m_DeferredDirectMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);
                m_DeferredDirectMaterial.SetVector("_ScreenSize", screenSize);
                m_DeferredDirectMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m_DeferredDirectMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                m_DeferredDirectMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                m_DeferredDirectMaterial.DisableKeyword(!bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");

                m_DeferredIndirectMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);
                m_DeferredIndirectMaterial.SetVector("_ScreenSize", screenSize);
                m_DeferredIndirectMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m_DeferredIndirectMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
                m_DeferredIndirectMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                m_DeferredIndirectMaterial.DisableKeyword(!bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");

                m_DeferredAllMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);
                m_DeferredAllMaterial.SetVector("_ScreenSize", screenSize);
                m_DeferredAllMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m_DeferredAllMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                m_DeferredAllMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                m_DeferredAllMaterial.DisableKeyword(!bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");

                m_DebugViewTilesMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);
                m_DebugViewTilesMaterial.SetVector("_ScreenSize", screenSize);
                m_DebugViewTilesMaterial.SetInt("_ViewTilesFlags", debugViewTilesFlags);
                m_DebugViewTilesMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                m_DebugViewTilesMaterial.DisableKeyword(!bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");

                m_SingleDeferredMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);
                m_SingleDeferredMaterial.SetVector("_ScreenSize", screenSize);
                m_SingleDeferredMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m_SingleDeferredMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

                using (new Utilities.ProfilingSample(disableTileAndCluster ? "SinglePass - Deferred Lighting Pass" : "TilePass - Deferred Lighting Pass", renderLoop))
                {

                    var cmd = new CommandBuffer();
                    cmd.SetGlobalBuffer("g_vLightListGlobal", bUseClusteredForDeferred ? s_PerVoxelLightLists : s_LightList);       // opaques list (unless MSAA possibly)

                    cmd.name = bUseClusteredForDeferred ? "Clustered pass" : "Tiled pass";

                    // In case of bUseClusteredForDeferred disable toggle option since we're using m_perVoxelLightLists as opposed to lightList
                    if (bUseClusteredForDeferred)
                    {
                        cmd.SetGlobalFloat("_UseTileLightList", 0);
                    }

                    /*
                    if (enableComputeLightEvaluation)  //TODO: temporary workaround for "All kernels must use same constant buffer layouts"
                    {
                        var w = camera.pixelWidth;
                        var h = camera.pixelHeight;
                        var numTilesX = (w + 7) / 8;
                        var numTilesY = (h + 7) / 8;

                        string kernelName = "ShadeDeferred" + (bUseClusteredForDeferred ? "_Clustered" : "_Fptl") + (enableDrawTileDebug ? "_Debug" : "");
                        int kernel = deferredComputeShader.FindKernel(kernelName);

                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraDepthTexture", new RenderTargetIdentifier(s_CameraDepthTexture));
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture0", new RenderTargetIdentifier(s_GBufferAlbedo));
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture1", new RenderTargetIdentifier(s_GBufferSpecRough));
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture2", new RenderTargetIdentifier(s_GBufferNormal));
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture3", new RenderTargetIdentifier(s_GBufferEmission));
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_spotCookieTextures", m_CookieTexArray.GetTexCache());
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_pointCookieTextures", m_CubeCookieTexArray.GetTexCache());
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_reflCubeTextures", m_CubeReflTexArray.GetTexCache());
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_reflRootCubeTexture", ReflectionProbe.GetDefaultTexture());
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "g_tShadowBuffer", new RenderTargetIdentifier(m_shadowBufferID));
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "unity_NHxRoughness", m_NHxRoughnessTexture);
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_LightTextureB0", m_LightAttentuationTexture);

                        cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_vLightListGlobal", bUseClusteredForDeferred ? s_PerVoxelLightLists : s_LightList);
                        cmd.SetComputeBufferParam(deferredComputeShader, kernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                        cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_dirLightData", s_DirLightList);

                        var defdecode = ReflectionProbe.GetDefaultTextureHDRDecodeValues();
                        cmd.SetComputeFloatParam(deferredComputeShader, "_reflRootHdrDecodeMult", defdecode.x);
                        cmd.SetComputeFloatParam(deferredComputeShader, "_reflRootHdrDecodeExp", defdecode.y);

                        cmd.SetComputeFloatParam(deferredComputeShader, "g_fClustScale", m_ClustScale);
                        cmd.SetComputeFloatParam(deferredComputeShader, "g_fClustBase", k_ClustLogBase);
                        cmd.SetComputeFloatParam(deferredComputeShader, "g_fNearPlane", camera.nearClipPlane);
                        cmd.SetComputeFloatParam(deferredComputeShader, "g_fFarPlane", camera.farClipPlane);
                        cmd.SetComputeIntParam(deferredComputeShader, "g_iLog2NumClusters", k_Log2NumClusters);
                        cmd.SetComputeIntParam(deferredComputeShader, "g_isLogBaseBufferEnabled", k_UseDepthBuffer ? 1 : 0);
                        cmd.SetComputeIntParam(deferredComputeShader, "_UseTileLightList", 0);


                        //
                        var proj = camera.projectionMatrix;
                        var temp = new Matrix4x4();
                        temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                        temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                        temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                        temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                        var projh = temp * proj;
                        var invProjh = projh.inverse;

                        temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
                        temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
                        temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                        temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                        var projscr = temp * proj;
                        var invProjscr = projscr.inverse;

                        cmd.SetComputeIntParam(deferredComputeShader, "_EnvLightIndexShift", m_lightList.lights.Count);
                        cmd.SetComputeIntParam(deferredComputeShader, "g_iNrVisibLights", numLights);
                        SetMatrixCS(cmd, deferredComputeShader, "g_mScrProjection", projscr);
                        SetMatrixCS(cmd, deferredComputeShader, "g_mInvScrProjection", invProjscr);
                        SetMatrixCS(cmd, deferredComputeShader, "g_mViewToWorld", camera.cameraToWorldMatrix);


                        if (bUseClusteredForDeferred)
                        {
                            cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_vLayeredOffsetsBuffer", s_PerVoxelOffset);
                            if (k_UseDepthBuffer)
                            {
                                cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_logBaseBuffer", s_PerTileLogBaseTweak);
                            }
                        }

                        cmd.SetComputeIntParam(deferredComputeShader, "g_widthRT", w);
                        cmd.SetComputeIntParam(deferredComputeShader, "g_heightRT", h);
                        cmd.SetComputeIntParam(deferredComputeShader, "g_nNumDirLights", numDirLights);
                        cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_dirLightData", s_DirLightList);
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, "uavOutput", new RenderTargetIdentifier(s_CameraTarget));

                        SetMatrixArrayCS(cmd, deferredComputeShader, "g_matWorldToShadow", m_MatWorldToShadow);
                        SetVectorArrayCS(cmd, deferredComputeShader, "g_vDirShadowSplitSpheres", m_DirShadowSplitSpheres);
                        cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms0", m_Shadow3X3PCFTerms[0]);
                        cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms1", m_Shadow3X3PCFTerms[1]);
                        cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms2", m_Shadow3X3PCFTerms[2]);
                        cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms3", m_Shadow3X3PCFTerms[3]);

                        cmd.DispatchCompute(deferredComputeShader, kernel, numTilesX, numTilesY, 1);
                    }
                    else
                    {*/

                    if (disableTileAndCluster)
                    {
                        cmd.Blit(null, cameraColorBufferRT, m_SingleDeferredMaterial, 0);
                    }
                    else
                    {
                        if (enableSplitLightEvaluation)
                        {
                            cmd.Blit(null, cameraColorBufferRT, m_DeferredDirectMaterial, 0);
                            cmd.Blit(null, cameraColorBufferRT, m_DeferredIndirectMaterial, 0);
                        }
                        else
                        {
                            cmd.Blit(null, cameraColorBufferRT, m_DeferredAllMaterial, 0);
                        }

                        if (debugViewTilesFlags != 0)
                        {
                            cmd.Blit(null, cameraColorBufferRT, m_DebugViewTilesMaterial, 0);
                        }
                    }

                    //}

                    renderLoop.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                } // TilePass - Deferred Lighting Pass
            }

            public override void RenderForward(Camera camera, RenderLoop renderLoop, bool renderOpaque)
            {
                // Note: if we use render opaque with deferred tiling we need to render a opque depth pass for these opaque objects
                bool useFptl = renderOpaque && usingFptl;

                var cmd = new CommandBuffer();

                if (disableTileAndCluster)
                {
                    cmd.name = "Forward pass";
                    cmd.EnableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                    cmd.DisableShaderKeyword("LIGHTLOOP_TILE_PASS");
                }
                else
                {
                    cmd.name = useFptl ? "Forward Tiled pass" : "Forward Clustered pass";
                    cmd.EnableShaderKeyword("LIGHTLOOP_TILE_PASS");
                    cmd.DisableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                    cmd.SetGlobalFloat("g_isOpaquesOnlyEnabled", useFptl ? 1 : 0);      // leaving this as a dynamic toggle for now for forward opaques to keep shader variants down.
                    cmd.SetGlobalBuffer("g_vLightListGlobal", useFptl ? s_LightList : s_PerVoxelLightLists);
                }

                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }
        }
    }
}

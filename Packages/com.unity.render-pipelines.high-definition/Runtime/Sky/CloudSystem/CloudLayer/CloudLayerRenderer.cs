using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    class CloudLayerRenderer : CloudRenderer
    {
        Material m_CloudLayerMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        private float lastTime = 0.0f;
        int m_LastPrecomputationParamHash;

        static readonly int _CloudTexture = Shader.PropertyToID("_CloudTexture");
        static readonly int _CloudShadows = Shader.PropertyToID("_CloudShadows");
        static readonly int _FlowmapA = Shader.PropertyToID("_FlowmapA"), _FlowmapB = Shader.PropertyToID("_FlowmapB");
        static readonly int _CloudMapA = Shader.PropertyToID("_CloudMapA"), _CloudMapB = Shader.PropertyToID("_CloudMapB");
        static ComputeShader s_BakeCloudTextureCS, s_BakeCloudShadowsCS;
        static int s_BakeCloudTextureKernel, s_BakeCloudShadowsKernel;
        static readonly Vector4[] s_VectorArray = new Vector4[2];


        public CloudLayerRenderer()
        {
        }

        public override void Build()
        {
            var globalSettings = HDRenderPipelineGlobalSettings.instance;
            m_CloudLayerMaterial = CoreUtils.CreateEngineMaterial(globalSettings.renderPipelineResources.shaders.cloudLayerPS);

            s_BakeCloudTextureCS = globalSettings.renderPipelineResources.shaders.bakeCloudTextureCS;
            s_BakeCloudTextureKernel = s_BakeCloudTextureCS.FindKernel("BakeCloudTexture");

            s_BakeCloudShadowsCS = globalSettings.renderPipelineResources.shaders.bakeCloudShadowsCS;
            s_BakeCloudShadowsKernel = s_BakeCloudShadowsCS.FindKernel("BakeCloudShadows");
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_CloudLayerMaterial);

            if (m_PrecomputedData != null)
            {
                s_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_LastPrecomputationParamHash = 0;
                m_PrecomputedData = null;
            }
        }

        bool UpdateCache(CloudLayer cloudLayer, Light sunLight)
        {
            int currPrecomputationParamHash = cloudLayer.GetBakingHashCode(sunLight);
            if (currPrecomputationParamHash != m_LastPrecomputationParamHash)
            {
                s_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_PrecomputedData = s_PrecomputationCache.Get(cloudLayer, currPrecomputationParamHash);
                m_LastPrecomputationParamHash = currPrecomputationParamHash;
                return true;
            }
            return false;
        }

        protected override bool Update(BuiltinSkyParameters builtinParams)
            => UpdateCache(builtinParams.cloudSettings as CloudLayer, builtinParams.sunLight);

        public override bool GetSunLightCookieParameters(CloudSettings settings, ref CookieParameters cookieParams)
        {
            var cloudLayer = (CloudLayer)settings;
            if (cloudLayer.CastShadows)
            {
                if (m_PrecomputedData == null || m_PrecomputedData.cloudShadowsRT == null)
                    UpdateCache(cloudLayer, HDRenderPipeline.currentPipeline.GetMainLight());

                cookieParams.texture = m_PrecomputedData.cloudShadowsRT;
                cookieParams.size = new Vector2(cloudLayer.shadowSize.value, cloudLayer.shadowSize.value);
                return true;
            }
            return false;
        }

        public override void RenderSunLightCookie(BuiltinSunCookieParameters builtinParams)
            => m_PrecomputedData.BakeCloudShadows((CloudLayer)builtinParams.cloudSettings, builtinParams.sunLight, builtinParams.hdCamera, builtinParams.commandBuffer);

        public override void RenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            var hdCamera = builtinParams.hdCamera;
            var cmd = builtinParams.commandBuffer;
            var cloudLayer = builtinParams.cloudSettings as CloudLayer;
            if (cloudLayer.opacity.value == 0.0f)
                return;

            float dt = hdCamera.animateMaterials ? hdCamera.time - lastTime : 0.0f;
            lastTime = hdCamera.time;

            m_PrecomputedData.InitIfNeeded(cloudLayer, builtinParams.sunLight, builtinParams.commandBuffer);
            m_CloudLayerMaterial.SetTexture(_CloudTexture, m_PrecomputedData.cloudTextureRT);

            float intensity = builtinParams.sunLight ? builtinParams.sunLight.intensity : 1;
            var paramsA = cloudLayer.layerA.GetRenderingParameters(hdCamera, intensity);
            var paramsB = cloudLayer.layerB.GetRenderingParameters(hdCamera, intensity);
            paramsA.Item1.w = cloudLayer.upperHemisphereOnly.value ? 1 : 0;
            paramsB.Item1.w = cloudLayer.opacity.value;

            s_VectorArray[0] = paramsA.Item1; s_VectorArray[1] = paramsB.Item1;
            m_CloudLayerMaterial.SetVectorArray(HDShaderIDs._FlowmapParam, s_VectorArray);
            s_VectorArray[0] = paramsA.Item2; s_VectorArray[1] = paramsB.Item2;
            m_CloudLayerMaterial.SetVectorArray(HDShaderIDs._ColorFilter, s_VectorArray);

            CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_CLOUD_MOTION", cloudLayer.layerA.distortionMode.value != CloudDistortionMode.None);
            if (cloudLayer.layerA.distortionMode.value != CloudDistortionMode.None)
                cloudLayer.layerA.scrollFactor += cloudLayer.layerA.scrollSpeed.GetValue(hdCamera) * dt * 0.01f;
            CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_FLOWMAP", cloudLayer.layerA.distortionMode.value == CloudDistortionMode.Flowmap);
            if (cloudLayer.layerA.distortionMode.value == CloudDistortionMode.Flowmap)
                m_CloudLayerMaterial.SetTexture(_FlowmapA, cloudLayer.layerA.flowmap.value);

            CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_SECOND_CLOUD_LAYER", cloudLayer.layers.value == CloudMapMode.Double);
            if (cloudLayer.layers.value == CloudMapMode.Double)
            {
                CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_SECOND_CLOUD_MOTION", cloudLayer.layerB.distortionMode.value != CloudDistortionMode.None);
                if (cloudLayer.layerB.distortionMode.value != CloudDistortionMode.None)
                    cloudLayer.layerB.scrollFactor += cloudLayer.layerB.scrollSpeed.GetValue(hdCamera) * dt * 0.01f;
                CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_SECOND_FLOWMAP", cloudLayer.layerB.distortionMode.value == CloudDistortionMode.Flowmap);
                if (cloudLayer.layerB.distortionMode.value == CloudDistortionMode.Flowmap)
                    m_CloudLayerMaterial.SetTexture(_FlowmapB, cloudLayer.layerB.flowmap.value);
            }

            // This matrix needs to be updated at the draw call frequency.
            m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            if (renderForCubemap)
            {
                CoreUtils.SetRenderTarget(cmd, builtinParams.colorBuffer, ClearFlag.None, 0, builtinParams.cubemapFace);
                CoreUtils.DrawFullScreen(cmd, m_CloudLayerMaterial, m_PropertyBlock, 0);
            }
            else
            {
                if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
                    CoreUtils.SetRenderTarget(cmd, builtinParams.colorBuffer);
                else
                    CoreUtils.SetRenderTarget(cmd, builtinParams.colorBuffer, builtinParams.depthBuffer);
                CoreUtils.DrawFullScreen(cmd, m_CloudLayerMaterial, m_PropertyBlock, 1);
            }
        }

        class PrecomputationCache
        {
            class RefCountedData
            {
                public int refCount;
                public PrecomputationData data = new PrecomputationData();
            }

            ObjectPool<RefCountedData> m_DataPool = new ObjectPool<RefCountedData>(null, null);
            Dictionary<int, RefCountedData> m_CachedData = new Dictionary<int, RefCountedData>();

            public PrecomputationData Get(CloudLayer cloudLayer, int currentHash)
            {
                RefCountedData result;
                if (m_CachedData.TryGetValue(currentHash, out result))
                {
                    result.refCount++;
                    return result.data;
                }
                else
                {
                    result = m_DataPool.Get();
                    result.refCount = 1;
                    result.data.Allocate(cloudLayer);
                    m_CachedData.Add(currentHash, result);
                    return result.data;
                }
            }

            public void Release(int hash)
            {
                if (m_CachedData.TryGetValue(hash, out var result))
                {
                    result.refCount--;
                    if (result.refCount == 0)
                    {
                        result.data.Release();
                        m_CachedData.Remove(hash);
                        m_DataPool.Release(result);
                    }
                }
            }
        }

        class PrecomputationData
        {
            struct TextureCache
            {
                int width, height;
                RTHandle rt;
                public bool TryGet(int textureWidth, int textureHeight, ref RTHandle texture)
                {
                    if (rt == null || textureWidth != width || textureHeight != height)
                        return false;

                    texture = rt;
                    rt = null;
                    return true;
                }

                public void Cache(int textureWidth, int textureHeight, RTHandle texture)
                {
                    if (texture == null)
                        return;
                    if (rt != null)
                        RTHandles.Release(rt);
                    width = textureWidth;
                    height = textureHeight;
                    rt = texture;
                }
            }
            static TextureCache cloudTextureCache;
            static TextureCache cloudShadowsCache;

            bool initialized = false;
            int cloudTextureWidth, cloudTextureHeight, cloudShadowsResolution;
            public RTHandle cloudTextureRT = null;
            public RTHandle cloudShadowsRT = null;

            public void Allocate(CloudLayer cloudLayer)
            {
                initialized = false;
                cloudTextureWidth = (int)cloudLayer.resolution.value;
                cloudTextureHeight = cloudLayer.upperHemisphereOnly.value ? cloudTextureWidth / 2 : cloudTextureWidth;
                if (!cloudTextureCache.TryGet(cloudTextureWidth, cloudTextureHeight, ref cloudTextureRT))
                    cloudTextureRT = RTHandles.Alloc(cloudTextureWidth, cloudTextureHeight, TextureWrapMode.Repeat, TextureWrapMode.Clamp,
                        slices: cloudLayer.NumLayers, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureDimension.Tex2DArray,
                        enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear, name: "Cloud Texture");

                cloudShadowsRT = null;
                cloudShadowsResolution = (int)cloudLayer.shadowResolution.value;
                if (cloudLayer.CastShadows && !cloudShadowsCache.TryGet(cloudShadowsResolution, cloudShadowsResolution, ref cloudShadowsRT))
                    cloudShadowsRT = RTHandles.Alloc(cloudShadowsResolution, cloudShadowsResolution,
                        colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, dimension: TextureDimension.Tex2D,
                        enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear, name: "Cloud Shadows");
            }

            public void Release()
            {
                cloudTextureCache.Cache(cloudTextureHeight, cloudTextureHeight, cloudTextureRT);
                cloudShadowsCache.Cache(cloudShadowsResolution, cloudShadowsResolution, cloudShadowsRT);
            }

            public bool InitIfNeeded(CloudLayer cloudLayer, Light sunLight, CommandBuffer cmd)
            {
                if (initialized)
                    return false;

                Vector4 params1 = sunLight == null ? Vector3.zero : sunLight.transform.forward;
                params1.w = (cloudLayer.upperHemisphereOnly.value ? 1.0f : 0.0f);

                cmd.SetComputeVectorParam(s_BakeCloudTextureCS, HDShaderIDs._Params, params1);
                cmd.SetComputeTextureParam(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, _CloudTexture, cloudTextureRT);

                cmd.SetComputeTextureParam(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, _CloudMapA, cloudLayer.layerA.cloudMap.value);
                var paramsA = cloudLayer.layerA.GetBakingParameters();
                paramsA.Item2.w = 1.0f / cloudTextureWidth;

                if (cloudLayer.NumLayers == 1)
                {
                    s_BakeCloudTextureCS.DisableKeyword("USE_SECOND_CLOUD_LAYER");
                    cmd.SetComputeVectorParam(s_BakeCloudTextureCS, HDShaderIDs._Params1, paramsA.Item1);
                    cmd.SetComputeVectorParam(s_BakeCloudTextureCS, HDShaderIDs._Params2, paramsA.Item2);
                }
                else
                {
                    cmd.SetComputeTextureParam(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, _CloudMapB, cloudLayer.layerB.cloudMap.value);
                    var paramsB = cloudLayer.layerB.GetBakingParameters();

                    s_BakeCloudTextureCS.EnableKeyword("USE_SECOND_CLOUD_LAYER");
                    s_VectorArray[0] = paramsA.Item1; s_VectorArray[1] = paramsB.Item1;
                    cmd.SetComputeVectorArrayParam(s_BakeCloudTextureCS, HDShaderIDs._Params1, s_VectorArray);
                    s_VectorArray[0] = paramsA.Item2; s_VectorArray[1] = paramsB.Item2;
                    cmd.SetComputeVectorArrayParam(s_BakeCloudTextureCS, HDShaderIDs._Params2, s_VectorArray);
                }

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = (cloudTextureWidth + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = (cloudTextureHeight + (groupSizeY - 1)) / groupSizeY;

                cmd.DispatchCompute(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, threadGroupX, threadGroupY, 1);

                initialized = true;
                return true;
            }

            public void BakeCloudShadows(CloudLayer cloudLayer, Light sunLight, HDCamera camera, CommandBuffer cmd)
            {
                InitIfNeeded(cloudLayer, sunLight, cmd);
                Vector4 _Params = sunLight.transform.forward;
                Vector4 _Params1 = sunLight.transform.right;
                Vector4 _Params2 = sunLight.transform.up;
                Vector4 _Params3 = cloudLayer.shadowTint.value;
                _Params.w = 1.0f / cloudShadowsResolution;
                _Params3.w = cloudLayer.shadowMultiplier.value * 8.0f;

                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params, _Params);
                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params1, _Params1);
                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params2, _Params2);
                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params3, _Params3);

                cmd.SetComputeTextureParam(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, _CloudTexture, cloudTextureRT);
                cmd.SetComputeTextureParam(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, _CloudShadows, cloudShadowsRT);

                var paramsA = cloudLayer.layerA.GetRenderingParameters(camera, 0);
                var paramsB = cloudLayer.layerB.GetRenderingParameters(camera, 0);
                paramsA.Item1.z = paramsA.Item1.z * 0.2f;
                paramsB.Item1.z = paramsB.Item1.z * 0.2f;
                paramsA.Item1.w = cloudLayer.upperHemisphereOnly.value ? 1 : 0;
                paramsB.Item1.w = cloudLayer.opacity.value;

                s_VectorArray[0] = paramsA.Item1; s_VectorArray[1] = paramsB.Item1;
                cmd.SetComputeVectorArrayParam(s_BakeCloudShadowsCS, HDShaderIDs._FlowmapParam, s_VectorArray);

                bool useSecond = (cloudLayer.layers.value == CloudMapMode.Double) && cloudLayer.layerB.castShadows.value;
                CoreUtils.SetKeyword(s_BakeCloudShadowsCS, "DISABLE_MAIN_LAYER", !cloudLayer.layerA.castShadows.value);
                CoreUtils.SetKeyword(s_BakeCloudShadowsCS, "USE_SECOND_CLOUD_LAYER", useSecond);

                if (cloudLayer.layerA.castShadows.value)
                {
                    CoreUtils.SetKeyword(s_BakeCloudShadowsCS, "USE_CLOUD_MOTION", cloudLayer.layerA.distortionMode.value != CloudDistortionMode.None);
                    CoreUtils.SetKeyword(s_BakeCloudShadowsCS, "USE_FLOWMAP", cloudLayer.layerA.distortionMode.value == CloudDistortionMode.Flowmap);
                    if (cloudLayer.layerA.distortionMode.value == CloudDistortionMode.Flowmap)
                        cmd.SetComputeTextureParam(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, _FlowmapA, cloudLayer.layerA.flowmap.value);
                }

                if (useSecond)
                {
                    CoreUtils.SetKeyword(s_BakeCloudShadowsCS, "USE_SECOND_CLOUD_MOTION", cloudLayer.layerB.distortionMode.value != CloudDistortionMode.None);
                    CoreUtils.SetKeyword(s_BakeCloudShadowsCS, "USE_SECOND_FLOWMAP", cloudLayer.layerB.distortionMode.value == CloudDistortionMode.Flowmap);
                    if (cloudLayer.layerB.distortionMode.value == CloudDistortionMode.Flowmap)
                        cmd.SetComputeTextureParam(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, _FlowmapB, cloudLayer.layerB.flowmap.value);
                }

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = (cloudShadowsResolution + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = (cloudShadowsResolution + (groupSizeY - 1)) / groupSizeY;

                cmd.DispatchCompute(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, threadGroupX, threadGroupY, 1);
                cloudShadowsRT.rt.IncrementUpdateCount();
            }
        }

        static PrecomputationCache s_PrecomputationCache = new PrecomputationCache();
        PrecomputationData m_PrecomputedData;
    }
}

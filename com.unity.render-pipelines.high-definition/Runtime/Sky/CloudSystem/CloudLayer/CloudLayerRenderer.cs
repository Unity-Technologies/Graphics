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
        static readonly int _FlowmapA = Shader.PropertyToID("_FlowmapA");
        static readonly int _FlowmapB = Shader.PropertyToID("_FlowmapB");
        static ComputeShader s_BakeCloudTextureCS, s_BakeCloudShadowsCS;
        static int s_BakeCloudTextureKernel, s_BakeCloudShadowsKernel;
        static Vector4[] s_VectorArray = new Vector4[2];


        public CloudLayerRenderer()
        {
        }

        public override void Build()
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            m_CloudLayerMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cloudLayerPS);

            s_BakeCloudTextureCS = hdrp.renderPipelineResources.shaders.bakeCloudTextureCS;
            s_BakeCloudTextureKernel = s_BakeCloudTextureCS.FindKernel("BakeCloudTexture");

            s_BakeCloudShadowsCS = hdrp.renderPipelineResources.shaders.bakeCloudShadowsCS;
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

        protected override bool Update(BuiltinSkyParameters builtinParams)
        {
            var cloudLayer = builtinParams.cloudSettings as CloudLayer;

            int currPrecomputationParamHash = cloudLayer.GetBakingHashCode(builtinParams.sunLight);
            if (currPrecomputationParamHash != m_LastPrecomputationParamHash)
            {
                s_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_PrecomputedData = s_PrecomputationCache.Get(builtinParams, currPrecomputationParamHash);
                m_LastPrecomputationParamHash = currPrecomputationParamHash;
                return true;
            }

            return false;
        }

        public override bool RequiresPreRenderClouds(BuiltinSkyParameters builtinParams)
        {
            var cloudLayer = builtinParams.cloudSettings as CloudLayer;
            return builtinParams.sunLight != null && cloudLayer.CastShadows;
        }

        public override void PreRenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            if (renderForCubemap)
                return;

            m_PrecomputedData.BakeCloudShadows(builtinParams);

            var cmd = builtinParams.commandBuffer;
            cmd.SetGlobalTexture(HDShaderIDs._DirectionalShadowCookie, m_PrecomputedData.cloudShadowsRT);
        }

        public override void RenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            var cmd = builtinParams.commandBuffer;
            var cloudLayer = builtinParams.cloudSettings as CloudLayer;
            if (cloudLayer.opacity.value == 0.0f)
                return;

#if UNITY_EDITOR
            float time = (float)EditorApplication.timeSinceStartup;
#else
            float time = Time.time;
#endif
            float dt = time - lastTime;
            lastTime = time;

            m_CloudLayerMaterial.SetTexture(_CloudTexture, m_PrecomputedData.cloudTextureRT);

            var paramsA = cloudLayer.layerA.GetRenderingParameters();
            var paramsB = cloudLayer.layerB.GetRenderingParameters();
            paramsA.Item1.w = cloudLayer.upperHemisphereOnly.value ? 1 : 0;
            paramsB.Item1.w = cloudLayer.opacity.value;

            s_VectorArray[0] = paramsA.Item1; s_VectorArray[1] = paramsB.Item1;
            m_CloudLayerMaterial.SetVectorArray(HDShaderIDs._FlowmapParam, s_VectorArray);
            s_VectorArray[0] = paramsA.Item2; s_VectorArray[1] = paramsB.Item2;
            m_CloudLayerMaterial.SetVectorArray(HDShaderIDs._ColorFilter, s_VectorArray);

            CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_CLOUD_MOTION", cloudLayer.layerA.distortionMode.value != CloudDistortionMode.None);
            if (cloudLayer.layerA.distortionMode.value != CloudDistortionMode.None)
                cloudLayer.layerA.scrollFactor += cloudLayer.layerA.scrollSpeed.value * dt * 0.01f;
            CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_FLOWMAP", cloudLayer.layerA.distortionMode.value == CloudDistortionMode.Flowmap);
            if (cloudLayer.layerA.distortionMode.value == CloudDistortionMode.Flowmap)
                m_CloudLayerMaterial.SetTexture(_FlowmapA, cloudLayer.layerA.flowmap.value);

            CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_SECOND_CLOUD_LAYER", cloudLayer.layers.value == CloudMapMode.Double);
            if (cloudLayer.layers.value == CloudMapMode.Double)
            {
                CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_SECOND_CLOUD_MOTION", cloudLayer.layerB.distortionMode.value != CloudDistortionMode.None);
                if (cloudLayer.layerB.distortionMode.value != CloudDistortionMode.None)
                    cloudLayer.layerB.scrollFactor += cloudLayer.layerB.scrollSpeed.value * dt * 0.01f;
                CoreUtils.SetKeyword(m_CloudLayerMaterial, "USE_SECOND_FLOWMAP", cloudLayer.layerB.distortionMode.value == CloudDistortionMode.Flowmap);
                if (cloudLayer.layerB.distortionMode.value == CloudDistortionMode.Flowmap)
                    m_CloudLayerMaterial.SetTexture(_FlowmapB, cloudLayer.layerB.flowmap.value);
            }

            // This matrix needs to be updated at the draw call frequency.
            m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            CoreUtils.DrawFullScreen(cmd, m_CloudLayerMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
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

            public PrecomputationData Get(BuiltinSkyParameters builtinParams, int currentHash)
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
                    result.data.Allocate(builtinParams);
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
            public RTHandle cloudTextureRT = null;
            public RTHandle cloudShadowsRT = null;

            // TODO
            int m_CloudResolution = 1024;
            int m_CloudShadowsResolution = 512;

            public void Allocate(BuiltinSkyParameters builtinParams)
            {
                var cloudLayer = builtinParams.cloudSettings as CloudLayer;

                cloudTextureRT = RTHandles.Alloc(m_CloudResolution, cloudLayer.upperHemisphereOnly.value ? m_CloudResolution / 2 : m_CloudResolution,
                    cloudLayer.NumLayers, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureDimension.Tex2DArray,
                    enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear, name: "Cloud Texture");

                if (cloudLayer.CastShadows)
                    cloudShadowsRT = RTHandles.Alloc(m_CloudShadowsResolution, m_CloudShadowsResolution,
                        colorFormat: GraphicsFormat.R16_SNorm, dimension: TextureDimension.Tex2D,
                        enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear, name: "Cloud Shadows");

                BakeCloudTexture(builtinParams);
            }

            public void Release()
            {
                RTHandles.Release(cloudTextureRT);
                RTHandles.Release(cloudShadowsRT);
            }

            void BakeCloudTexture(BuiltinSkyParameters builtinParams)
            {
                var cmd = builtinParams.commandBuffer;
                var cloudLayer = builtinParams.cloudSettings as CloudLayer;

                Vector4 params1 = builtinParams.sunLight == null ? Vector3.zero : builtinParams.sunLight.transform.forward;
                params1.w = (cloudLayer.upperHemisphereOnly.value ? 1.0f : 0.0f);

                cmd.SetComputeVectorParam(s_BakeCloudTextureCS, HDShaderIDs._Params, params1);
                cmd.SetComputeTextureParam(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, _CloudTexture, cloudTextureRT);

                cmd.SetComputeTextureParam(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, "_CloudMapA", cloudLayer.layerA.cloudMap.value);
                var paramsA = cloudLayer.layerA.GetBakingParameters();
                paramsA.Item2.w = 1.0f / m_CloudResolution;

                if (cloudLayer.NumLayers == 1)
                {
                    s_BakeCloudTextureCS.DisableKeyword("USE_SECOND_CLOUD_LAYER");
                    cmd.SetComputeVectorParam(s_BakeCloudTextureCS, HDShaderIDs._Params1, paramsA.Item1);
                    cmd.SetComputeVectorParam(s_BakeCloudTextureCS, HDShaderIDs._Params2, paramsA.Item2);
                }
                else
                {
                    cmd.SetComputeTextureParam(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, "_CloudMapB", cloudLayer.layerB.cloudMap.value);
                    var paramsB = cloudLayer.layerB.GetBakingParameters();

                    s_BakeCloudTextureCS.EnableKeyword("USE_SECOND_CLOUD_LAYER");
                    s_VectorArray[0] = paramsA.Item1; s_VectorArray[1] = paramsB.Item1;
                    cmd.SetComputeVectorArrayParam(s_BakeCloudTextureCS, HDShaderIDs._Params1, s_VectorArray);
                    s_VectorArray[0] = paramsA.Item2; s_VectorArray[1] = paramsB.Item2;
                    cmd.SetComputeVectorArrayParam(s_BakeCloudTextureCS, HDShaderIDs._Params2, s_VectorArray);
                }

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = (m_CloudResolution + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = (m_CloudResolution / 2 + (groupSizeY - 1)) / groupSizeY;

                cmd.DispatchCompute(s_BakeCloudTextureCS, s_BakeCloudTextureKernel, threadGroupX, threadGroupY, 1);
            }

            public void BakeCloudShadows(BuiltinSkyParameters builtinParams)
            {
                var cmd = builtinParams.commandBuffer;
                var cloudLayer = builtinParams.cloudSettings as CloudLayer;

                Vector4 _Params = builtinParams.sunLight.transform.forward;
                Vector4 _Params1 = builtinParams.sunLight.transform.right;
                Vector4 _Params2 = builtinParams.sunLight.transform.up;
                _Params.w = 1.0f / (float)m_CloudShadowsResolution;
                _Params1.w = 50.0f * cloudLayer.shadowsOpacity.value;

                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params, _Params);
                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params1, _Params1);
                cmd.SetComputeVectorParam(s_BakeCloudShadowsCS, HDShaderIDs._Params2, _Params2);

                cmd.SetComputeTextureParam(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, _CloudTexture, cloudTextureRT);
                cmd.SetComputeTextureParam(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, _CloudShadows, cloudShadowsRT);

                var paramsA = cloudLayer.layerA.GetRenderingParameters();
                var paramsB = cloudLayer.layerB.GetRenderingParameters();
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
                int threadGroupX = (m_CloudShadowsResolution + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = (m_CloudShadowsResolution + (groupSizeY - 1)) / groupSizeY;

                cmd.DispatchCompute(s_BakeCloudShadowsCS, s_BakeCloudShadowsKernel, threadGroupX, threadGroupY, 1);
            }
        }

        static PrecomputationCache  s_PrecomputationCache = new PrecomputationCache();
        PrecomputationData          m_PrecomputedData;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

using ShadingSource = UnityEngine.Rendering.HighDefinition.HDAdditionalLightData.CelestialBodyShadingSource;

namespace UnityEngine.Rendering.HighDefinition
{
    class PhysicallyBasedSkyRenderer : SkyRenderer
    {
        static bool SupportSpace => ShaderConfig.s_PrecomputedAtmosphericAttenuation == 0;

        class PrecomputationCache
        {
            class RefCountedData
            {
                public int refCount;
                public PrecomputationData data = new PrecomputationData();
            }

            ObjectPool<RefCountedData> m_DataPool = new ObjectPool<RefCountedData>(null, null);
            Dictionary<int, RefCountedData> m_CachedData = new Dictionary<int, RefCountedData>();

            public bool HasAliveData() => m_CachedData.Count != 0;

            public PrecomputationData Get(BuiltinSkyParameters builtinParams, int hash, PhysicallyBasedSkyRenderer physicallyBasedSkyRenderer)
            {
                RefCountedData result;
                if (m_CachedData.TryGetValue(hash, out result))
                {
                    result.refCount++;
                    return result.data;
                }
                else
                {
                    result = m_DataPool.Get();
                    result.refCount = 1;
                    result.data.Allocate(builtinParams, physicallyBasedSkyRenderer);
                    m_CachedData.Add(hash, result);
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
            // Local sky
            RTHandle m_GroundIrradianceTable;
            RTHandle[] m_InScatteredRadianceTables; // Air SS, Aerosol SS, Atmosphere MS

            // Distant sky
            RTHandle m_MultiScatteringLut, m_SkyViewLut;
            int m_LastLightsHash;

            RTHandle m_AtmosphericScatteringLut;

            PhysicallyBasedSkyRenderer m_PhysicallyBasedSkyRenderer;

            // BUG: https://jira.unity3d.com/browse/UUM-86915 RwTex3D outputs red. Disable on some devices as a workaround.
            bool m_EnableAtmosphericScatteringBlur;

            bool IsWorldSpace() => m_InScatteredRadianceTables != null;

            RTHandle AllocateGroundIrradianceTable()
            {
                var table = RTHandles.Alloc((int)PbrSkyConfig.GroundIrradianceTableSize, 1,
                    colorFormat: m_PhysicallyBasedSkyRenderer.m_ColorFormat,
                    enableRandomWrite: true,
                    name: "GroundIrradianceTable");

                Debug.Assert(table != null);

                return table;
            }

            RTHandle AllocateInScatteredRadianceTable(int index)
            {
                // Emulate a 4D texture with a "deep" 3D texture.
                var table = RTHandles.Alloc((int)PbrSkyConfig.InScatteredRadianceTableSizeX,
                    (int)PbrSkyConfig.InScatteredRadianceTableSizeY,
                    (int)PbrSkyConfig.InScatteredRadianceTableSizeZ *
                    (int)PbrSkyConfig.InScatteredRadianceTableSizeW,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: m_PhysicallyBasedSkyRenderer.m_ColorFormat,
                    enableRandomWrite: true,
                    name: $"InScatteredRadianceTable{index}");

                Debug.Assert(table != null);

                return table;
            }

            public void Allocate(BuiltinSkyParameters builtinParams, PhysicallyBasedSkyRenderer physicallyBasedSkyRenderer)
            {
                m_PhysicallyBasedSkyRenderer = physicallyBasedSkyRenderer;

                var cmd = builtinParams.commandBuffer;
                var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

                m_MultiScatteringLut = RTHandles.Alloc(
                    (int)PbrSkyConfig.MultiScatteringLutWidth,
                    (int)PbrSkyConfig.MultiScatteringLutHeight,
                    colorFormat: m_PhysicallyBasedSkyRenderer.m_ColorFormat,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "MultiScatteringLUT");

                RenderMultiScatteringLut(cmd);

                if (!SupportSpace && builtinParams.hdCamera.planet.renderingSpace == RenderingSpace.Camera)
                {
                    m_LastLightsHash = -1;

                    m_SkyViewLut = RTHandles.Alloc(
                        (int)PbrSkyConfig.SkyViewLutWidth,
                        (int)PbrSkyConfig.SkyViewLutHeight,
                        colorFormat: m_PhysicallyBasedSkyRenderer.m_ColorFormat,
                        filterMode: FilterMode.Bilinear,
                        wrapModeU: TextureWrapMode.Repeat,
                        wrapModeV: TextureWrapMode.Clamp,
                        enableRandomWrite: true,
                        name: "SkyViewLUT");
                }
                else
                {
                    m_GroundIrradianceTable = AllocateGroundIrradianceTable();

                    m_InScatteredRadianceTables = new RTHandle[3];
                    m_InScatteredRadianceTables[0] = AllocateInScatteredRadianceTable(0);
                    m_InScatteredRadianceTables[1] = AllocateInScatteredRadianceTable(1);
                    m_InScatteredRadianceTables[2] = AllocateInScatteredRadianceTable(2);

                    PrecomputeTables(cmd);
                }

                if (!SupportSpace && pbrSky.atmosphericScattering.value)
                {
                    m_AtmosphericScatteringLut = RTHandles.Alloc(
                        (int)PbrSkyConfig.AtmosphericScatteringLutWidth,
                        (int)PbrSkyConfig.AtmosphericScatteringLutHeight,
                        (int)PbrSkyConfig.AtmosphericScatteringLutDepth,
                        dimension: TextureDimension.Tex3D,
                        colorFormat: m_PhysicallyBasedSkyRenderer.m_ColorFormat,
                        enableRandomWrite: true,
                        name: "AtmosphericScatteringLUT");

                    // BUG: https://jira.unity3d.com/browse/UUM-86915
                    var graphicsDeviceName = SystemInfo.graphicsDeviceName;
                    m_EnableAtmosphericScatteringBlur = !(m_PhysicallyBasedSkyRenderer.m_ColorFormat == GraphicsFormat.B10G11R11_UFloatPack32 &&
                                                          graphicsDeviceName.Contains("Graphics") &&
                                                          (graphicsDeviceName.Contains("HD")      ||    // and UHD
                                                           graphicsDeviceName.Contains("Iris")    ||
                                                           graphicsDeviceName.Contains("Xe")));
                }
            }

            public void Release()
            {
                m_PhysicallyBasedSkyRenderer = null;

                if (m_MultiScatteringLut != null)
                {
                    RTHandles.Release(m_MultiScatteringLut); m_MultiScatteringLut = null;
                }

                if (IsWorldSpace())
                {
                    RTHandles.Release(m_GroundIrradianceTable); m_GroundIrradianceTable = null;
                    RTHandles.Release(m_InScatteredRadianceTables[0]); m_InScatteredRadianceTables[0] = null;
                    RTHandles.Release(m_InScatteredRadianceTables[1]); m_InScatteredRadianceTables[1] = null;
                    RTHandles.Release(m_InScatteredRadianceTables[2]); m_InScatteredRadianceTables[2] = null;
                    m_InScatteredRadianceTables = null;
                }
                else
                {
                    RTHandles.Release(m_SkyViewLut); m_SkyViewLut = null;
                }

                if (m_AtmosphericScatteringLut != null)
                {
                    RTHandles.Release(m_AtmosphericScatteringLut); m_AtmosphericScatteringLut = null;
                }
            }

            void RenderMultiScatteringLut(CommandBuffer cmd)
            {
                cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_MultiScatteringKernel, HDShaderIDs._MultiScatteringLUT_RW, m_MultiScatteringLut);

                cmd.DispatchCompute(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_MultiScatteringKernel,
                    (int)PbrSkyConfig.MultiScatteringLutWidth,
                    (int)PbrSkyConfig.MultiScatteringLutHeight,
                    1);
            }

            void PrecomputeTables(CommandBuffer cmd)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.InScatteredRadiancePrecomputation)))
                {
                    // Multiple scattering LUT
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._AirSingleScatteringTable, m_InScatteredRadianceTables[0]);
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._AerosolSingleScatteringTable, m_InScatteredRadianceTables[1]);
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._MultipleScatteringTable, m_InScatteredRadianceTables[2]);
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._MultiScatteringLUT, m_MultiScatteringLut);

                    // Emulate a 4D dispatch with a "deep" 3D dispatch.
                    cmd.DispatchCompute(m_PhysicallyBasedSkyRenderer.m_InScatteredRadiancePrecomputationCS, 0, (int)PbrSkyConfig.InScatteredRadianceTableSizeX / 4,
                        (int)PbrSkyConfig.InScatteredRadianceTableSizeY / 4,
                        (int)PbrSkyConfig.InScatteredRadianceTableSizeZ / 4 *
                        (int)PbrSkyConfig.InScatteredRadianceTableSizeW);

                    // Ground irradiance LUT
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[2]);

                    cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._GroundIrradianceTable, m_GroundIrradianceTable);

                    cmd.DispatchCompute(m_PhysicallyBasedSkyRenderer.m_GroundIrradiancePrecomputationCS, 0, (int)PbrSkyConfig.GroundIrradianceTableSize / 64, 1, 1);
                }
            }

            internal static float CastFloat(float value, int size)
            {
                return (int)(value * size) / (float)size;
            }

            // Computes hash code of light parameters used during sky view lut precomputation
            int GetLightsHash()
            {
                int hash = 13;
                for (int i = 0; i < m_PhysicallyBasedSkyRenderer.m_CelestialLightCount; i++)
                {
                    ref var data = ref m_PhysicallyBasedSkyRenderer.m_CelestialBodyData[i];
                    hash = hash * 23 + data.forward.GetHashCode();
                    hash = hash * 23 + data.color.GetHashCode();
                }
                return hash;
            }

            internal void RenderSkyViewLut(CommandBuffer cmd)
            {
                int currLightsHash = GetLightsHash();
                if (currLightsHash == m_LastLightsHash) return;
                m_LastLightsHash = currLightsHash;

                cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_SkyViewKernel, HDShaderIDs._MultiScatteringLUT, m_MultiScatteringLut);
                cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_SkyViewKernel, HDShaderIDs._SkyViewLUT_RW, m_SkyViewLut);
                cmd.SetComputeBufferParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_SkyViewKernel, HDShaderIDs._CelestialBodyDatas, m_PhysicallyBasedSkyRenderer.m_CelestialBodyBuffer);

                cmd.DispatchCompute(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_SkyViewKernel,
                    (int)PbrSkyConfig.SkyViewLutWidth / 8,
                    (int)PbrSkyConfig.SkyViewLutHeight / 8,
                    1);
            }

            internal void RenderAtmosphericScatteringLut(BuiltinSkyParameters builtinParams)
            {
                var cmd = builtinParams.commandBuffer;
                cmd.SetComputeMatrixParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                int kernel = IsWorldSpace() ? m_PhysicallyBasedSkyRenderer.m_AtmosphericScatteringKernelWorld : m_PhysicallyBasedSkyRenderer.m_AtmosphericScatteringKernelCamera;

                cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, kernel, HDShaderIDs._MultiScatteringLUT, m_MultiScatteringLut);
                cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, kernel, HDShaderIDs._AtmosphericScatteringLUT_RW, m_AtmosphericScatteringLut);
                cmd.SetComputeBufferParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, kernel, HDShaderIDs._CelestialBodyDatas, m_PhysicallyBasedSkyRenderer.m_CelestialBodyBuffer);

                cmd.DispatchCompute(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, kernel,
                    (int)PbrSkyConfig.AtmosphericScatteringLutWidth,
                    (int)PbrSkyConfig.AtmosphericScatteringLutHeight,
                    1);

                // Perform a blur pass on the buffer to reduce resolution artefacts
                cmd.SetComputeTextureParam(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_AtmosphericScatteringBlurKernel, HDShaderIDs._AtmosphericScatteringLUT_RW, m_AtmosphericScatteringLut);

                if(m_EnableAtmosphericScatteringBlur)
                {
                    cmd.DispatchCompute(m_PhysicallyBasedSkyRenderer.m_SkyLUTGenerator, m_PhysicallyBasedSkyRenderer.m_AtmosphericScatteringBlurKernel,
                        1,
                        1,
                        (int)PbrSkyConfig.AtmosphericScatteringLutDepth);
                }

            }

            public void BindGlobalBuffers(CommandBuffer cmd)
            {
                cmd.SetGlobalBuffer(HDShaderIDs._CelestialBodyDatas, m_PhysicallyBasedSkyRenderer.m_CelestialBodyBuffer);

                if (SupportSpace)
                {
                    cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                    cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                    cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[2]);
                }
                else
                {
                    cmd.SetGlobalTexture(HDShaderIDs._AtmosphericScatteringLUT, m_AtmosphericScatteringLut ?? (RenderTargetIdentifier)CoreUtils.blackVolumeTexture);
                }
            }

            public void BindBuffers(MaterialPropertyBlock mpb)
            {
                if (IsWorldSpace())
                {
                    mpb.SetTexture(HDShaderIDs._GroundIrradianceTexture, m_GroundIrradianceTable);
                    mpb.SetTexture(HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                    mpb.SetTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                    mpb.SetTexture(HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[2]);
                }
                else
                {
                    mpb.SetTexture(HDShaderIDs._SkyViewLUT, m_SkyViewLut);
                }
            }
        }

        static readonly int s_ShaderVariablesPhysicallyBasedSkyID = Shader.PropertyToID("ShaderVariablesPhysicallyBasedSky");

        // Store the hash of the parameters each time precomputation is done.
        // If the hash does not match, we must recompute our data.
        int m_LastPrecomputationParamHash;

        // Precomputed data below.
        PrecomputationData m_PrecomputedData;

        Material m_PbrSkyMaterial;
        MaterialPropertyBlock m_PbrSkyMaterialProperties;

        PrecomputationCache m_PrecomputationCache = new PrecomputationCache();

        const int k_MaxCelestialBodies = 16;
        GraphicsBuffer m_CelestialBodyBuffer;
        CelestialBodyData[] m_CelestialBodyData;
        int m_DataFrameUpdate = -1;
        uint m_CelestialLightCount;
        uint m_CelestialBodyCount;
        float m_CelestialLightExposure;

        ShaderVariablesPhysicallyBasedSky m_ConstantBuffer;
        GraphicsFormat m_ColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;

        // Common resourcse
        ComputeShader m_SkyLUTGenerator;
        int m_MultiScatteringKernel, m_AtmosphericScatteringBlurKernel;

        // Resources for world space sky
        ComputeShader m_GroundIrradiancePrecomputationCS;
        ComputeShader m_InScatteredRadiancePrecomputationCS;
        int m_AtmosphericScatteringKernelWorld;

        // Resources for camera space sky
        int m_SkyViewKernel, m_AtmosphericScatteringKernelCamera;

        public override void Build()
        {
            var shaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdPipeline != null)
                m_ColorFormat = hdPipeline.GetColorBufferFormat();

            // Common
            m_SkyLUTGenerator = shaders.skyLUTGenerator;
            m_MultiScatteringKernel = m_SkyLUTGenerator.FindKernel("MultiScatteringLUT");
            m_AtmosphericScatteringBlurKernel = m_SkyLUTGenerator.FindKernel("AtmosphericScatteringBlur");

            // Camera space sky
            m_SkyViewKernel = m_SkyLUTGenerator.FindKernel("SkyViewLUT");
            m_AtmosphericScatteringKernelCamera = m_SkyLUTGenerator.FindKernel("AtmosphericScatteringLUTCamera");

            // World space sky
            m_GroundIrradiancePrecomputationCS = shaders.groundIrradiancePrecomputationCS;
            m_InScatteredRadiancePrecomputationCS = shaders.inScatteredRadiancePrecomputationCS;
            m_AtmosphericScatteringKernelWorld = m_SkyLUTGenerator.FindKernel("AtmosphericScatteringLUTWorld");

            // Main Shader
            m_PbrSkyMaterial = CoreUtils.CreateEngineMaterial(shaders.physicallyBasedSkyPS);
            m_PbrSkyMaterialProperties = new MaterialPropertyBlock();
        }

        public override void SetGlobalSkyData(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            UpdateGlobalConstantBuffer(cmd, builtinParams);
            if (m_PrecomputedData != null)
                m_PrecomputedData.BindGlobalBuffers(builtinParams.commandBuffer);
        }

        public static void SetDefaultGlobalSkyData(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AtmosphericScatteringLUT, CoreUtils.blackVolumeTexture);
        }

        public override void Cleanup()
        {
            m_DataFrameUpdate = -1;

            if (m_PrecomputedData != null)
            {
                m_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_LastPrecomputationParamHash = 0;
                m_PrecomputedData = null;
            }
            CoreUtils.Destroy(m_PbrSkyMaterial);

            if (!m_PrecomputationCache.HasAliveData() && m_CelestialBodyBuffer != null)
            {
                m_CelestialBodyBuffer.Dispose();
                m_CelestialBodyBuffer = null;
            }

            m_PbrSkyMaterialProperties.Clear();
            m_PbrSkyMaterialProperties = null;
        }

        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }

        static Vector2 ComputeExponentialInterpolationParams(float k)
        {
            if (k == 0) k = 1e-6f; // Avoid the numerical explosion around 0

            // Remap t: (exp(10 k t) - 1) / (exp(10 k) - 1) = exp(x t) y - y.
            float x = 10 * k;
            float y = 1 / (Mathf.Exp(x) - 1);

            return new Vector2(x, y);
        }

        void UpdateCelestialBodyBuffer(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            if (m_CelestialBodyBuffer == null)
            {
                int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CelestialBodyData));
                m_CelestialBodyBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, k_MaxCelestialBodies, stride);
                m_CelestialBodyData = new CelestialBodyData[k_MaxCelestialBodies];
            }

            if (builtinParams.frameIndex != m_DataFrameUpdate)
            {
                m_DataFrameUpdate = builtinParams.frameIndex;
                var directionalLights = HDLightRenderDatabase.instance.directionalLights;

                float exposure = 1.0f;

                uint lightCount = 0;
                foreach (var light in directionalLights)
                {
                    if (light.legacyLight.enabled && light.interactsWithSky && light.legacyLight.intensity != 0.0f)
                    {
                        FillCelestialBodyData(cmd, light, ref m_CelestialBodyData[lightCount++]);
                        exposure = Mathf.Max(light.legacyLight.intensity * -light.transform.forward.y, exposure);
                        if (lightCount >= k_MaxCelestialBodies) break;
                    }
                }

                uint bodyCount = lightCount;
                foreach (var light in directionalLights)
                {
                    if (bodyCount >= k_MaxCelestialBodies) break;
                    if (light.legacyLight.enabled && light.interactsWithSky && light.legacyLight.intensity == 0.0f)
                        FillCelestialBodyData(cmd, light, ref m_CelestialBodyData[bodyCount++]);
                }

                m_CelestialLightCount = lightCount;
                m_CelestialBodyCount = bodyCount;
                m_CelestialLightExposure = exposure;

                m_CelestialBodyBuffer.SetData(m_CelestialBodyData);
            }
        }

        // For both precomputation and runtime lighting passes.
        void UpdateGlobalConstantBuffer(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

            UpdateCelestialBodyBuffer(cmd, builtinParams);

            float R = builtinParams.hdCamera.planet.radius;
            float D = pbrSky.GetMaximumAltitude();
            float airH = pbrSky.GetAirScaleHeight();
            float aerH = pbrSky.GetAerosolScaleHeight();
            float aerA = pbrSky.aerosolAnisotropy.value;
            float ozoS = pbrSky.GetOzoneLayerMinimumAltitude();
            float ozoW = pbrSky.GetOzoneLayerWidth();
            float iMul = GetSkyIntensity(pbrSky, builtinParams.debugSettings);

            Vector2 expParams = ComputeExponentialInterpolationParams(pbrSky.horizonZenithShift.value);

            m_ConstantBuffer._AtmosphericDepth = D;
            m_ConstantBuffer._RcpAtmosphericDepth = 1.0f / D;
            m_ConstantBuffer._AtmosphericRadius = R + D;
            m_ConstantBuffer._AerosolAnisotropy = aerA;
            m_ConstantBuffer._AerosolPhasePartConstant = CornetteShanksPhasePartConstant(aerA);

            m_ConstantBuffer._AirDensityFalloff = 1.0f / airH;
            m_ConstantBuffer._AirScaleHeight = airH;
            m_ConstantBuffer._AerosolDensityFalloff = 1.0f / aerH;
            m_ConstantBuffer._AerosolScaleHeight = aerH;

            m_ConstantBuffer._AirSeaLevelExtinction = pbrSky.GetAirExtinctionCoefficient();
            m_ConstantBuffer._AerosolSeaLevelExtinction = pbrSky.GetAerosolExtinctionCoefficient();

            m_ConstantBuffer._AirSeaLevelScattering = pbrSky.GetAirScatteringCoefficient();
            m_ConstantBuffer._IntensityMultiplier = iMul;

            m_ConstantBuffer._AerosolSeaLevelScattering = pbrSky.GetAerosolScatteringCoefficient();
            m_ConstantBuffer._ColorSaturation = pbrSky.colorSaturation.value;

            m_ConstantBuffer._OzoneSeaLevelExtinction = pbrSky.GetOzoneExtinctionCoefficient();
            m_ConstantBuffer._OzoneScaleOffset = new Vector2(2.0f / ozoW, -2.0f * ozoS / ozoW - 1.0f);
            m_ConstantBuffer._OzoneLayerStart = R + ozoS;
            m_ConstantBuffer._OzoneLayerEnd = R + ozoS + ozoW;

            m_ConstantBuffer._GroundAlbedo_PlanetRadius = pbrSky.groundTint.value;
            m_ConstantBuffer._GroundAlbedo_PlanetRadius.w = R;
            m_ConstantBuffer._AlphaSaturation = pbrSky.alphaSaturation.value;

            m_ConstantBuffer._AlphaMultiplier = pbrSky.alphaMultiplier.value;

            Vector3 horizonTint = new Vector3(pbrSky.horizonTint.value.r, pbrSky.horizonTint.value.g, pbrSky.horizonTint.value.b);
            m_ConstantBuffer._HorizonTint = horizonTint;
            m_ConstantBuffer._HorizonZenithShiftPower = expParams.x;

            Vector3 zenithTint = new Vector3(pbrSky.zenithTint.value.r, pbrSky.zenithTint.value.g, pbrSky.zenithTint.value.b);
            m_ConstantBuffer._ZenithTint = zenithTint;
            m_ConstantBuffer._HorizonZenithShiftScale = expParams.y;

            m_ConstantBuffer._CelestialLightCount = m_CelestialLightCount;
            m_ConstantBuffer._CelestialBodyCount = m_CelestialBodyCount;
            m_ConstantBuffer._CelestialLightExposure = m_CelestialLightExposure;
            if (builtinParams.volumetricClouds != null)
                m_ConstantBuffer._VolumetricCloudsBottomAltitude = builtinParams.volumetricClouds.bottomAltitude.value;

            ConstantBuffer.PushGlobal(cmd, m_ConstantBuffer, s_ShaderVariablesPhysicallyBasedSkyID);
        }

        protected override bool Update(BuiltinSkyParameters builtinParams)
        {
            UpdateGlobalConstantBuffer(builtinParams.commandBuffer, builtinParams);
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

            int currPrecomputationParamHash = pbrSky.GetPrecomputationHashCode(builtinParams.hdCamera);
            if (currPrecomputationParamHash != m_LastPrecomputationParamHash)
            {
                if (m_LastPrecomputationParamHash != 0)
                    m_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_PrecomputedData = m_PrecomputationCache.Get(builtinParams, currPrecomputationParamHash, this);
                m_LastPrecomputationParamHash = currPrecomputationParamHash;
            }

            return false;
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;
            var renderingSpace = SupportSpace ? RenderingSpace.World : builtinParams.hdCamera.planet.renderingSpace;

            if (renderingSpace == RenderingSpace.Camera)
                m_PrecomputedData.RenderSkyViewLut(builtinParams.commandBuffer);
            if (!SupportSpace && pbrSky.atmosphericScattering.value && !renderForCubemap) // TODO: include fog & scattering in cubemaps
                m_PrecomputedData.RenderAtmosphericScatteringLut(builtinParams);

            m_PbrSkyMaterialProperties.Clear();

            m_PrecomputedData.BindGlobalBuffers(builtinParams.commandBuffer);
            m_PrecomputedData.BindBuffers(m_PbrSkyMaterialProperties);

            Unity.Mathematics.float4 upAltitude = HDRenderPipeline.currentPipeline.GetShaderVariablesGlobalCB()._PlanetUpAltitude;
            Vector3 cameraPosPS = builtinParams.worldSpaceCameraPos - builtinParams.hdCamera.planet.center;
            if (upAltitude.w < 1.0f) // Ensure camera is not below the ground
                cameraPosPS -= (upAltitude.w - 1.0f) * (Vector3)upAltitude.xyz;

            bool simpleEarthMode = pbrSky.type.value == PhysicallyBasedSkyModel.EarthSimple;
            bool customMaterial = pbrSky.renderingMode.value == PhysicallyBasedSky.RenderingMode.Material && pbrSky.material.value != null && pbrSky.material.overrideState;
            var material = customMaterial ? pbrSky.material.value : m_PbrSkyMaterial;

            // Common material properties
            m_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            m_PbrSkyMaterialProperties.SetVector(HDShaderIDs._PBRSkyCameraPosPS, cameraPosPS);
            m_PbrSkyMaterialProperties.SetInt(HDShaderIDs._RenderSunDisk, renderSunDisk ? 1 : 0);
            m_PbrSkyMaterialProperties.SetBuffer(HDShaderIDs._CelestialBodyDatas, m_CelestialBodyBuffer);

            CoreUtils.SetKeyword(material, "LOCAL_SKY", renderingSpace == RenderingSpace.World);

            if (!customMaterial)
            {
                // Precomputation is done, shading is next.
                Quaternion planetRotation = Quaternion.Euler(pbrSky.planetRotation.value.x,
                    pbrSky.planetRotation.value.y,
                    pbrSky.planetRotation.value.z);

                Quaternion spaceRotation = Quaternion.Euler(pbrSky.spaceRotation.value.x,
                    pbrSky.spaceRotation.value.y,
                    pbrSky.spaceRotation.value.z);

                var planetRotationMatrix = Matrix4x4.Rotate(planetRotation);
                planetRotationMatrix[0] *= -1;
                planetRotationMatrix[1] *= -1;
                planetRotationMatrix[2] *= -1;

                m_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PlanetRotation, planetRotationMatrix);
                m_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._SpaceRotation, Matrix4x4.Rotate(spaceRotation));

                int hasGroundAlbedoTexture = 0;

                if (pbrSky.groundColorTexture.value != null && !simpleEarthMode)
                {
                    hasGroundAlbedoTexture = 1;
                    m_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundAlbedoTexture, pbrSky.groundColorTexture.value);
                }
                m_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasGroundAlbedoTexture, hasGroundAlbedoTexture);

                int hasGroundEmissionTexture = 0;

                if (pbrSky.groundEmissionTexture.value != null && !simpleEarthMode)
                {
                    hasGroundEmissionTexture = 1;
                    m_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundEmissionTexture, pbrSky.groundEmissionTexture.value);
                    m_PbrSkyMaterialProperties.SetFloat(HDShaderIDs._GroundEmissionMultiplier, pbrSky.groundEmissionMultiplier.value);
                }
                m_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasGroundEmissionTexture, hasGroundEmissionTexture);

                int hasSpaceEmissionTexture = 0;

                if (pbrSky.spaceEmissionTexture.value != null && !simpleEarthMode)
                {
                    hasSpaceEmissionTexture = 1;
                    m_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._SpaceEmissionTexture, pbrSky.spaceEmissionTexture.value);
                    m_PbrSkyMaterialProperties.SetFloat(HDShaderIDs._SpaceEmissionMultiplier, pbrSky.spaceEmissionMultiplier.value);
                }
                m_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasSpaceEmissionTexture, hasSpaceEmissionTexture);
            }

            int pass = (renderForCubemap ? 0 : 1);
            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, material, m_PbrSkyMaterialProperties, pass);
        }

        internal static void FillCelestialBodyData(CommandBuffer cmd, HDAdditionalLightData additional, ref CelestialBodyData celestialBodyData)
        {
            var light = additional.legacyLight;
            var transform = light.transform;
            celestialBodyData.color = (Vector4)additional.EvaluateLightColor();

            // General
            celestialBodyData.forward = transform.forward;
            celestialBodyData.right = transform.right.normalized;
            celestialBodyData.up = transform.up.normalized;

            var angularDiameter = additional.diameterMultiplerMode ? additional.diameterMultiplier * additional.angularDiameter : additional.diameterOverride;
            celestialBodyData.angularRadius = angularDiameter * 0.5f * Mathf.Deg2Rad;
            celestialBodyData.distanceFromCamera = additional.distance;
            celestialBodyData.radius = Mathf.Tan(celestialBodyData.angularRadius) * celestialBodyData.distanceFromCamera;

            celestialBodyData.surfaceColor = (Vector4)additional.surfaceTint.linear;
            celestialBodyData.earthshine = additional.earthshine * 0.01f; // earth reflects about 0.01% of sun light
            celestialBodyData.shadowIndex = additional.shadowIndex;

            if (additional.surfaceTexture == null)
                celestialBodyData.surfaceTextureScaleOffset = Vector4.zero;
            else
                celestialBodyData.surfaceTextureScaleOffset = HDRenderPipeline.currentPipeline.m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, additional.surfaceTexture);

            // Flare
            celestialBodyData.flareSize = Mathf.Max(additional.flareSize * Mathf.Deg2Rad, 5.960464478e-8f);
            celestialBodyData.flareFalloff = additional.flareFalloff;

            celestialBodyData.flareCosInner = Mathf.Cos(celestialBodyData.angularRadius);
            celestialBodyData.flareCosOuter = Mathf.Cos(celestialBodyData.angularRadius + celestialBodyData.flareSize);

            celestialBodyData.flareColor = additional.flareMultiplier * (Vector4)additional.flareTint.linear;

            // Shading
            var source = additional.celestialBodyShadingSource;
            if (source == ShadingSource.Emission)
            {
                celestialBodyData.type = 0;
                float rcpSolidAngle = 1.0f / (Mathf.PI * 2.0f * (1 - celestialBodyData.flareCosInner));
                celestialBodyData.surfaceColor *= rcpSolidAngle;
                celestialBodyData.flareColor *= rcpSolidAngle;

                celestialBodyData.surfaceColor = Vector4.Scale(celestialBodyData.color, celestialBodyData.surfaceColor);
                celestialBodyData.flareColor = Vector4.Scale(celestialBodyData.color, celestialBodyData.flareColor);
            }
            else
            {
                Color sunColor;
                if (source == ShadingSource.Manual)
                {
                    var rotation = Quaternion.AngleAxis(additional.moonPhaseRotation, celestialBodyData.forward);
                    var remap = Quaternion.FromToRotation(Vector3.right, celestialBodyData.forward);
                    float phase = additional.moonPhase * 2.0f * Mathf.PI;

                    sunColor = additional.sunColor * additional.sunIntensity;
                    celestialBodyData.sunDirection = rotation * remap * new Vector3(Mathf.Cos(phase), 0, Mathf.Sin(phase));
                }
                else
                {
                    var lightSource = additional.sunLightOverride;
                    if (lightSource == null || lightSource == additional.legacyLight || lightSource.type != LightType.Directional)
                        lightSource = FindSunLight(additional.legacyLight);
                    sunColor = lightSource != null ? (Vector4)lightSource.GetComponent<HDAdditionalLightData>().EvaluateLightColor() : Vector4.zero;
                    celestialBodyData.sunDirection = lightSource != null ? lightSource.transform.forward : Vector3.forward;
                }

                celestialBodyData.type = 1;
                celestialBodyData.surfaceColor = Vector4.Scale(sunColor, celestialBodyData.surfaceColor);
                celestialBodyData.flareColor = Vector4.Scale(sunColor, celestialBodyData.flareColor);
            }
        }

        static Light FindSunLight(Light toExclude)
        {
            Light result = null;
            float currentMax = 0.0f;
            foreach (var light in HDLightRenderDatabase.instance.directionalLights)
            {
                var legacyLight = light.legacyLight;
                if (legacyLight != toExclude && legacyLight.intensity > currentMax)
                {
                    currentMax = legacyLight.intensity;
                    result = legacyLight;
                }
            }
            return result;
        }
    }
}

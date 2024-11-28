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

            public PrecomputationData Get(BuiltinSkyParameters builtinParams, int hash)
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
                    result.data.Allocate(builtinParams);
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

            bool IsWorldSpace() => m_InScatteredRadianceTables != null;

            RTHandle AllocateGroundIrradianceTable()
            {
                var table = RTHandles.Alloc((int)PbrSkyConfig.GroundIrradianceTableSize, 1,
                    colorFormat: s_ColorFormat,
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
                    colorFormat: s_ColorFormat,
                    enableRandomWrite: true,
                    name: string.Format("InScatteredRadianceTable{0}", index));

                Debug.Assert(table != null);

                return table;
            }

            public void Allocate(BuiltinSkyParameters builtinParams)
            {
                var cmd = builtinParams.commandBuffer;
                var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

                m_MultiScatteringLut = RTHandles.Alloc(
                    (int)PbrSkyConfig.MultiScatteringLutWidth,
                    (int)PbrSkyConfig.MultiScatteringLutHeight,
                    colorFormat: s_ColorFormat,
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
                        colorFormat: s_ColorFormat,
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
                        colorFormat: s_ColorFormat,
                        enableRandomWrite: true,
                        name: "AtmosphericScatteringLUT");
                }
            }

            public void Release()
            {
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
                cmd.SetComputeTextureParam(s_SkyLUTGenerator, s_MultiScatteringKernel, HDShaderIDs._MultiScatteringLUT_RW, m_MultiScatteringLut);

                cmd.DispatchCompute(s_SkyLUTGenerator, s_MultiScatteringKernel,
                    (int)PbrSkyConfig.MultiScatteringLutWidth,
                    (int)PbrSkyConfig.MultiScatteringLutHeight,
                    1);
            }

            void PrecomputeTables(CommandBuffer cmd)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.InScatteredRadiancePrecomputation)))
                {
                    // Multiple scattering LUT
                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._AirSingleScatteringTable, m_InScatteredRadianceTables[0]);
                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._AerosolSingleScatteringTable, m_InScatteredRadianceTables[1]);
                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._MultipleScatteringTable, m_InScatteredRadianceTables[2]);
                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, 0, HDShaderIDs._MultiScatteringLUT, m_MultiScatteringLut);

                    // Emulate a 4D dispatch with a "deep" 3D dispatch.
                    cmd.DispatchCompute(s_InScatteredRadiancePrecomputationCS, 0, (int)PbrSkyConfig.InScatteredRadianceTableSizeX / 4,
                        (int)PbrSkyConfig.InScatteredRadianceTableSizeY / 4,
                        (int)PbrSkyConfig.InScatteredRadianceTableSizeZ / 4 *
                        (int)PbrSkyConfig.InScatteredRadianceTableSizeW);

                    // Ground irradiance LUT
                    cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                    cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                    cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[2]);

                    cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, 0, HDShaderIDs._GroundIrradianceTable, m_GroundIrradianceTable);

                    cmd.DispatchCompute(s_GroundIrradiancePrecomputationCS, 0, (int)PbrSkyConfig.GroundIrradianceTableSize / 64, 1, 1);
                }
            }

            static internal float CastFloat(float value, int size)
            {
                return (int)(value * size) / (float)size;
            }

            // Computes hash code of light parameters used during sky view lut precomputation
            static int GetLightsHash()
            {
                int hash = 13;
                for (int i = 0; i < s_CelestialLightCount; i++)
                {
                    ref var data = ref s_CelestialBodyData[i];
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

                cmd.SetComputeTextureParam(s_SkyLUTGenerator, s_SkyViewKernel, HDShaderIDs._MultiScatteringLUT, m_MultiScatteringLut);
                cmd.SetComputeTextureParam(s_SkyLUTGenerator, s_SkyViewKernel, HDShaderIDs._SkyViewLUT_RW, m_SkyViewLut);
                cmd.SetComputeBufferParam(s_SkyLUTGenerator, s_SkyViewKernel, HDShaderIDs._CelestialBodyDatas, s_CelestialBodyBuffer);

                cmd.DispatchCompute(s_SkyLUTGenerator, s_SkyViewKernel,
                    (int)PbrSkyConfig.SkyViewLutWidth / 8,
                    (int)PbrSkyConfig.SkyViewLutHeight / 8,
                    1);
            }

            internal void RenderAtmosphericScatteringLut(BuiltinSkyParameters builtinParams)
            {
                var cmd = builtinParams.commandBuffer;
                cmd.SetComputeMatrixParam(s_SkyLUTGenerator, HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                int kernel = IsWorldSpace() ? s_AtmosphericScatteringKernelWorld : s_AtmosphericScatteringKernelCamera;

                cmd.SetComputeTextureParam(s_SkyLUTGenerator, kernel, HDShaderIDs._MultiScatteringLUT, m_MultiScatteringLut);
                cmd.SetComputeTextureParam(s_SkyLUTGenerator, kernel, HDShaderIDs._AtmosphericScatteringLUT_RW, m_AtmosphericScatteringLut);
                cmd.SetComputeBufferParam(s_SkyLUTGenerator, kernel, HDShaderIDs._CelestialBodyDatas, s_CelestialBodyBuffer);

                cmd.DispatchCompute(s_SkyLUTGenerator, kernel,
                    (int)PbrSkyConfig.AtmosphericScatteringLutWidth,
                    (int)PbrSkyConfig.AtmosphericScatteringLutHeight,
                    1);

                // Perform a blur pass on the buffer to reduce resolution artefacts
                cmd.SetComputeTextureParam(s_SkyLUTGenerator, s_AtmosphericScatteringBlurKernel, HDShaderIDs._AtmosphericScatteringLUT_RW, m_AtmosphericScatteringLut);

                cmd.DispatchCompute(s_SkyLUTGenerator, s_AtmosphericScatteringBlurKernel,
                    1,
                    1,
                    (int)PbrSkyConfig.AtmosphericScatteringLutDepth);
            }

            public void BindGlobalBuffers(CommandBuffer cmd)
            {
                cmd.SetGlobalBuffer(HDShaderIDs._CelestialBodyDatas, s_CelestialBodyBuffer);

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

        // Store the hash of the parameters each time precomputation is done.
        // If the hash does not match, we must recompute our data.
        int m_LastPrecomputationParamHash;

        // Precomputed data below.
        PrecomputationData m_PrecomputedData;

        Material m_PbrSkyMaterial;
        static MaterialPropertyBlock s_PbrSkyMaterialProperties;

        static PrecomputationCache s_PrecomputationCache = new PrecomputationCache();

        const int k_MaxCelestialBodies = 16;
        static GraphicsBuffer s_CelestialBodyBuffer;
        static CelestialBodyData[] s_CelestialBodyData;
        static int s_DataFrameUpdate = -1;
        static uint s_CelestialLightCount;
        static uint s_CelestialBodyCount;
        static float s_CelestialLightExposure;

        ShaderVariablesPhysicallyBasedSky m_ConstantBuffer;
        int m_ShaderVariablesPhysicallyBasedSkyID = Shader.PropertyToID("ShaderVariablesPhysicallyBasedSky");
        static GraphicsFormat s_ColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;

        // Common resourcse
        static ComputeShader s_SkyLUTGenerator;
        static int s_MultiScatteringKernel, s_AtmosphericScatteringBlurKernel;

        // Resources for world space sky
        static ComputeShader s_GroundIrradiancePrecomputationCS;
        static ComputeShader s_InScatteredRadiancePrecomputationCS;
        static int s_AtmosphericScatteringKernelWorld;

        // Resources for camera space sky
        static int s_SkyViewKernel, s_AtmosphericScatteringKernelCamera;

        public override void Build()
        {
            var shaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdPipeline != null)
                s_ColorFormat = hdPipeline.GetColorBufferFormat();

            // Common
            s_SkyLUTGenerator = shaders.skyLUTGenerator;
            s_MultiScatteringKernel = s_SkyLUTGenerator.FindKernel("MultiScatteringLUT");
            s_AtmosphericScatteringBlurKernel = s_SkyLUTGenerator.FindKernel("AtmosphericScatteringBlur");

            // Camera space sky
            s_SkyViewKernel = s_SkyLUTGenerator.FindKernel("SkyViewLUT");
            s_AtmosphericScatteringKernelCamera = s_SkyLUTGenerator.FindKernel("AtmosphericScatteringLUTCamera");

            // World space sky
            s_GroundIrradiancePrecomputationCS = shaders.groundIrradiancePrecomputationCS;
            s_InScatteredRadiancePrecomputationCS = shaders.inScatteredRadiancePrecomputationCS;
            s_AtmosphericScatteringKernelWorld = s_SkyLUTGenerator.FindKernel("AtmosphericScatteringLUTWorld");

            // Main Shader
            m_PbrSkyMaterial = CoreUtils.CreateEngineMaterial(shaders.physicallyBasedSkyPS);
            s_PbrSkyMaterialProperties = new MaterialPropertyBlock();
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
            if (m_PrecomputedData != null)
            {
                s_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_LastPrecomputationParamHash = 0;
                m_PrecomputedData = null;
            }
            CoreUtils.Destroy(m_PbrSkyMaterial);

            if (!s_PrecomputationCache.HasAliveData() && s_CelestialBodyBuffer != null)
            {
                s_CelestialBodyBuffer.Dispose();
                s_CelestialBodyBuffer = null;
            }
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
            if (s_CelestialBodyBuffer == null)
            {
                int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CelestialBodyData));
                s_CelestialBodyBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, k_MaxCelestialBodies, stride);
                s_CelestialBodyData = new CelestialBodyData[k_MaxCelestialBodies];
            }

            if (builtinParams.frameIndex != s_DataFrameUpdate)
            {
                s_DataFrameUpdate = builtinParams.frameIndex;
                var directionalLights = HDLightRenderDatabase.instance.directionalLights;

                float exposure = 1.0f;

                uint lightCount = 0;
                foreach (var light in directionalLights)
                {
                    if (light.legacyLight.enabled && light.interactsWithSky && light.legacyLight.intensity != 0.0f)
                    {
                        FillCelestialBodyData(cmd, light, ref s_CelestialBodyData[lightCount++]);
                        exposure = Mathf.Max(light.legacyLight.intensity * -light.transform.forward.y, exposure);
                        if (lightCount >= k_MaxCelestialBodies) break;
                    }
                }

                uint bodyCount = lightCount;
                foreach (var light in directionalLights)
                {
                    if (bodyCount >= k_MaxCelestialBodies) break;
                    if (light.legacyLight.enabled && light.interactsWithSky && light.legacyLight.intensity == 0.0f)
                        FillCelestialBodyData(cmd, light, ref s_CelestialBodyData[bodyCount++]);
                }

                s_CelestialLightCount = lightCount;
                s_CelestialBodyCount = bodyCount;
                s_CelestialLightExposure = exposure;

                s_CelestialBodyBuffer.SetData(s_CelestialBodyData);
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

            m_ConstantBuffer._CelestialLightCount = s_CelestialLightCount;
            m_ConstantBuffer._CelestialBodyCount = s_CelestialBodyCount;
            m_ConstantBuffer._CelestialLightExposure = s_CelestialLightExposure;
            if (builtinParams.volumetricClouds != null)
                m_ConstantBuffer._VolumetricCloudsBottomAltitude = builtinParams.volumetricClouds.bottomAltitude.value;

            ConstantBuffer.PushGlobal(cmd, m_ConstantBuffer, m_ShaderVariablesPhysicallyBasedSkyID);
        }

        protected override bool Update(BuiltinSkyParameters builtinParams)
        {
            UpdateGlobalConstantBuffer(builtinParams.commandBuffer, builtinParams);
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

            int currPrecomputationParamHash = pbrSky.GetPrecomputationHashCode(builtinParams.hdCamera);
            if (currPrecomputationParamHash != m_LastPrecomputationParamHash)
            {
                if (m_LastPrecomputationParamHash != 0)
                    s_PrecomputationCache.Release(m_LastPrecomputationParamHash);
                m_PrecomputedData = s_PrecomputationCache.Get(builtinParams, currPrecomputationParamHash);
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

            m_PrecomputedData.BindGlobalBuffers(builtinParams.commandBuffer);
            m_PrecomputedData.BindBuffers(s_PbrSkyMaterialProperties);

            Unity.Mathematics.float4 upAltitude = HDRenderPipeline.currentPipeline.GetShaderVariablesGlobalCB()._PlanetUpAltitude;
            Vector3 cameraPosPS = builtinParams.worldSpaceCameraPos - builtinParams.hdCamera.planet.center;
            if (upAltitude.w < 1.0f) // Ensure camera is not below the ground
                cameraPosPS -= (upAltitude.w - 1.0f) * (Vector3)upAltitude.xyz;

            bool simpleEarthMode = pbrSky.type.value == PhysicallyBasedSkyModel.EarthSimple;
            bool customMaterial = pbrSky.renderingMode.value == PhysicallyBasedSky.RenderingMode.Material && pbrSky.material.value != null && pbrSky.material.overrideState;
            var material = customMaterial ? pbrSky.material.value : m_PbrSkyMaterial;

            // Common material properties
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            s_PbrSkyMaterialProperties.SetVector(HDShaderIDs._PBRSkyCameraPosPS, cameraPosPS);
            s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._RenderSunDisk, renderSunDisk ? 1 : 0);
            s_PbrSkyMaterialProperties.SetBuffer(HDShaderIDs._CelestialBodyDatas, s_CelestialBodyBuffer);

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

                s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PlanetRotation, planetRotationMatrix);
                s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._SpaceRotation, Matrix4x4.Rotate(spaceRotation));

                int hasGroundAlbedoTexture = 0;

                if (pbrSky.groundColorTexture.value != null && !simpleEarthMode)
                {
                    hasGroundAlbedoTexture = 1;
                    s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundAlbedoTexture, pbrSky.groundColorTexture.value);
                }
                s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasGroundAlbedoTexture, hasGroundAlbedoTexture);

                int hasGroundEmissionTexture = 0;

                if (pbrSky.groundEmissionTexture.value != null && !simpleEarthMode)
                {
                    hasGroundEmissionTexture = 1;
                    s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundEmissionTexture, pbrSky.groundEmissionTexture.value);
                    s_PbrSkyMaterialProperties.SetFloat(HDShaderIDs._GroundEmissionMultiplier, pbrSky.groundEmissionMultiplier.value);
                }
                s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasGroundEmissionTexture, hasGroundEmissionTexture);

                int hasSpaceEmissionTexture = 0;

                if (pbrSky.spaceEmissionTexture.value != null && !simpleEarthMode)
                {
                    hasSpaceEmissionTexture = 1;
                    s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._SpaceEmissionTexture, pbrSky.spaceEmissionTexture.value);
                    s_PbrSkyMaterialProperties.SetFloat(HDShaderIDs._SpaceEmissionMultiplier, pbrSky.spaceEmissionMultiplier.value);
                }
                s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasSpaceEmissionTexture, hasSpaceEmissionTexture);
            }

            int pass = (renderForCubemap ? 0 : 1);
            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, material, s_PbrSkyMaterialProperties, pass);
        }

        static internal void FillCelestialBodyData(CommandBuffer cmd, HDAdditionalLightData additional, ref CelestialBodyData celestialBodyData)
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
                if (light != toExclude && light.legacyLight.intensity > currentMax)
                {
                    currentMax = light.legacyLight.intensity;
                    result = light.legacyLight;
                }
            }
            return result;
        }
    }
}

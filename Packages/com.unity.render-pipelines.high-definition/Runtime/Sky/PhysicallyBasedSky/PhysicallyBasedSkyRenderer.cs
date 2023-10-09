using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

using ShadingSource = UnityEngine.Rendering.HighDefinition.HDAdditionalLightData.CelestialBodyShadingSource;

namespace UnityEngine.Rendering.HighDefinition
{
    class PhysicallyBasedSkyRenderer : SkyRenderer
    {
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

            public PrecomputationData Get(int hash)
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
                    result.data.Allocate();
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
            // We compute at most one bounce per frame for perf reasons.
            // We need to store the frame index because more than one render can happen during a frame (cubemap update + regular rendering).
            int m_LastPrecomputedBounce;
            int m_LastFrameComputation;

            RTHandle[] m_GroundIrradianceTables;    // All orders, one order
            RTHandle[] m_InScatteredRadianceTables; // Air SS, Aerosol SS, Atmosphere MS, Atmosphere one order, Temp

            RTHandle AllocateGroundIrradianceTable(int index)
            {
                var table = RTHandles.Alloc((int)PbrSkyConfig.GroundIrradianceTableSize, 1,
                    colorFormat: s_ColorFormat,
                    enableRandomWrite: true,
                    name: string.Format("GroundIrradianceTable{0}", index));

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

            public void Allocate()
            {
                m_LastFrameComputation = -1;
                m_LastPrecomputedBounce = 0;

                // No temp tables.
                m_GroundIrradianceTables = new RTHandle[2];
                m_GroundIrradianceTables[0] = AllocateGroundIrradianceTable(0);

                m_InScatteredRadianceTables = new RTHandle[5];
                m_InScatteredRadianceTables[0] = AllocateInScatteredRadianceTable(0);
                m_InScatteredRadianceTables[1] = AllocateInScatteredRadianceTable(1);
                m_InScatteredRadianceTables[2] = AllocateInScatteredRadianceTable(2);
            }

            public void Release()
            {
                RTHandles.Release(m_GroundIrradianceTables[0]); m_GroundIrradianceTables[0] = null;
                RTHandles.Release(m_GroundIrradianceTables[1]); m_GroundIrradianceTables[1] = null;
                RTHandles.Release(m_InScatteredRadianceTables[0]); m_InScatteredRadianceTables[0] = null;
                RTHandles.Release(m_InScatteredRadianceTables[1]); m_InScatteredRadianceTables[1] = null;
                RTHandles.Release(m_InScatteredRadianceTables[2]); m_InScatteredRadianceTables[2] = null;
                RTHandles.Release(m_InScatteredRadianceTables[3]); m_InScatteredRadianceTables[3] = null;
                RTHandles.Release(m_InScatteredRadianceTables[4]); m_InScatteredRadianceTables[4] = null;
            }

            void PrecomputeTables(CommandBuffer cmd)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.InScatteredRadiancePrecomputation)))
                {
                    int order = m_LastPrecomputedBounce + 1;
                    {
                        // For efficiency reasons, multiple scattering is computed in 2 passes:
                        // 1. Gather the in-scattered radiance over the entire sphere of directions.
                        // 2. Accumulate the in-scattered radiance along the ray.
                        // Single scattering performs both steps during the same pass.

                        int firstPass = Math.Min(order - 1, 2);
                        int accumPass = 3;
                        int numPasses = Math.Min(order, 2);

                        for (int i = 0; i < numPasses; i++)
                        {
                            int pass = (i == 0) ? firstPass : accumPass;

                            switch (pass)
                            {
                                case 0:
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AirSingleScatteringTable, m_InScatteredRadianceTables[0]);
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AerosolSingleScatteringTable, m_InScatteredRadianceTables[1]);
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable, m_InScatteredRadianceTables[2]); // MS orders
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTableOrder, m_InScatteredRadianceTables[3]); // One order
                                    break;
                                case 1:
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._GroundIrradianceTexture, m_GroundIrradianceTables[1]);    // One order
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable, m_InScatteredRadianceTables[4]); // Temp
                                    break;
                                case 2:
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[3]); // One order
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._GroundIrradianceTexture, m_GroundIrradianceTables[1]);    // One order
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable, m_InScatteredRadianceTables[4]); // Temp
                                    break;
                                case 3:
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[4]); // Temp
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTableOrder, m_InScatteredRadianceTables[3]); // One order
                                    cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable, m_InScatteredRadianceTables[2]); // MS orders
                                    break;
                                default:
                                    Debug.Assert(false);
                                    break;
                            }

                            // Re-illuminate the sky with each bounce.
                            // Emulate a 4D dispatch with a "deep" 3D dispatch.
                            cmd.DispatchCompute(s_InScatteredRadiancePrecomputationCS, pass, (int)PbrSkyConfig.InScatteredRadianceTableSizeX / 4,
                                (int)PbrSkyConfig.InScatteredRadianceTableSizeY / 4,
                                (int)PbrSkyConfig.InScatteredRadianceTableSizeZ / 4 *
                                (int)PbrSkyConfig.InScatteredRadianceTableSizeW);
                        }

                        {
                            // Used by all passes.
                            cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._GroundIrradianceTable, m_GroundIrradianceTables[0]); // All orders
                            cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._GroundIrradianceTableOrder, m_GroundIrradianceTables[1]); // One order
                        }

                        switch (firstPass)
                        {
                            case 0:
                                break;
                            case 1:
                                cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                                cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                                break;
                            case 2:
                                cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[3]); // One order
                                break;
                            default:
                                Debug.Assert(false);
                                break;
                        }

                        // Re-illuminate the ground with each bounce.
                        cmd.DispatchCompute(s_GroundIrradiancePrecomputationCS, firstPass, (int)PbrSkyConfig.GroundIrradianceTableSize / 64, 1, 1);
                    }
                }
            }

            public void BindGlobalBuffers(CommandBuffer cmd)
            {
                // TODO: ground irradiance table? Volume SH? Something else?
                if (m_LastPrecomputedBounce > 0)
                {
                    cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                    cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                    cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[2]);
                }
                else
                {
                    cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                    cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                    cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, CoreUtils.blackVolumeTexture);
                }
            }

            public void BindBuffers(CommandBuffer cmd, MaterialPropertyBlock mpb)
            {
                if (m_LastPrecomputedBounce != 0)
                {
                    mpb.SetTexture(HDShaderIDs._GroundIrradianceTexture, m_GroundIrradianceTables[0]);
                    mpb.SetTexture(HDShaderIDs._AirSingleScatteringTexture, m_InScatteredRadianceTables[0]);
                    mpb.SetTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                    mpb.SetTexture(HDShaderIDs._MultipleScatteringTexture, m_InScatteredRadianceTables[2]);
                }
                else
                {
                    mpb.SetTexture(HDShaderIDs._GroundIrradianceTexture, Texture2D.blackTexture);
                    mpb.SetTexture(HDShaderIDs._AirSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                    mpb.SetTexture(HDShaderIDs._AerosolSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                    mpb.SetTexture(HDShaderIDs._MultipleScatteringTexture, CoreUtils.blackVolumeTexture);
                }
            }

            public bool Update(BuiltinSkyParameters builtinParams, PhysicallyBasedSky pbrSky)
            {
                if (builtinParams.frameIndex <= m_LastFrameComputation)
                    return false;

                m_LastFrameComputation = builtinParams.frameIndex;

                if (m_LastPrecomputedBounce == 0)
                {
                    // Allocate temp tables if needed
                    if (m_GroundIrradianceTables[1] == null)
                    {
                        m_GroundIrradianceTables[1] = AllocateGroundIrradianceTable(1);
                    }

                    if (m_InScatteredRadianceTables[3] == null)
                    {
                        m_InScatteredRadianceTables[3] = AllocateInScatteredRadianceTable(3);
                    }

                    if (m_InScatteredRadianceTables[4] == null)
                    {
                        m_InScatteredRadianceTables[4] = AllocateInScatteredRadianceTable(4);
                    }
                }

                if (m_LastPrecomputedBounce == pbrSky.numberOfBounces.value)
                {
                    // Free temp tables.
                    // This is a deferred release (one frame late)!
                    RTHandles.Release(m_GroundIrradianceTables[1]);
                    RTHandles.Release(m_InScatteredRadianceTables[3]);
                    RTHandles.Release(m_InScatteredRadianceTables[4]);
                    m_GroundIrradianceTables[1] = null;
                    m_InScatteredRadianceTables[3] = null;
                    m_InScatteredRadianceTables[4] = null;
                }

                if (m_LastPrecomputedBounce < pbrSky.numberOfBounces.value)
                {
                    PrecomputeTables(builtinParams.commandBuffer);
                    m_LastPrecomputedBounce++;

                    // If the sky is realtime, an upcoming update will update the sky lighting. Otherwise we need to force an update.
                    return builtinParams.skySettings.updateMode != EnvironmentUpdateMode.Realtime;
                }

                return false;
            }
        }

        // Store the hash of the parameters each time precomputation is done.
        // If the hash does not match, we must recompute our data.
        int m_LastPrecomputationParamHash;

        // Precomputed data below.
        PrecomputationData m_PrecomputedData;

        static ComputeShader s_GroundIrradiancePrecomputationCS;
        static ComputeShader s_InScatteredRadiancePrecomputationCS;
        Material m_PbrSkyMaterial;
        static MaterialPropertyBlock s_PbrSkyMaterialProperties;

        static PrecomputationCache s_PrecomputaionCache = new PrecomputationCache();

        static GraphicsBuffer s_CelestialBodyBuffer;
        static CelestialBodyData[] s_CelestialBodyData;
        static int s_DataFrameUpdate = -1;
        static uint s_CelestialLightCount;
        static uint s_CelestialBodyCount;

        ShaderVariablesPhysicallyBasedSky m_ConstantBuffer;
        int m_ShaderVariablesPhysicallyBasedSkyID = Shader.PropertyToID("ShaderVariablesPhysicallyBasedSky");

        static GraphicsFormat s_ColorFormat = GraphicsFormat.R16G16B16A16_SFloat;


        public PhysicallyBasedSkyRenderer()
        {
        }

        public override void Build()
        {
            var hdrpResources = HDRenderPipelineGlobalSettings.instance.renderPipelineResources;

            // Shaders
            s_GroundIrradiancePrecomputationCS = hdrpResources.shaders.groundIrradiancePrecomputationCS;
            s_InScatteredRadiancePrecomputationCS = hdrpResources.shaders.inScatteredRadiancePrecomputationCS;
            s_PbrSkyMaterialProperties = new MaterialPropertyBlock();

            m_PbrSkyMaterial = CoreUtils.CreateEngineMaterial(hdrpResources.shaders.physicallyBasedSkyPS);

            Debug.Assert(s_GroundIrradiancePrecomputationCS != null);
            Debug.Assert(s_InScatteredRadiancePrecomputationCS != null);
        }

        public override void SetGlobalSkyData(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            UpdateGlobalConstantBuffer(cmd, builtinParams);
            if (m_PrecomputedData != null)
                m_PrecomputedData.BindGlobalBuffers(builtinParams.commandBuffer);
        }

        public override void Cleanup()
        {
            if (m_PrecomputedData != null)
            {
                s_PrecomputaionCache.Release(m_LastPrecomputationParamHash);
                m_LastPrecomputationParamHash = 0;
                m_PrecomputedData = null;
            }
            CoreUtils.Destroy(m_PbrSkyMaterial);

            if (!s_PrecomputaionCache.HasAliveData())
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
                int k_MaxCelestialBodies = 16;
                int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CelestialBodyData));
                s_CelestialBodyBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, k_MaxCelestialBodies, stride);
                s_CelestialBodyData = new CelestialBodyData[k_MaxCelestialBodies];
            }

            if (builtinParams.frameIndex != s_DataFrameUpdate)
            {
                s_DataFrameUpdate = builtinParams.frameIndex;
                var directionalLights = HDLightRenderDatabase.instance.directionalLights;

                uint lightCount = 0;
                foreach (var light in directionalLights)
                {
                    if (light.legacyLight.enabled && light.interactsWithSky && light.intensity != 0.0f)
                        FillCelestialBodyData(cmd, light, ref s_CelestialBodyData[lightCount++]);
                }

                uint bodyCount = lightCount;
                foreach (var light in directionalLights)
                {
                    if (light.legacyLight.enabled && light.interactsWithSky && light.intensity == 0.0f)
                        FillCelestialBodyData(cmd, light, ref s_CelestialBodyData[bodyCount++]);
                }

                s_CelestialLightCount = lightCount;
                s_CelestialBodyCount = bodyCount;

                s_CelestialBodyBuffer.SetData(s_CelestialBodyData);
            }
        }

        // For both precomputation and runtime lighting passes.
        void UpdateGlobalConstantBuffer(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

            UpdateCelestialBodyBuffer(cmd, builtinParams);

            float R = pbrSky.GetPlanetaryRadius();
            float D = pbrSky.GetMaximumAltitude();
            float airH = pbrSky.GetAirScaleHeight();
            float aerH = pbrSky.GetAerosolScaleHeight();
            float aerA = pbrSky.GetAerosolAnisotropy();
            float iMul = GetSkyIntensity(pbrSky, builtinParams.debugSettings);

            Vector2 expParams = ComputeExponentialInterpolationParams(pbrSky.horizonZenithShift.value);

            m_ConstantBuffer._PlanetaryRadius = R;
            m_ConstantBuffer._RcpPlanetaryRadius = 1.0f / R;
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

            Vector3 groundAlbedo = new Vector3(pbrSky.groundTint.value.r, pbrSky.groundTint.value.g, pbrSky.groundTint.value.b);
            m_ConstantBuffer._GroundAlbedo = groundAlbedo;
            m_ConstantBuffer._AlphaSaturation = pbrSky.alphaSaturation.value;

            m_ConstantBuffer._PlanetCenterPosition = pbrSky.GetPlanetCenterPosition(builtinParams.worldSpaceCameraPos);
            m_ConstantBuffer._AlphaMultiplier = pbrSky.alphaMultiplier.value;

            Vector3 horizonTint = new Vector3(pbrSky.horizonTint.value.r, pbrSky.horizonTint.value.g, pbrSky.horizonTint.value.b);
            m_ConstantBuffer._HorizonTint = horizonTint;
            m_ConstantBuffer._HorizonZenithShiftPower = expParams.x;

            Vector3 zenithTint = new Vector3(pbrSky.zenithTint.value.r, pbrSky.zenithTint.value.g, pbrSky.zenithTint.value.b);
            m_ConstantBuffer._ZenithTint = zenithTint;
            m_ConstantBuffer._HorizonZenithShiftScale = expParams.y;

            m_ConstantBuffer._CelestialLightCount = s_CelestialLightCount;
            m_ConstantBuffer._CelestialBodyCount = s_CelestialBodyCount;

            ConstantBuffer.PushGlobal(cmd, m_ConstantBuffer, m_ShaderVariablesPhysicallyBasedSkyID);
        }

        protected override bool Update(BuiltinSkyParameters builtinParams)
        {
            UpdateGlobalConstantBuffer(builtinParams.commandBuffer, builtinParams);
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

            int currPrecomputationParamHash = pbrSky.GetPrecomputationHashCode();
            if (currPrecomputationParamHash != m_LastPrecomputationParamHash)
            {
                if (m_LastPrecomputationParamHash != 0)
                    s_PrecomputaionCache.Release(m_LastPrecomputationParamHash);
                m_PrecomputedData = s_PrecomputaionCache.Get(currPrecomputationParamHash);
                m_LastPrecomputationParamHash = currPrecomputationParamHash;
            }

            return m_PrecomputedData.Update(builtinParams, pbrSky);
        }

        // 'renderSunDisk' parameter is not supported.
        // Users should instead create an emissive (or lit) mesh for every relevant light source
        // (to support multiple stars in space, moons with moon phases, etc).
        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var pbrSky = builtinParams.skySettings as PhysicallyBasedSky;

            // TODO: the following expression is somewhat inefficient, but good enough for now.
            Vector3 cameraPos = builtinParams.worldSpaceCameraPos;
            Vector3 planetCenter = pbrSky.GetPlanetCenterPosition(cameraPos);
            float R = pbrSky.GetPlanetaryRadius();

            Vector3 cameraPosPS = cameraPos - planetCenter;
            float r = cameraPosPS.magnitude;
            cameraPosPS = Mathf.Max(R / r, 1.0f) * cameraPosPS;

            bool simpleEarthMode = pbrSky.type.value == PhysicallyBasedSkyModel.EarthSimple;
            bool customMaterial = pbrSky.renderingMode.value == PhysicallyBasedSky.RenderingMode.Material && pbrSky.material.value != null;
            var material = customMaterial ? pbrSky.material.value : m_PbrSkyMaterial;

            CommandBuffer cmd = builtinParams.commandBuffer;

            // Common material properties
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            s_PbrSkyMaterialProperties.SetVector(HDShaderIDs._PBRSkyCameraPosPS, cameraPosPS);
            s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._RenderSunDisk, renderSunDisk ? 1 : 0);
            s_PbrSkyMaterialProperties.SetBuffer(HDShaderIDs._CelestialBodyDatas, s_CelestialBodyBuffer);

            m_PrecomputedData.BindBuffers(cmd, s_PbrSkyMaterialProperties);

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
            celestialBodyData.color = (Vector4)LightUtils.EvaluateLightColor(light, additional);

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
                    sunColor = lightSource != null ? (Vector4)LightUtils.EvaluateLightColor(lightSource, lightSource.GetComponent<HDAdditionalLightData>()) : Vector4.zero;
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
                if (light != toExclude && light.intensity > currentMax)
                {
                    currentMax = light.intensity;
                    result = light.legacyLight;
                }
            }
            return result;
        }
    }
}

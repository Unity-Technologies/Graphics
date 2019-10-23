using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class PhysicallyBasedSkyRenderer : SkyRenderer
    {
        [GenerateHLSL]
        public enum PbrSkyConfig
        {
            // Tiny
            GroundIrradianceTableSize     = 256, // <N, L>

            // 32 MiB
            InScatteredRadianceTableSizeX = 128, // <N, V>
            InScatteredRadianceTableSizeY = 32,  // height
            InScatteredRadianceTableSizeZ = 16,  // AzimuthAngle(L) w.r.t. the view vector
            InScatteredRadianceTableSizeW = 64,  // <N, L>,
        }

        // Store the hash of the parameters each time precomputation is done.
        // If the hash does not match, we must recompute our data.
        int m_LastPrecomputationParamHash;

        // We compute at most one bounce per frame for perf reasons.
        // We need to store the frame index because more than one render can happen during a frame (cubemap update + regular rendering).
        int m_LastPrecomputedBounce;

        bool m_IsBuilt = false;

        PhysicallyBasedSky           m_Settings;

        // Precomputed data below.
        RTHandle[]                   m_GroundIrradianceTables;    // All orders, one order
        RTHandle[]                   m_InScatteredRadianceTables; // Air SS, Aerosol SS, Atmosphere MS, Atmosphere one order, Temp

        static ComputeShader         s_GroundIrradiancePrecomputationCS;
        static ComputeShader         s_InScatteredRadiancePrecomputationCS;
        static Material              s_PbrSkyMaterial;
        static MaterialPropertyBlock s_PbrSkyMaterialProperties;

        static GraphicsFormat s_ColorFormat = GraphicsFormat.R16G16B16A16_SFloat;

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

        public PhysicallyBasedSkyRenderer(PhysicallyBasedSky settings)
        {
            m_Settings = settings;
        }

        public override void Build()
        {
            var hdrpAsset     = HDRenderPipeline.currentAsset;
            var hdrpResources = HDRenderPipeline.defaultAsset.renderPipelineResources;

            // Shaders
            s_GroundIrradiancePrecomputationCS    = hdrpResources.shaders.groundIrradiancePrecomputationCS;
            s_InScatteredRadiancePrecomputationCS = hdrpResources.shaders.inScatteredRadiancePrecomputationCS;
            s_PbrSkyMaterial                      = CoreUtils.CreateEngineMaterial(hdrpResources.shaders.physicallyBasedSkyPS);
            s_PbrSkyMaterialProperties            = new MaterialPropertyBlock();

            Debug.Assert(s_GroundIrradiancePrecomputationCS    != null);
            Debug.Assert(s_InScatteredRadiancePrecomputationCS != null);

            // No temp tables.
            m_GroundIrradianceTables       = new RTHandle[2];
            m_GroundIrradianceTables[0]    = AllocateGroundIrradianceTable(0);

            m_InScatteredRadianceTables    = new RTHandle[5];
            m_InScatteredRadianceTables[0] = AllocateInScatteredRadianceTable(0);
            m_InScatteredRadianceTables[1] = AllocateInScatteredRadianceTable(1);
            m_InScatteredRadianceTables[2] = AllocateInScatteredRadianceTable(2);

            m_IsBuilt = true;
        }

        public override void SetGlobalSkyData(CommandBuffer cmd)
        {
            UpdateGlobalConstantBuffer(cmd);

            // TODO: ground irradiance table? Volume SH? Something else?
            if (m_LastPrecomputedBounce > 0)
            {
                cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture,     m_InScatteredRadianceTables[0]);
                cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[2]);
            }
            else
            {
                cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, CoreUtils.blackVolumeTexture);
            }

        }

        public override bool IsValid()
        {
            return m_IsBuilt;
        }

        public override void Cleanup()
        {
            m_Settings = null;

            RTHandles.Release(m_GroundIrradianceTables[0]);    m_GroundIrradianceTables[0]    = null;
            RTHandles.Release(m_GroundIrradianceTables[1]);    m_GroundIrradianceTables[1]    = null;
            RTHandles.Release(m_InScatteredRadianceTables[0]); m_InScatteredRadianceTables[0] = null;
            RTHandles.Release(m_InScatteredRadianceTables[1]); m_InScatteredRadianceTables[1] = null;
            RTHandles.Release(m_InScatteredRadianceTables[2]); m_InScatteredRadianceTables[2] = null;
            RTHandles.Release(m_InScatteredRadianceTables[3]); m_InScatteredRadianceTables[3] = null;
            RTHandles.Release(m_InScatteredRadianceTables[4]); m_InScatteredRadianceTables[4] = null;

            m_LastPrecomputedBounce = 0;
            m_IsBuilt = false;
        }

        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }

        // For both precomputation and runtime lighting passes.
        void UpdateGlobalConstantBuffer(CommandBuffer cmd)
        {
            float R    = m_Settings.planetaryRadius.value;
            float D    = Mathf.Max(m_Settings.airMaximumAltitude.value, m_Settings.aerosolMaximumAltitude.value);
            float airH = m_Settings.GetAirScaleHeight();
            float aerH = m_Settings.GetAerosolScaleHeight();
            float iMul = Mathf.Pow(2.0f, m_Settings.exposure.value) * m_Settings.multiplier.value;

            cmd.SetGlobalFloat( HDShaderIDs._PlanetaryRadius,           R);
            cmd.SetGlobalFloat( HDShaderIDs._RcpPlanetaryRadius,        1.0f / R);
            cmd.SetGlobalFloat( HDShaderIDs._AtmosphericDepth,          D);
            cmd.SetGlobalFloat( HDShaderIDs._RcpAtmosphericDepth,       1.0f / D);

            cmd.SetGlobalFloat( HDShaderIDs._AtmosphericRadius,         R + D);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolAnisotropy,         m_Settings.aerosolAnisotropy.value);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolPhasePartConstant,  CornetteShanksPhasePartConstant(m_Settings.aerosolAnisotropy.value));

            cmd.SetGlobalFloat( HDShaderIDs._AirDensityFalloff,         1.0f / airH);
            cmd.SetGlobalFloat( HDShaderIDs._AirScaleHeight,            airH);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolDensityFalloff,     1.0f / aerH);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolScaleHeight,        aerH);

            cmd.SetGlobalVector(HDShaderIDs._AirSeaLevelExtinction,     m_Settings.GetAirExtinctionCoefficient());
            cmd.SetGlobalFloat( HDShaderIDs._AerosolSeaLevelExtinction, m_Settings.GetAerosolExtinctionCoefficient());

            cmd.SetGlobalVector(HDShaderIDs._AirSeaLevelScattering,     m_Settings.GetAirScatteringCoefficient());
            cmd.SetGlobalFloat( HDShaderIDs._AerosolSeaLevelScattering, m_Settings.GetAerosolScatteringCoefficient());

            cmd.SetGlobalVector(HDShaderIDs._GroundAlbedo,              m_Settings.groundColor.value);
            cmd.SetGlobalFloat(HDShaderIDs._IntensityMultiplier,        iMul);

            cmd.SetGlobalVector(HDShaderIDs._PlanetCenterPosition,      m_Settings.planetCenterPosition.value);
        }

        void PrecomputeTables(CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "In-Scattered Radiance Precomputation"))
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
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AirSingleScatteringTable,       m_InScatteredRadianceTables[0]);
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AerosolSingleScatteringTable,   m_InScatteredRadianceTables[1]);
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable,        m_InScatteredRadianceTables[2]); // MS orders
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTableOrder,   m_InScatteredRadianceTables[3]); // One order
                            break;
                        case 1:
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AirSingleScatteringTexture,     m_InScatteredRadianceTables[0]);
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._GroundIrradianceTexture,        m_GroundIrradianceTables[1]);    // One order
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable,        m_InScatteredRadianceTables[4]); // Temp
                            break;
                        case 2:
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[3]); // One order
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._GroundIrradianceTexture,        m_GroundIrradianceTables[1]);    // One order
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable,        m_InScatteredRadianceTables[4]); // Temp
                            break;
                        case 3:
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[4]); // Temp
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTableOrder,   m_InScatteredRadianceTables[3]); // One order
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._MultipleScatteringTable,        m_InScatteredRadianceTables[2]); // MS orders
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
                        cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._GroundIrradianceTable,          m_GroundIrradianceTables[0]); // All orders
                        cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._GroundIrradianceTableOrder,     m_GroundIrradianceTables[1]); // One order
                    }

                    switch (firstPass)
                    {
                    case 0:
                        break;
                    case 1:
                        cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._AirSingleScatteringTexture,     m_InScatteredRadianceTables[0]);
                        cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                        break;
                    case 2:
                        cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[3]); // One order
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

        public override bool Update(BuiltinSkyParameters builtinParams)
        {
            var cmd = builtinParams.commandBuffer;
            UpdateGlobalConstantBuffer(cmd);

            int currPrecomputationParamHash = m_Settings.GetPrecomputationHashCode();
            if (currPrecomputationParamHash != m_LastPrecomputationParamHash)
            {
                // Hash does not match, have to restart the precomputation from scratch.
                m_LastPrecomputedBounce = 0;
            }

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

            if (m_LastPrecomputedBounce == m_Settings.numberOfBounces.value)
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

            if (m_LastPrecomputedBounce < m_Settings.numberOfBounces.value)
            {
                PrecomputeTables(cmd);
                m_LastPrecomputedBounce++;

                // Update the hash for the current bounce.
                m_LastPrecomputationParamHash = currPrecomputationParamHash;

                // If the sky is realtime, an upcoming update will update the sky lighting. Otherwise we need to force an update.
                return builtinParams.updateMode != EnvironmentUpdateMode.Realtime;
            }

            return false;
        }

        // 'renderSunDisk' parameter is not supported.
        // Users should instead create an emissive (or lit) mesh for every relevant light source
        // (to support multiple stars in space, moons with moon phases, etc).
        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {

            float r = Vector3.Distance(builtinParams.worldSpaceCameraPos, m_Settings.planetCenterPosition.value);
            float R = m_Settings.planetaryRadius.value;

            bool isPbrSkyActive = r > R; // Disable sky rendering below the ground

            CommandBuffer cmd = builtinParams.commandBuffer;

            // Precomputation is done, shading is next.
            Quaternion planetRotation = Quaternion.Euler(m_Settings.planetRotation.value.x,
                                                         m_Settings.planetRotation.value.y,
                                                         m_Settings.planetRotation.value.z);

            Quaternion spaceRotation  = Quaternion.Euler(m_Settings.spaceRotation.value.x,
                                                         m_Settings.spaceRotation.value.y,
                                                         m_Settings.spaceRotation.value.z);

            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            s_PbrSkyMaterialProperties.SetVector(HDShaderIDs._WorldSpaceCameraPos1,  builtinParams.worldSpaceCameraPos);
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._ViewMatrix1,           builtinParams.viewMatrix);
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PlanetRotation,        Matrix4x4.Rotate(planetRotation));
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._SpaceRotation,         Matrix4x4.Rotate(spaceRotation));

            if (m_LastPrecomputedBounce != 0)
            {
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundIrradianceTexture,        m_GroundIrradianceTables[0]);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._AirSingleScatteringTexture,     m_InScatteredRadianceTables[0]);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[2]);
            }
            else
            {
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundIrradianceTexture,        Texture2D.blackTexture);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._AirSingleScatteringTexture,     CoreUtils.blackVolumeTexture);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._AerosolSingleScatteringTexture, CoreUtils.blackVolumeTexture);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._MultipleScatteringTexture,      CoreUtils.blackVolumeTexture);
            }

            int hasGroundAlbedoTexture = 0;

            if (m_Settings.groundAlbedoTexture.value != null)
            {
                hasGroundAlbedoTexture = 1;
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundAlbedoTexture, m_Settings.groundAlbedoTexture.value);
            }
            s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasGroundAlbedoTexture, hasGroundAlbedoTexture);

            int hasGroundEmissionTexture = 0;

            if (m_Settings.groundEmissionTexture.value != null)
            {
                hasGroundEmissionTexture = 1;
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundEmissionTexture, m_Settings.groundEmissionTexture.value);
            }
            s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasGroundEmissionTexture, hasGroundEmissionTexture);

            int hasSpaceEmissionTexture = 0;

            if (m_Settings.spaceEmissionTexture.value != null)
            {
                hasSpaceEmissionTexture = 1;
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._SpaceEmissionTexture, m_Settings.spaceEmissionTexture.value);
            }
            s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._HasSpaceEmissionTexture, hasSpaceEmissionTexture);

            s_PbrSkyMaterialProperties.SetInt(HDShaderIDs._RenderSunDisk, renderSunDisk ? 1 : 0);

            int pass = (renderForCubemap ? 0 : 2) + (isPbrSkyActive ? 0 : 1);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, s_PbrSkyMaterial, s_PbrSkyMaterialProperties, pass);
        }
    }
}

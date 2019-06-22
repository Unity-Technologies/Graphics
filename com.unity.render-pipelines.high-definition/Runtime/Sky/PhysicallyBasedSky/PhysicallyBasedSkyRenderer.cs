using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class PhysicallyBasedSkyRenderer : SkyRenderer
    {
        [GenerateHLSL]
        public enum PbrSkyConfig
        {
            // 64 KiB
            OpticalDepthTableSizeX        = 128, // <N, X>
            OpticalDepthTableSizeY        = 128, // height

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
        int m_LastPrecomputedBounce;

        bool m_IsBuilt = false;

        PhysicallyBasedSkySettings   m_Settings;
        // Precomputed data below.
        RTHandleSystem.RTHandle      m_OpticalDepthTable;
        RTHandleSystem.RTHandle[]    m_GroundIrradianceTables;    // All orders, one order
        RTHandleSystem.RTHandle[]    m_InScatteredRadianceTables; // Air SS, Aerosol SS, Atmosphere MS, Atmosphere one order, Temp

        static ComputeShader         s_OpticalDepthPrecomputationCS;
        static ComputeShader         s_GroundIrradiancePrecomputationCS;
        static ComputeShader         s_InScatteredRadiancePrecomputationCS;
        static Material              s_PbrSkyMaterial;
        static MaterialPropertyBlock s_PbrSkyMaterialProperties;

        static GraphicsFormat s_ColorFormat = GraphicsFormat.R16G16B16A16_SFloat;

        RTHandleSystem.RTHandle AllocateOpticalDepthTable()
        {
            var table = RTHandles.Alloc((int)PbrSkyConfig.OpticalDepthTableSizeX,
                                        (int)PbrSkyConfig.OpticalDepthTableSizeY,
                                        colorFormat: GraphicsFormat.R16G16_SFloat,
                                        enableRandomWrite: true,
                                        name: "OpticalDepthTable");

            Debug.Assert(table != null);

            return table;
        }

        RTHandleSystem.RTHandle AllocateGroundIrradianceTable(int index)
        {
            var table = RTHandles.Alloc((int)PbrSkyConfig.GroundIrradianceTableSize, 1,
                                        colorFormat: s_ColorFormat,
                                        enableRandomWrite: true,
                                        name: string.Format("GroundIrradianceTable{0}", index));

            Debug.Assert(table != null);

            return table;
        }

        RTHandleSystem.RTHandle AllocateInScatteredRadianceTable(int index)
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

        public PhysicallyBasedSkyRenderer(PhysicallyBasedSkySettings settings)
        {
            m_Settings = settings;
        }

        public override void Build()
        {
            var hdrpAsset     = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            var hdrpResources = hdrpAsset.renderPipelineResources;

            // Shaders
            s_OpticalDepthPrecomputationCS        = hdrpResources.shaders.opticalDepthPrecomputationCS;
            s_GroundIrradiancePrecomputationCS    = hdrpResources.shaders.groundIrradiancePrecomputationCS;
            s_InScatteredRadiancePrecomputationCS = hdrpResources.shaders.inScatteredRadiancePrecomputationCS;
            s_PbrSkyMaterial                      = CoreUtils.CreateEngineMaterial(hdrpResources.shaders.physicallyBasedSkyPS);
            s_PbrSkyMaterialProperties            = new MaterialPropertyBlock();

            Debug.Assert(s_OpticalDepthPrecomputationCS        != null);
            Debug.Assert(s_GroundIrradiancePrecomputationCS    != null);
            Debug.Assert(s_InScatteredRadiancePrecomputationCS != null);

            // Textures
            m_OpticalDepthTable = AllocateOpticalDepthTable();

            // No temp tables.
            m_GroundIrradianceTables       = new RTHandleSystem.RTHandle[2];
            m_GroundIrradianceTables[0]    = AllocateGroundIrradianceTable(0);

            m_InScatteredRadianceTables    = new RTHandleSystem.RTHandle[5];
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
                cmd.SetGlobalTexture(HDShaderIDs._OpticalDepthTexture,            m_OpticalDepthTable);
                cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture,     m_InScatteredRadianceTables[0]);
                cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[2]);
            }
            else
            {
                SkyRenderer.SetGlobalNeutralSkyData(cmd);
            }

        }

        public override bool IsValid()
        {
            return m_IsBuilt;
        }

        public override void Cleanup()
        {
            m_Settings = null;

            RTHandles.Release(m_OpticalDepthTable);            m_OpticalDepthTable            = null;
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

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            /* TODO: why is this overridable? */

            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.colorBuffer);
            }
            else
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }

        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }

        static float ComputeScaleHeight(float layerDepth)
        {
            // Exp[-d / s] = 0.001
            // -d / s = Log[0.001]
            // s = d / -Log[0.001]
            return layerDepth * 0.144765f;
        }

        // For both precomputation and runtime lighting passes.
        void UpdateGlobalConstantBuffer(CommandBuffer cmd)
        {

            float R    = m_Settings.planetaryRadius.value;
            float H    = Mathf.Max(m_Settings.airMaxAltitude.value, m_Settings.aerosolMaxAltitude.value);
            float airS = ComputeScaleHeight(m_Settings.airMaxAltitude.value);
            float aerS = ComputeScaleHeight(m_Settings.aerosolMaxAltitude.value);

            cmd.SetGlobalFloat( HDShaderIDs._PlanetaryRadius,           R);
            cmd.SetGlobalFloat( HDShaderIDs._RcpPlanetaryRadius,        1.0f / R);
            cmd.SetGlobalFloat( HDShaderIDs._AtmosphericDepth,          H);
            cmd.SetGlobalFloat( HDShaderIDs._RcpAtmosphericDepth,       1.0f / H);

            cmd.SetGlobalFloat( HDShaderIDs._AtmosphericRadius,         R + H);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolAnisotropy,         m_Settings.aerosolAnisotropy.value);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolPhasePartConstant,  CornetteShanksPhasePartConstant(m_Settings.aerosolAnisotropy.value));

            cmd.SetGlobalFloat( HDShaderIDs._AirDensityFalloff,         1.0f / airS);
            cmd.SetGlobalFloat( HDShaderIDs._AirScaleHeight,            airS);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolDensityFalloff,     1.0f / aerS);
            cmd.SetGlobalFloat( HDShaderIDs._AerosolScaleHeight,        aerS);

            cmd.SetGlobalVector(HDShaderIDs._AirSeaLevelExtinction,     m_Settings.airThickness.value     * 0.001f); // Convert to 1/km
            cmd.SetGlobalFloat( HDShaderIDs._AerosolSeaLevelExtinction, m_Settings.aerosolThickness.value * 0.001f); // Convert to 1/km

            cmd.SetGlobalVector(HDShaderIDs._AirSeaLevelScattering,     m_Settings.airAlbedo.value     * m_Settings.airThickness.value     * 0.001f); // Convert to 1/km
            cmd.SetGlobalFloat( HDShaderIDs._AerosolSeaLevelScattering, m_Settings.aerosolAlbedo.value * m_Settings.aerosolThickness.value * 0.001f); // Convert to 1/km

            cmd.SetGlobalVector(HDShaderIDs._GroundAlbedo,              m_Settings.groundColor.value);
            cmd.SetGlobalVector(HDShaderIDs._PlanetCenterPosition,      m_Settings.planetCenterPosition.value);
        }

        void PrecomputeTables(CommandBuffer cmd)
        {
            if (m_LastPrecomputedBounce == 0)
            {
                // Only needs to be done once.
                using (new ProfilingSample(cmd, "Optical Depth Precomputation"))
                {
                    cmd.SetComputeTextureParam(s_OpticalDepthPrecomputationCS, 0, HDShaderIDs._OpticalDepthTable, m_OpticalDepthTable);
                    cmd.DispatchCompute(s_OpticalDepthPrecomputationCS, 0, (int)PbrSkyConfig.OpticalDepthTableSizeX / 8, (int)PbrSkyConfig.OpticalDepthTableSizeY / 8, 1);
                }
            }

            using (new ProfilingSample(cmd, "In-Scattered Radiance Precomputation"))
            {
                //for (int order = 1; order <= m_Settings.numBounces; order++)
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

                        {
                            // Used by all passes.
                            cmd.SetComputeTextureParam(s_InScatteredRadiancePrecomputationCS, pass, HDShaderIDs._OpticalDepthTexture,  m_OpticalDepthTable);
                        }

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
                        cmd.SetComputeTextureParam(s_GroundIrradiancePrecomputationCS, firstPass, HDShaderIDs._OpticalDepthTexture,            m_OpticalDepthTable);
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

        // 'renderSunDisk' parameter is not supported.
        // Users should instead create an emissive (or lit) mesh for every relevant light source
        // (to support multiple stars in space, moons with moon phases, etc).
        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            CommandBuffer cmd = builtinParams.commandBuffer;
            UpdateGlobalConstantBuffer(cmd);

            int currentParamHash = m_Settings.GetHashCode();

            if (currentParamHash != m_LastPrecomputationParamHash)
            {
                // Hash does not match, have to restart the precomputation from scratch.
                m_LastPrecomputedBounce = 0;
            }

            // F**k cubemap.
            if (!renderForCubemap)
            {
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

                if (m_LastPrecomputedBounce == m_Settings.numBounces.value)
                {
                    // Free temp tables.
                    // This is a deferred release (one frame late)!
                    RTHandles.Release(m_GroundIrradianceTables[1]);
                    RTHandles.Release(m_InScatteredRadianceTables[3]);
                    RTHandles.Release(m_InScatteredRadianceTables[4]);
                    m_GroundIrradianceTables[1]    = null;
                    m_InScatteredRadianceTables[3] = null;
                    m_InScatteredRadianceTables[4] = null;
                }

                if (m_LastPrecomputedBounce < m_Settings.numBounces.value)
                {
                    // We precompute one bounce per render call.
                    PrecomputeTables(cmd);
                    m_LastPrecomputedBounce++;

                    // Update the hash for the current bounce.
                    m_LastPrecomputationParamHash = currentParamHash;
                }
            }

            // Precomputation is done, shading is next.
            Quaternion planetRotation = Quaternion.Euler(m_Settings.planetRotation.value.x,
                                                         m_Settings.planetRotation.value.y,
                                                         m_Settings.planetRotation.value.z);

            Quaternion spaceRotation  = Quaternion.Euler(m_Settings.spaceRotation.value.x,
                                                         m_Settings.spaceRotation.value.y,
                                                         m_Settings.spaceRotation.value.z);

            // This matrix needs to be updated at the draw call frequency.
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._PlanetRotation,        Matrix4x4.Rotate(planetRotation));
            s_PbrSkyMaterialProperties.SetMatrix(HDShaderIDs._SpaceRotation,         Matrix4x4.Rotate(spaceRotation));

            if (m_LastPrecomputedBounce != 0)
            {
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._OpticalDepthTexture,            m_OpticalDepthTable);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._GroundIrradianceTexture,        m_GroundIrradianceTables[0]);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._AirSingleScatteringTexture,     m_InScatteredRadianceTables[0]);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._AerosolSingleScatteringTexture, m_InScatteredRadianceTables[1]);
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._MultipleScatteringTexture,      m_InScatteredRadianceTables[2]);
            }
            else
            {
                s_PbrSkyMaterialProperties.SetTexture(HDShaderIDs._OpticalDepthTexture,            Texture2D.blackTexture);
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

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, s_PbrSkyMaterial, s_PbrSkyMaterialProperties, renderForCubemap ? 0 : 1);
        }
    }
}

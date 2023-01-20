using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Controls the maximum number of foam generators that are supported in one frame
        const int k_MaxNumWaterFoamGenerators = 64;
        const float k_FoamIntensityThreshold = 0.015f;

        // Flag that allows us to track if the system currently supports foam.
        bool m_ActiveWaterFoam = false;

        // Buffer used to hold all the water foam generators on the CPU
        NativeArray<WaterGeneratorData> m_WaterFoamGeneratorDataCPU;

        // The number of foam generators for the current frame
        int m_ActiveWaterFoamGenerators = 0;

        // Buffer used to hold all the water foam generators on the GPU
        ComputeBuffer m_WaterFoamGeneratorData = null;

        // Materials and Compute shaders
        Material m_FoamMaterial;
        MaterialPropertyBlock m_WaterFoamMBP = new MaterialPropertyBlock();
        ComputeShader m_WaterFoamCS;
        int m_ReprojectFoamKernel;
        int m_PostProcessFoamKernel;

        // Keeps track of maximum possible foam intensity in to estimate when there is no more foam
        float m_MaxInjectedFoamIntensity = 0.0f;

        // Atlas used to hold the custom foam generators' textures.
        PowerOfTwoTextureAtlas m_FoamTextureAtlas;

        void InitializeWaterFoam()
        {
            m_ActiveWaterFoam = m_Asset.currentPlatformRenderPipelineSettings.supportWaterFoam;
            if (!m_ActiveWaterFoam)
                return;

            m_FoamMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.waterFoamPS);
            m_WaterFoamGeneratorDataCPU = new NativeArray<WaterGeneratorData>(k_MaxNumWaterFoamGenerators, Allocator.Persistent);
            m_WaterFoamGeneratorData = new ComputeBuffer(k_MaxNumWaterFoamGenerators, System.Runtime.InteropServices.Marshal.SizeOf<WaterGeneratorData>());
            m_FoamTextureAtlas = new PowerOfTwoTextureAtlas((int)m_Asset.currentPlatformRenderPipelineSettings.foamAtlasSize, 1, GraphicsFormat.R16G16_UNorm, name: "Water Foam Atlas", useMipMap: false);
            m_WaterFoamCS = defaultResources.shaders.waterFoamCS;
            m_ReprojectFoamKernel = m_WaterFoamCS.FindKernel("ReprojectFoam");
            m_PostProcessFoamKernel = m_WaterFoamCS.FindKernel("PostProcessFoam");
        }

        void ReleaseWaterFoam()
        {
            if (!m_ActiveWaterFoam)
                return;

            CoreUtils.Destroy(m_FoamMaterial);
            m_WaterFoamGeneratorDataCPU.Dispose();
            CoreUtils.SafeRelease(m_WaterFoamGeneratorData);
            m_FoamTextureAtlas.ResetRequestedTexture();
        }

        void ProcessWaterFoamGenerators(CommandBuffer cmd)
        {
            // Grab all the procedural generators in the scene
            var foamGenerators = WaterFoamGenerator.instancesAsArray;
            int numWaterGenerators = WaterFoamGenerator.instanceCount;

            // Reset the atlas
            m_FoamTextureAtlas.ResetRequestedTexture();

            // Loop through the custom generators and reserve space
            bool needRelayout = false;
            for (int generatorIdx = 0; generatorIdx < numWaterGenerators; ++generatorIdx)
            {
                // If we don't have any slots left, we're done
                if (m_ActiveWaterFoamGenerators >= k_MaxNumWaterFoamGenerators)
                    break;

                WaterFoamGenerator foamGenerator = foamGenerators[generatorIdx];
                if (foamGenerator.type == WaterFoamGeneratorType.Texture && foamGenerator.texture != null)
                {
                    if (!m_FoamTextureAtlas.ReserveSpace(foamGenerator.texture))
                        needRelayout = true;
                }
            }

            // Ask for a relayout
            bool outOfSpace = false;
            if (needRelayout && !m_FoamTextureAtlas.RelayoutEntries())
            {
                outOfSpace = true;
            }

            // Loop through the procedural generators first
            WaterGeneratorData data = new WaterGeneratorData();
            for (int generatorIdx = 0; generatorIdx < numWaterGenerators; ++generatorIdx)
            {
                // If we don't have any slots left, we're done
                if (m_ActiveWaterFoamGenerators >= k_MaxNumWaterFoamGenerators)
                    break;

                // Grab the current generator to process
                WaterFoamGenerator currentGenerator = foamGenerators[generatorIdx];

                // Generator properties
                data.position = currentGenerator.transform.position;
                data.type = (int)currentGenerator.type;
                data.rotation = -currentGenerator.transform.eulerAngles.y * Mathf.Deg2Rad;
                data.regionSize = currentGenerator.regionSize;
                data.deepFoamDimmer = currentGenerator.deepFoamDimmer;
                data.surfaceFoamDimmer = currentGenerator.surfaceFoamDimmer;

                if (currentGenerator.type == WaterFoamGeneratorType.Texture && currentGenerator.texture != null)
                {
                    Texture tex = currentGenerator.texture;
                    if (!m_FoamTextureAtlas.IsCached(out var scaleBias, m_FoamTextureAtlas.GetTextureID(tex)) && outOfSpace)
                        Debug.LogError($"No more space in the 2D Water Generator Altas to store the texture {tex}. To solve this issue, increase the resolution of the Generator Atlas Size in the current HDRP asset.");

                    if (m_FoamTextureAtlas.NeedsUpdate(tex, false))
                        m_FoamTextureAtlas.BlitTexture(cmd, scaleBias, tex, new Vector4(1, 1, 0, 0), blitMips: false, overrideInstanceID: m_FoamTextureAtlas.GetTextureID(tex));
                    data.scaleOffset = scaleBias;
                }

                // Enqueue the generator
                m_WaterFoamGeneratorDataCPU[m_ActiveWaterFoamGenerators] = data;
                m_ActiveWaterFoamGenerators++;
            }
        }

        void UpdateWaterGeneratorsData(CommandBuffer cmd)
        {
            // Reset the water generators count
            m_ActiveWaterFoamGenerators = 0;

            // If foam is not supported, nothing to do beyond this point
            if (!m_ActiveWaterFoam)
                return;

            // Do a pass on the foam generators
            ProcessWaterFoamGenerators(cmd);

            // Push the generators to the GPU
            m_WaterFoamGeneratorData.SetData(m_WaterFoamGeneratorDataCPU);

            // The global generators buffer data needs to be bound once
            cmd.SetGlobalBuffer(HDShaderIDs._WaterGeneratorData, m_WaterFoamGeneratorData);

            // Bind the deformer atlas texture
            cmd.SetGlobalTexture(HDShaderIDs._WaterGeneratorTextureAtlas, m_FoamTextureAtlas.AtlasTexture);
        }

        void UpdateWaterFoamSimulation(CommandBuffer cmd, WaterSurface currentWater)
        {
            // If foam is not supported, nothing to do beyond this point
            if (!m_ActiveWaterFoam)
                return;

            // First we must ensure, that the texture is there (if it should be) and at the right resolution
            currentWater.CheckFoamResources();

            // Skip if there are is foam to render
            if (!currentWater.foam)
                return;

            // What are the type of foam injectors?
            bool foamGenerators = m_ActiveWaterFoamGenerators > 0;
            bool waterDeformers = WaterHasDeformation(currentWater);
            if (!foamGenerators && !waterDeformers)
            {
                if (m_MaxInjectedFoamIntensity <= k_FoamIntensityThreshold)
                    return;
                // Attenuation formula must be in sync with WaterFoam.shader
                m_MaxInjectedFoamIntensity *= Mathf.Exp(-m_ShaderVariablesWater._DeltaTime * m_ShaderVariablesWater._FoamPersistenceMultiplier * 0.5f);
            }
            else
                m_MaxInjectedFoamIntensity = 1.0f;

            // Grab the foam buffers
            RTHandle currentFoamBuffer = currentWater.foamBuffers[0];
            RTHandle previousFoamBuffer = currentWater.foamBuffers[1];

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceFoam)))
            {
                // Reproject the previous frame's foam buffer
                int tileC = ((int)currentWater.foamResolution + 7) / 8;
                ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_WaterFoamCS, HDShaderIDs._ShaderVariablesWater);
                cmd.SetComputeVectorParam(m_WaterFoamCS, HDShaderIDs._PreviousFoamRegionData, currentWater.previousFoamData);
                cmd.SetComputeTextureParam(m_WaterFoamCS, m_ReprojectFoamKernel, HDShaderIDs._WaterFoamBuffer, currentFoamBuffer);
                cmd.SetComputeTextureParam(m_WaterFoamCS, m_ReprojectFoamKernel, HDShaderIDs._WaterFoamBufferRW, previousFoamBuffer);
                cmd.DispatchCompute(m_WaterFoamCS, m_ReprojectFoamKernel, tileC, tileC, 1);

                // Then we render the deformers and the generators
                ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_FoamMaterial, HDShaderIDs._ShaderVariablesWater);
                CoreUtils.SetRenderTarget(cmd, currentFoamBuffer, ClearFlag.Color, Color.black);
                if (waterDeformers)
                    cmd.DrawProcedural(Matrix4x4.identity, m_FoamMaterial, 0, MeshTopology.Triangles, 6, m_ActiveWaterDeformers);
                if (foamGenerators)
                    cmd.DrawProcedural(Matrix4x4.identity, m_FoamMaterial, 1, MeshTopology.Triangles, 6, m_ActiveWaterFoamGenerators);

                // Apply an attenuation and blend the current foam
                CoreUtils.SetRenderTarget(cmd, previousFoamBuffer);
                // Setup the mbp
                m_WaterFoamMBP.Clear();
                m_WaterFoamMBP.SetTexture(HDShaderIDs._WaterFoamBuffer, currentFoamBuffer);
                cmd.DrawProcedural(Matrix4x4.identity, m_FoamMaterial, 2, MeshTopology.Triangles, 3, 1, m_WaterFoamMBP);

                // To avoid the swap in swap out of the textures, we do this.
                cmd.SetComputeTextureParam(m_WaterFoamCS, m_PostProcessFoamKernel, HDShaderIDs._WaterFoamBuffer, previousFoamBuffer);
                cmd.SetComputeTextureParam(m_WaterFoamCS, m_PostProcessFoamKernel, HDShaderIDs._WaterFoamBufferRW, currentFoamBuffer);
                cmd.DispatchCompute(m_WaterFoamCS, m_PostProcessFoamKernel, tileC, tileC, 1);

                // Update the foam data for the next frame
                currentWater.previousFoamData = float4(currentWater.foamAreaSize.x, currentWater.foamAreaSize.y, currentWater.foamAreaOffset.x, currentWater.foamAreaOffset.y);
            }
        }
    }
}

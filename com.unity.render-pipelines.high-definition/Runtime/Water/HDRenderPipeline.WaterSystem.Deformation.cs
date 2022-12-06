using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Controls the maximum number of deformers that are supported in one frame
        const int k_MaxNumWaterDeformers = 64;

        // Flag that allows us to track if the system currently supports foam.
        bool m_ActiveWaterDeformation = false;

        // Buffer used to hold all the water deformers on the CPU
        NativeArray<WaterDeformerData> m_WaterDeformersDataCPU;

        // The number of deformers for the current frame
        int m_ActiveWaterDeformers = 0;

        // Buffer used to hold all the water deformers on the GPU
        ComputeBuffer m_WaterDeformersData = null;

        // The material used to render the deformers
        Material m_DeformerMaterial = null;
        ShaderVariablesWaterDeformation m_SVWaterDeformation;

        // Filtering and normal kernels
        ComputeShader m_WaterDeformationCS;
        int m_FilterDeformationKernel;
        int m_EvaluateDeformationSurfaceGradientKernel;

        // Atlas used to hold the custom deformers' textures.
        PowerOfTwoTextureAtlas m_DeformerAtlas;

        void InitializeWaterDeformers()
        {
            m_ActiveWaterDeformation = m_Asset.currentPlatformRenderPipelineSettings.supportWaterDeformation;
            if (!m_ActiveWaterDeformation)
                return;

            m_WaterDeformersData = new ComputeBuffer(k_MaxNumWaterDeformers, System.Runtime.InteropServices.Marshal.SizeOf<WaterDeformerData>());
            m_WaterDeformersDataCPU = new NativeArray<WaterDeformerData>(k_MaxNumWaterDeformers, Allocator.Persistent);
            m_DeformerMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.waterDeformationPS);
            m_WaterDeformationCS = defaultResources.shaders.waterDeformationCS;
            m_FilterDeformationKernel = m_WaterDeformationCS.FindKernel("FilterDeformation");
            m_EvaluateDeformationSurfaceGradientKernel = m_WaterDeformationCS.FindKernel("EvaluateDeformationSurfaceGradient");
            m_DeformerAtlas = new PowerOfTwoTextureAtlas((int)m_Asset.currentPlatformRenderPipelineSettings.deformationAtlasSize, 1, GraphicsFormat.R16_UNorm, name: "Water Deformation Atlas", useMipMap: false);
        }

        void ReleaseWaterDeformers()
        {
            if (!m_ActiveWaterDeformation)
                return;

            m_WaterDeformersDataCPU.Dispose();
            m_DeformerAtlas.ResetRequestedTexture();
            CoreUtils.Destroy(m_DeformerMaterial);
            CoreUtils.SafeRelease(m_WaterDeformersData);
        }

        void ProcessWaterDeformers(CommandBuffer cmd)
        {
            // Reset the requested textures
            m_DeformerAtlas.ResetRequestedTexture();

            // Grab all the deformers in the scene
            var deformerArray = WaterDeformer.instancesAsArray;
            int numDeformers = WaterDeformer.instanceCount;

            // Loop through the deformers and reserve space
            bool needRelayout = false;
            for (int deformerIdx = 0; deformerIdx < numDeformers; ++deformerIdx)
            {
                // If we don't have any slots left, we're done
                if (m_ActiveWaterDeformers >= k_MaxNumWaterDeformers)
                    break;

                // Grab the current deformer to process
                WaterDeformer deformer = deformerArray[deformerIdx];
                if (deformer.type == WaterDeformerType.Texture && deformer.texture != null)
                {
                    if (!m_DeformerAtlas.ReserveSpace(deformer.texture))
                    {
                        needRelayout = true;
                    }
                }
            }

            // Ask for a relayout
            bool outOfSpace = false;
            if (needRelayout && !m_DeformerAtlas.RelayoutEntries())
            {
                outOfSpace = true;
            }

            // Loop through the deformers
            WaterDeformerData data = new WaterDeformerData();
            for (int deformerIdx = 0; deformerIdx < numDeformers; ++deformerIdx)
            {
                // If we don't have any slots left, we're done
                if (m_ActiveWaterDeformers >= k_MaxNumWaterDeformers)
                    break;

                // Grab the current deformer to process
                WaterDeformer currentDeformer = deformerArray[deformerIdx];

                // If this is a texture deformer without a texture skip it
                if (currentDeformer.type == WaterDeformerType.Texture && currentDeformer.texture == null)
                    continue;

                // General
                data.position = currentDeformer.transform.position;
                data.type = (int)currentDeformer.type;
                data.amplitude = currentDeformer.amplitude;
                data.rotation = -currentDeformer.transform.eulerAngles.y * Mathf.Deg2Rad;
                data.regionSize = currentDeformer.regionSize;
                data.deepFoamDimmer = currentDeformer.deepFoamDimmer;
                data.surfaceFoamDimmer = currentDeformer.surfaceFoamDimmer;

                switch (currentDeformer.type)
                {
                    case WaterDeformerType.Sphere:
                        {
                            // We do not want any blend for the sphere
                            data.blendRegion = Vector2.zero;
                            data.cubicBlend = 0;
                        }
                        break;
                    case WaterDeformerType.Box:
                        {
                            data.blendRegion = currentDeformer.boxBlend;
                            data.cubicBlend = currentDeformer.cubicBlend ? 1 : 0;
                        }
                        break;
                    case WaterDeformerType.BowWave:
                        {
                            data.bowWaveElevation = currentDeformer.bowWaveElevation;
                            data.cubicBlend = 0;
                            data.blendRegion = Vector2.zero;
                        }
                        break;
                    case WaterDeformerType.ShoreWave:
                        {
                            data.amplitude *= 0.5f;
                            data.waveLength = currentDeformer.waveLength;
                            data.waveRepetition = currentDeformer.waveRepetition;
                            data.breakingRange = currentDeformer.breakingRange;
                            data.waveSpeed = currentDeformer.waveSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                            data.waveOffset = currentDeformer.waveOffset;
                            data.blendRegion = currentDeformer.waveBlend;
                            data.deepFoamRange = currentDeformer.deepFoamRange;
                        }
                        break;
                    case WaterDeformerType.Texture:
                        {
                            Texture tex = currentDeformer.texture;
                            if (!m_DeformerAtlas.IsCached(out var scaleBias, m_DeformerAtlas.GetTextureID(tex)) && outOfSpace)
                                Debug.LogError($"No more space in the 2D Water Deformer Altas to store the texture {tex}. To solve this issue, increase the resolution of the Deformation Atlas Size in the current HDRP asset.");

                            if (m_DeformerAtlas.NeedsUpdate(tex, false))
                                m_DeformerAtlas.BlitTexture(cmd, scaleBias, tex, new Vector4(1, 1, 0, 0), blitMips: false, overrideInstanceID: m_DeformerAtlas.GetTextureID(tex));

                            // General
                            data.scaleOffset = scaleBias;
                            data.regionSize = currentDeformer.regionSize;
                            data.blendRegion = currentDeformer.range;

                            // Validate it and push it to the buffer
                            m_WaterDeformersDataCPU[m_ActiveWaterDeformers] = data;
                            m_ActiveWaterDeformers++;
                        }
                        break;
                }

                m_WaterDeformersDataCPU[m_ActiveWaterDeformers] = data;
                m_ActiveWaterDeformers++;
            }
        }

        void UpdateWaterDeformersData(CommandBuffer cmd)
        {
            // Reset the water deformer count
            m_ActiveWaterDeformers = 0;

            // If deformation is not supported, nothing to do beyond this point
            if (!m_ActiveWaterDeformation)
                return;

            // Do a pass on the deformers
            ProcessWaterDeformers(cmd);

            // Push the deformers to the GPU
            m_WaterDeformersData.SetData(m_WaterDeformersDataCPU);

            // The global deformer buffer data needs to be bound once
            cmd.SetGlobalBuffer(HDShaderIDs._WaterDeformerData, m_WaterDeformersData);

            // Bind the deformer atlas texture
            cmd.SetGlobalTexture(HDShaderIDs._WaterDeformerTextureAtlas, m_DeformerAtlas.AtlasTexture);
        }

        bool WaterHasDeformation(WaterSurface currentWater)
        {
            return (m_ActiveWaterDeformers > 0 && currentWater.deformation);
        }

        void UpdateWaterDeformation(CommandBuffer cmd, WaterSurface currentWater)
        {
            // First we must ensure, that the texture is there (if it should be) and at the right resolution
            currentWater.CheckDeformationResources();

            // If deformation will not be read, nothing to do
            if (!currentWater.deformation || !m_ActiveWaterDeformation)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceDeformation)))
            {
                if (m_ActiveWaterDeformers > 0)
                {
                    // Fill the deformation constant buffer
                    m_SVWaterDeformation._WaterDeformationCenter = currentWater.deformationAreaOffset;
                    m_SVWaterDeformation._WaterDeformationExtent = currentWater.deformationAreaSize;
                    m_SVWaterDeformation._WaterDeformationResolution = (int)currentWater.deformationRes;

                    // Clear the render target to black and draw all the deformers to the texture
                    CoreUtils.SetRenderTarget(cmd, currentWater.deformationSGBuffer, clearFlag: ClearFlag.Color, Color.black);
                    ConstantBuffer.Push(cmd, m_SVWaterDeformation, m_DeformerMaterial, HDShaderIDs._ShaderVariablesWaterDeformation);
                    ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_DeformerMaterial, HDShaderIDs._ShaderVariablesWater);
                    cmd.DrawProcedural(Matrix4x4.identity, m_DeformerMaterial, 0, MeshTopology.Triangles, 6, m_ActiveWaterDeformers);

                    // Evaluate the normals
                    ConstantBuffer.Push(cmd, m_SVWaterDeformation, m_WaterDeformationCS, HDShaderIDs._ShaderVariablesWaterDeformation);
                    int numTiles = (m_SVWaterDeformation._WaterDeformationResolution + 7) / 8;

                    // First we need to clear the edge pixel and blur the deformation a bit
                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_FilterDeformationKernel, HDShaderIDs._WaterDeformationBuffer, currentWater.deformationSGBuffer);
                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_FilterDeformationKernel, HDShaderIDs._WaterDeformationBufferRW, currentWater.deformationBuffer);
                    cmd.DispatchCompute(m_WaterDeformationCS, m_FilterDeformationKernel, numTiles, numTiles, 1);
                    // For the CPU Simulation
                    currentWater.deformationBuffer.rt.IncrementUpdateCount();

                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_EvaluateDeformationSurfaceGradientKernel, HDShaderIDs._WaterDeformationBuffer, currentWater.deformationBuffer);
                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_EvaluateDeformationSurfaceGradientKernel, HDShaderIDs._WaterDeformationSGBufferRW, currentWater.deformationSGBuffer);
                    cmd.DispatchCompute(m_WaterDeformationCS, m_EvaluateDeformationSurfaceGradientKernel, numTiles, numTiles, 1);
                }
                else
                {
                    CoreUtils.SetRenderTarget(cmd, currentWater.deformationBuffer, clearFlag: ClearFlag.Color, Color.black);
                    CoreUtils.SetRenderTarget(cmd, currentWater.deformationSGBuffer, clearFlag: ClearFlag.Color, Color.black);
                }
            }
        }

        // Function that returns the number of active water deformers
        internal int NumActiveWaterDeformers()
        {
            return m_ActiveWaterDeformers;
        }
    }
}

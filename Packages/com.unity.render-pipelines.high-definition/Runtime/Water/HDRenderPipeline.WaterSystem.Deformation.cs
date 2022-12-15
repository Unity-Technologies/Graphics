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

        // Flag that tracks if the water deformation was enabled for this HDRP asset
        bool m_ActiveWaterDeformation = false;

        // Buffer used to hold all the water deformers on the CPU
        NativeArray<WaterDeformerData> m_WaterDeformersDataCPU;

        // The number of deformers for the current frame
        int m_CurrentActiveDeformers = 0;

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
            {
                // Needs to be allocated for the CPU simulation
                m_WaterDeformersDataCPU = new NativeArray<WaterDeformerData>(1, Allocator.Persistent);
                return;
            }

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
            m_WaterDeformersDataCPU.Dispose();

            if (!m_ActiveWaterDeformation)
                return;

            m_DeformerAtlas.ResetRequestedTexture();
            CoreUtils.Destroy(m_DeformerMaterial);
            CoreUtils.SafeRelease(m_WaterDeformersData);
        }

        void ProcessProceduralWaterDeformers()
        {
            // Grab all the procedural deformers in the scene
            var proceduralDeformers = ProceduralWaterDeformer.instancesAsArray;
            int numProceduralDeformers = ProceduralWaterDeformer.instanceCount;

            // Loop through the procedural deformers
            WaterDeformerData data = new WaterDeformerData();
            for (int deformerIdx = 0; deformerIdx < numProceduralDeformers; ++deformerIdx)
            {
                // If we don't have any slots left, we're done
                if (m_CurrentActiveDeformers >= k_MaxNumWaterDeformers)
                    break;

                // Grab the current deformer to process
                ProceduralWaterDeformer currentDeformer = proceduralDeformers[deformerIdx];

                // General
                data.position = currentDeformer.transform.position;
                data.type = (int)currentDeformer.type;
                data.amplitude = currentDeformer.amplitude;
                data.rotation = -currentDeformer.transform.eulerAngles.y * Mathf.Deg2Rad;
                data.regionSize = currentDeformer.regionSize;

                switch (currentDeformer.type)
                {
                    case ProceduralWaterDeformerType.Sphere:
                        {
                            // We do not want any blend for the sphere
                            data.blendRegion = Vector2.zero;
                            data.cubicBlend = 0;
                        }
                        break;
                    case ProceduralWaterDeformerType.Box:
                        {
                            data.blendRegion = currentDeformer.boxBlend;
                            data.cubicBlend = currentDeformer.cubicBlend ? 1 : 0;
                        }
                        break;
                    case ProceduralWaterDeformerType.BowWave:
                        {
                            data.bowWaveElevation = currentDeformer.bowWaveElevation;
                            data.cubicBlend = 0;
                            data.blendRegion = Vector2.zero;
                        }
                        break;
                    case ProceduralWaterDeformerType.SineWave:
                        {
                            data.amplitude *= 0.5f;
                            data.waveLength = currentDeformer.waveLength;
                            data.waveRepetition = currentDeformer.waveRepetition;
                            data.peakLocation = currentDeformer.peakLocation;
                            data.waveSpeed = currentDeformer.waveSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                            data.waveOffset = currentDeformer.waveOffset;
                            data.blendRegion = currentDeformer.waveBlend;
                        }
                        break;
                }

                m_WaterDeformersDataCPU[m_CurrentActiveDeformers] = data;
                m_CurrentActiveDeformers++;
            }
        }

        void ProcessTextureWaterDeformers(CommandBuffer cmd)
        {
            m_DeformerAtlas.ResetRequestedTexture();

            // Grab all the water deformers in the scene
            var textureDeformers = TextureWaterDeformer.instancesAsArray;
            int numTextureDeformers = TextureWaterDeformer.instanceCount;

            // Loop through the custom deformers and reserve space
            bool needRelayout = false;
            for (int deformerIdx = 0; deformerIdx < numTextureDeformers; ++deformerIdx)
            {
                // If we don't have any slots left, we're done
                if (m_CurrentActiveDeformers >= k_MaxNumWaterDeformers)
                    break;

                // Grab the current deformer to process
                TextureWaterDeformer textureDeformer = textureDeformers[deformerIdx];
                if (textureDeformer.texture != null)
                {
                    if(!m_DeformerAtlas.ReserveSpace(textureDeformer.texture))
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


            // Loop through the custom deformers and reserve space
            WaterDeformerData data = new WaterDeformerData();
            for (int deformerIdx = 0; deformerIdx < numTextureDeformers; ++deformerIdx)
            {
                // If we don't have any slots left, we're done
                if (m_CurrentActiveDeformers >= k_MaxNumWaterDeformers)
                    break;

                // Grab the current deformer to process
                TextureWaterDeformer textureDeformer = textureDeformers[deformerIdx];
                if (textureDeformer.texture != null)
                {
                    Texture tex = textureDeformer.texture;
                    if (!m_DeformerAtlas.IsCached(out var scaleBias, m_DeformerAtlas.GetTextureID(tex)) && outOfSpace)
                        Debug.LogError($"No more space in the 2D Water Deformer Altas to store the texture {tex}. To solve this issue, increase the resolution of the Deformation Atlas Size in the current HDRP asset.");

                    if (m_DeformerAtlas.NeedsUpdate(tex, false))
                        m_DeformerAtlas.BlitTexture(cmd, scaleBias, tex, new Vector4(1, 1, 0, 0), blitMips: false, overrideInstanceID: m_DeformerAtlas.GetTextureID(tex));

                    // General
                    data.position = textureDeformer.transform.position;
                    data.type = 4;
                    data.amplitude = textureDeformer.amplitude;
                    data.rotation = -textureDeformer.transform.eulerAngles.y * Mathf.Deg2Rad;
                    data.scaleOffset = scaleBias;
                    data.regionSize = textureDeformer.regionSize;
                    data.blendRegion = textureDeformer.range;

                    // Validate it and push it to the buffer
                    m_WaterDeformersDataCPU[m_CurrentActiveDeformers] = data;
                    m_CurrentActiveDeformers++;
                }
            }
        }

        void UpdateWaterDeformersData(CommandBuffer cmd)
        {
            // Reset the water deformer count
            m_CurrentActiveDeformers = 0;

            // If the water deformation is not active, skip this step
            if (!m_ActiveWaterDeformation)
                return;

            // Do a pass on the procedural deformers
            ProcessProceduralWaterDeformers();

            // Do a pass on the custom deformers
            ProcessTextureWaterDeformers(cmd);

            // Push the deformers to the GPU
            m_WaterDeformersData.SetData(m_WaterDeformersDataCPU);

            // The global deformer buffer data needs to be bound once
            cmd.SetGlobalBuffer(HDShaderIDs._WaterDeformerData, m_WaterDeformersData);

            // Bind the deformer atlas texture
            cmd.SetGlobalTexture(HDShaderIDs._WaterDeformerTextureAtlas, m_DeformerAtlas.AtlasTexture);
        }

        void UpdateWaterDeformation(CommandBuffer cmd, WaterSurface currentWater)
        {
            // First we must ensure, that the texture is there (if it should be) and at the right resolution
            currentWater.CheckDeformationResources();

            // Skip if there are no deformation to render
            if (!m_ActiveWaterDeformation|| !currentWater.deformation)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceDeformation)))
            {
                if (m_CurrentActiveDeformers > 0)
                {
                    // Fill the deformation constant buffer
                    m_SVWaterDeformation._WaterDeformationCenter = currentWater.deformationAreaOffset;
                    m_SVWaterDeformation._WaterDeformationExtent = currentWater.deformationAreaSize;
                    m_SVWaterDeformation._WaterDeformationResolution = (int)currentWater.deformationRes;

                    // Clear the render target to black and draw all the deformers to the texture
                    CoreUtils.SetRenderTarget(cmd, currentWater.deformationSGBuffer, clearFlag: ClearFlag.Color, Color.black);
                    ConstantBuffer.Push(cmd, m_SVWaterDeformation, m_DeformerMaterial, HDShaderIDs._ShaderVariablesWaterDeformation);
                    ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_DeformerMaterial, HDShaderIDs._ShaderVariablesWater);
                    cmd.DrawProcedural(Matrix4x4.identity, m_DeformerMaterial, 0, MeshTopology.Triangles, 6, m_CurrentActiveDeformers);

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
            return m_ActiveWaterDeformation ? m_CurrentActiveDeformers : 0;
        }

        // Function that returns the array of deformers
        internal NativeArray<WaterDeformerData> ActiveWaterDeformers()
        {
            return m_WaterDeformersDataCPU;
        }
    }
}

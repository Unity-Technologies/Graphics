using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

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

        // The pass used in the WaterFoam.shader
        int m_ShoreWaveFoamGenerationPass;
        int m_OtherFoamGenerationPass;
        int m_AttenuationPass;

        ComputeShader m_WaterFoamCS;
        int m_ReprojectFoamKernel;
        int m_AttenuateFoamKernel;

        // Keeps track of maximum possible foam intensity in to estimate when there is no more foam
        float m_MaxInjectedFoamIntensity = 0.0f;

        // Atlas used to hold the custom foam generators' textures.
        PowerOfTwoTextureAtlas m_FoamTextureAtlas;

        void InitializeWaterFoam()
        {
            m_ActiveWaterFoam = m_Asset.currentPlatformRenderPipelineSettings.supportWaterFoam;
            if (!m_ActiveWaterFoam)
                return;

            m_FoamMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.waterFoamPS);
            m_WaterFoamGeneratorDataCPU = new NativeArray<WaterGeneratorData>(k_MaxNumWaterFoamGenerators, Allocator.Persistent);
            m_WaterFoamGeneratorData = new ComputeBuffer(k_MaxNumWaterFoamGenerators, System.Runtime.InteropServices.Marshal.SizeOf<WaterGeneratorData>());
            m_FoamTextureAtlas = new PowerOfTwoTextureAtlas((int)m_Asset.currentPlatformRenderPipelineSettings.foamAtlasSize, 0, GraphicsFormat.R16G16_UNorm, name: "Water Foam Atlas", useMipMap: false);
            m_WaterFoamCS = runtimeShaders.waterFoamCS;
            m_ReprojectFoamKernel = m_WaterFoamCS.FindKernel("ReprojectFoam");
            m_AttenuateFoamKernel = m_WaterFoamCS.FindKernel("AttenuateFoam");

            m_ShoreWaveFoamGenerationPass = m_FoamMaterial.FindPass("ShoreWaveFoamGeneration");
            m_OtherFoamGenerationPass = m_FoamMaterial.FindPass("OtherFoamGeneration");
            m_AttenuationPass = m_FoamMaterial.FindPass("Attenuation");
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (WaterFoamGenerator.instanceCount >= k_MaxNumWaterFoamGenerators)
                Debug.LogWarning("Maximum amount of Foam Generator reached. Some of them will be ignored.");
#endif

            // Grab all the procedural generators in the scene
            var foamGenerators = WaterFoamGenerator.instancesAsArray;
            int numWaterGenerators = Mathf.Min(WaterFoamGenerator.instanceCount, k_MaxNumWaterFoamGenerators);

            // Reset the atlas
            m_FoamTextureAtlas.ResetRequestedTexture();

            // Loop through the custom generators and reserve space
            bool needRelayout = false;
            for (int generatorIdx = 0; generatorIdx < numWaterGenerators; ++generatorIdx)
            {
                WaterFoamGenerator foamGenerator = foamGenerators[generatorIdx];
                if (foamGenerator.type == WaterFoamGeneratorType.Texture && foamGenerator.texture != null)
                {
                    if (!m_FoamTextureAtlas.ReserveSpace(foamGenerator.texture))
                        needRelayout = true;
                }
                else if (foamGenerator.type == WaterFoamGeneratorType.Material && foamGenerator.IsValidMaterial())
                {
                    if (!m_FoamTextureAtlas.ReserveSpace(foamGenerator.GetMaterialAtlasingId(), foamGenerator.resolution.x, foamGenerator.resolution.y))
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
                // Grab the current generator to process
                WaterFoamGenerator currentGenerator = foamGenerators[generatorIdx];

                // If this is a texture deformer without a texture skip it
                if (currentGenerator.type == WaterFoamGeneratorType.Texture && currentGenerator.texture == null)
                    continue;
                if (currentGenerator.type == WaterFoamGeneratorType.Material && !currentGenerator.IsValidMaterial())
                    continue;

                // Generator properties
                data.position = currentGenerator.transform.position;
                data.type = (int)currentGenerator.type;
                data.rotation = -currentGenerator.transform.eulerAngles.y * Mathf.Deg2Rad;
                data.regionSize = Vector2.Scale(currentGenerator.regionSize, currentGenerator.scale);
                data.deepFoamDimmer = currentGenerator.deepFoamDimmer;
                data.surfaceFoamDimmer = currentGenerator.surfaceFoamDimmer;

                if (currentGenerator.type == WaterFoamGeneratorType.Texture)
                {
                    Texture tex = currentGenerator.texture;
                    if (!m_FoamTextureAtlas.IsCached(out var scaleBias, m_FoamTextureAtlas.GetTextureID(tex)) && outOfSpace)
                        Debug.LogError($"No more space in the 2D Water Foam Altas to store the texture {tex}. To solve this issue, increase the resolution of the Foam Atlas Size in the current HDRP asset.");

                    if (m_FoamTextureAtlas.NeedsUpdate(tex, false))
                        m_FoamTextureAtlas.BlitTexture(cmd, scaleBias, tex, new Vector4(1, 1, 0, 0), blitMips: false, overrideInstanceID: m_FoamTextureAtlas.GetTextureID(tex));
                    data.scaleOffset = scaleBias;
                }
                else if (currentGenerator.type == WaterFoamGeneratorType.Material)
                {
                    Material mat = currentGenerator.material;
                    if (!m_FoamTextureAtlas.IsCached(out var scaleBias, currentGenerator.GetMaterialAtlasingId()) && outOfSpace)
                        Debug.LogError($"No more space in the 2D Water Foam Altas to store the material {mat}. To solve this issue, increase the resolution of the Foam Atlas Size in the current HDRP asset.");

                    if (currentGenerator.updateMode == CustomRenderTextureUpdateMode.Realtime || currentGenerator.shouldUpdate)
                    {
                        var size = (int)m_Asset.currentPlatformRenderPipelineSettings.foamAtlasSize;
                        cmd.SetRenderTarget(m_FoamTextureAtlas.AtlasTexture);
                        cmd.SetViewport(new Rect(scaleBias.z * size, scaleBias.w * size, scaleBias.x * size, scaleBias.y * size));
                        cmd.DrawProcedural(Matrix4x4.identity, mat, (int)WaterDeformer.PassType.FoamGenerator, MeshTopology.Triangles, 3, 1, currentGenerator.mpb);

                        currentGenerator.shouldUpdate = false;
                    }

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
            currentWater.CheckFoamResources(cmd);

            // Skip if there are is foam to render
            if (!currentWater.foam)
                return;

            // What are the type of foam injectors?
            ref var cb = ref m_ShaderVariablesPerSurfaceArray[currentWater.surfaceIndex];
            bool foamGenerators = m_ActiveWaterFoamGenerators > 0;
            bool waterDeformers = WaterHasDeformation(currentWater);
            if (!foamGenerators && !waterDeformers)
            {
                if (m_MaxInjectedFoamIntensity <= k_FoamIntensityThreshold)
                    return;

                // Attenuation formula must be in sync with WaterFoam.shader
                m_MaxInjectedFoamIntensity *= Mathf.Exp(-cb._DeltaTime * cb._FoamPersistenceMultiplier * 0.5f);
            }
            else
                m_MaxInjectedFoamIntensity = 1.0f;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceFoam)))
            {
                RTHandle currentFoamBuffer = currentWater.FoamBuffer();
                BindPerSurfaceConstantBuffer(cmd, m_WaterFoamCS, m_ShaderVariablesWaterPerSurface[currentWater.surfaceIndex]);

                // Check if we need to reproj
                if (currentWater.previousFoamRegionScaleOffset.x != cb._FoamRegionScale.x ||
                    currentWater.previousFoamRegionScaleOffset.y != cb._FoamRegionScale.y ||
                    currentWater.previousFoamRegionScaleOffset.z != cb._FoamRegionOffset.x ||
                    currentWater.previousFoamRegionScaleOffset.w != cb._FoamRegionOffset.y)
                {
                    RTHandle tmpFoamBuffer = currentWater.TmpFoamBuffer();

                    // Reproject the previous frame's foam buffer
                    int tileC = HDUtils.DivRoundUp((int)currentWater.foamResolution, 8);
                    cmd.SetComputeVectorParam(m_WaterFoamCS, HDShaderIDs._PreviousFoamRegionScaleOffset, currentWater.previousFoamRegionScaleOffset);
                    cmd.SetComputeTextureParam(m_WaterFoamCS, m_ReprojectFoamKernel, HDShaderIDs._WaterFoamBuffer, currentFoamBuffer);
                    cmd.SetComputeTextureParam(m_WaterFoamCS, m_ReprojectFoamKernel, HDShaderIDs._WaterFoamBufferRW, tmpFoamBuffer);
                    cmd.DispatchCompute(m_WaterFoamCS, m_ReprojectFoamKernel, tileC, tileC, 1);

                    // Attenuate the foam buffer
                    cmd.SetComputeTextureParam(m_WaterFoamCS, m_AttenuateFoamKernel, HDShaderIDs._WaterFoamBuffer, tmpFoamBuffer);
                    cmd.SetComputeTextureParam(m_WaterFoamCS, m_AttenuateFoamKernel, HDShaderIDs._WaterFoamBufferRW, currentFoamBuffer);
                    cmd.DispatchCompute(m_WaterFoamCS, m_AttenuateFoamKernel, tileC, tileC, 1);
                    
                    // Update the foam data for the next frame
                    currentWater.previousFoamRegionScaleOffset = new float4(cb._FoamRegionScale, cb._FoamRegionOffset);
                }
                else
                {
                    // Attenuate the foam buffer
                    CoreUtils.SetRenderTarget(cmd, currentFoamBuffer);
                    cmd.DrawProcedural(Matrix4x4.identity, m_FoamMaterial, m_AttenuationPass, MeshTopology.Triangles, 3, 1, currentWater.mpb);
                }

                // Then we render the deformers and the generators
                if (waterDeformers)
                    cmd.DrawProcedural(Matrix4x4.identity, m_FoamMaterial, m_ShoreWaveFoamGenerationPass, MeshTopology.Triangles, 6, m_ActiveWaterDeformers, currentWater.mpb);
                if (foamGenerators)
                    cmd.DrawProcedural(Matrix4x4.identity, m_FoamMaterial, m_OtherFoamGenerationPass, MeshTopology.Triangles, 6, m_ActiveWaterFoamGenerators, currentWater.mpb);
            }
        }
    }
}

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSystem
    {
        const float k_FoamIntensityThreshold = 0.015f;

        const string k_DeformPassName = nameof(WaterDecal.PassType.Deformation);
        const string k_FoamPassName = nameof(WaterDecal.PassType.Foam);
        const string k_SimulationMaskPassName = nameof(WaterDecal.PassType.SimulationMask);
        const string k_LargeCurrentPassName = nameof(WaterDecal.PassType.LargeCurrent);
        const string k_RipplesCurrentPassName = nameof(WaterDecal.PassType.RipplesCurrent);

        bool m_ActiveWaterDecals;
        int m_DecalAtlasSize;
        int m_MaxDecalCount;

        GlobalKeyword horizontalDeformationKeyword; 

        // Buffers used to hold all the water foam generators
        WaterDecalData[] m_WaterDecalDataCPU;
        ComputeBuffer m_WaterDecalData;

        // CPU culling
        Vector4[] m_WaterRegions;
        VisibleDecalData[] m_VisibleDecals;
        int m_NumActiveWaterDecals = 0;
        float m_MaxWaterDeformation = 0;
        bool m_ActiveFoam;
        internal bool m_ActiveMask, m_ActiveDeformation;
        internal bool m_ActiveLargeCurrent, m_ActiveRipplesCurrent;

        internal bool HasActiveFoam() => m_ActiveFoam || (m_MaxInjectedFoamIntensity > k_FoamIntensityThreshold);

        // Deformation
        ComputeShader m_WaterDeformationCS;
        int m_FilterDeformationKernel;
        int m_EvaluateDeformationSurfaceGradientKernel;

        // Foam
        ComputeShader m_WaterFoamCS;
        int m_ReprojectFoamKernel;
        int m_AttenuateFoamKernel;

        // Decals
        Material m_DecalMaterial;
        int m_DeformationDecalPass;
        int m_FoamDecalPass;
        int m_MaskDecalPass;
        int m_LargeCurrentDecalPass;
        int m_RipplesCurrentDecalPass;
        int m_AttenuationPass;

        // Keeps track of maximum possible foam intensity in to estimate when there is no more foam
        float m_MaxInjectedFoamIntensity = 0.0f;

        // Atlas used to hold the various decal output
        // R: Deformation, G: Surface Foam, B: Deep Foam
        // RGB: Simulation Mask for each band, A: Simulation Foam Mask
        // RG: Large Current Direction, B: Large Current Influence
        // RG: Ripples Current Direction, B: Ripples Current Influence
        // Note: deformation and foam are rendered together cause they often share intermediate computations
        PowerOfTwoTextureAtlas m_DecalAtlas;

        void InitializeWaterDecals()
        {
            m_ActiveWaterDecals = m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.supportWaterDecals;
            if (!m_ActiveWaterDecals)
                return;

            horizontalDeformationKeyword = GlobalKeyword.Create("HORIZONTAL_DEFORMATION");

            m_DecalAtlasSize = (int)m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.waterDecalAtlasSize;
            m_MaxDecalCount = m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.maximumWaterDecalCount;

            m_WaterRegions = new Vector4[k_MaxNumWaterSurfaceProfiles];
            m_VisibleDecals = new VisibleDecalData[m_MaxDecalCount];

            m_WaterDecalDataCPU = new WaterDecalData[m_MaxDecalCount];
            m_WaterDecalData = new ComputeBuffer(m_MaxDecalCount, System.Runtime.InteropServices.Marshal.SizeOf<WaterDecalData>());

            m_DecalAtlas = new PowerOfTwoTextureAtlas(m_DecalAtlasSize, 0, GraphicsFormat.R16G16B16A16_SNorm, name: "Water Decal Atlas", useMipMap: false);

            // Decals
            m_DecalMaterial = CoreUtils.CreateEngineMaterial(m_RuntimeResources.waterDecalPS);
            m_DeformationDecalPass = m_DecalMaterial.FindPass("DeformationDecal");
            m_FoamDecalPass = m_DecalMaterial.FindPass("FoamDecal");
            m_MaskDecalPass = m_DecalMaterial.FindPass("MaskDecal");
            m_LargeCurrentDecalPass = m_DecalMaterial.FindPass("LargeCurrentDecal");
            m_RipplesCurrentDecalPass = m_DecalMaterial.FindPass("RipplesCurrentDecal");
            m_AttenuationPass = m_DecalMaterial.FindPass("FoamAttenuation");

            // Deformation
            m_WaterDeformationCS = m_RuntimeResources.waterDeformationCS;
            m_FilterDeformationKernel = m_WaterDeformationCS.FindKernel("FilterDeformation");
            m_EvaluateDeformationSurfaceGradientKernel = m_WaterDeformationCS.FindKernel("EvaluateDeformationSurfaceGradient");

            // Foam
            m_WaterFoamCS = m_RuntimeResources.waterFoamCS;
            m_ReprojectFoamKernel = m_WaterFoamCS.FindKernel("ReprojectFoam");
            m_AttenuateFoamKernel = m_WaterFoamCS.FindKernel("AttenuateFoam");
        }

        void ReleaseWaterDecals()
        {
            if (!m_ActiveWaterDecals)
                return;

            CoreUtils.SafeRelease(m_WaterDecalData);
            CoreUtils.Destroy(m_DecalMaterial);
            m_DecalAtlas.Release();
        }

        #if UNITY_EDITOR
        internal void UpdateWaterDecalAtlas(Shader shader)
        {
            // When a water decal shadergraph is saved, update all decals that are affected
            foreach (var decal in WaterDecal.instances)
            {
                if (decal.material != null && decal.material.shader == shader)
                    decal.RequestUpdate();
            }
        }
        #endif

        static internal bool IsAffectingProperty(WaterDecal decal, int nameId)
        {
            if (decal.material.HasProperty(nameId))
                return decal.material.GetFloat(nameId) != 0.0f;
            return false;
        }

        struct VisibleDecalData
        {
            public WaterDecal decal;
            public int materialId;

            public readonly int resX => decal.resolution.x;
            public readonly int resY => decal.resolution.y;
            public int MaterialId(WaterDecal.PassType passType)
            {
                int hash = 17;
                hash = hash * 23 + (int)passType;
                hash = hash * 23 + materialId;
                return hash;
            }
        }

        bool CullWaterDecals()
        {
            // Update decal regions based on camera position
            Transform anchor = Camera.main != null ? Camera.main.transform : null;
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                anchor = UnityEditor.SceneView.lastActiveSceneView?.camera.transform;
            #endif

            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = Mathf.Min(WaterSurface.instanceCount, k_MaxNumWaterSurfaceProfiles);
            for (int i = 0; i < numWaterSurfaces; ++i)
            {
                waterSurfaces[i].UpdateDecalRegion(anchor);
                waterSurfaces[i].GetDecalRegion(out var decalRegionCenter, out var decalRegionSize);
                m_WaterRegions[i] = new Vector4(decalRegionCenter.x, decalRegionCenter.y, decalRegionSize.x * 0.5f, decalRegionSize.y * 0.5f);
            }

            // Reset counters
            m_NumActiveWaterDecals = 0;
            m_MaxWaterDeformation = 0.0f;
            m_ActiveMask = m_ActiveDeformation = m_ActiveFoam = false;
            m_ActiveLargeCurrent = m_ActiveRipplesCurrent = false;

            // Reserve slot in atlas for decals affecting at least one surface
            bool needRelayout = false;
            foreach (var decal in WaterDecal.instances)
            {
                if (!decal.IsValidMaterial())
                    continue;

                float3 scale = decal.effectiveScale;
                Vector3 posWS = decal.transform.position;
                float2 size = (float2)decal.regionSize * 0.5f * scale.xz;
                bool visible = false;

                bool affectDeformation = IsAffectingProperty(decal, HDShaderIDs._AffectDeformation);
                bool affectFoam = IsAffectingProperty(decal, HDShaderIDs._AffectsFoam);
                bool affectMask = IsAffectingProperty(decal, HDShaderIDs._AffectsSimulationMask);
                bool affectLarge = IsAffectingProperty(decal, HDShaderIDs._AffectsLargeCurrent);
                bool affectRipples = IsAffectingProperty(decal, HDShaderIDs._AffectsRipplesCurrent);

                // Decals can be rotated, use a bounding circle to simplify computations
                float radiusSquare = size.x * size.x + size.y * size.y;
                for (int i = 0; i < numWaterSurfaces; i++)
                {
                    float distX = Mathf.Abs(posWS.x - m_WaterRegions[i].x) - m_WaterRegions[i].z;
                    float distY = Mathf.Abs(posWS.z - m_WaterRegions[i].y) - m_WaterRegions[i].w;

                    if (distX > 0.0f && distX*distX > radiusSquare) continue;
                    if (distY > 0.0f && distY*distY > radiusSquare) continue;

                    visible |= waterSurfaces[i].deformation && affectDeformation;
                    visible |= waterSurfaces[i].foam && affectFoam;
                    if (m_EnableDecalWorkflow)
                    {
                        visible |= (waterSurfaces[i].simulationMask || waterSurfaces[i].supportSimulationFoamMask) && affectMask;

                        if (waterSurfaces[i].surfaceType == WaterSurfaceType.Pool)
                        {
                            visible |= waterSurfaces[i].supportRipplesCurrent && affectRipples;
                        }
                        else
                        {
                            visible |= waterSurfaces[i].supportLargeCurrent && affectLarge;
                            visible |= waterSurfaces[i].UsesRipplesCurrent() && affectRipples;
                        }
                    }

                    if (visible)
                        break;
                }

                // If the decal is visible, prepare GPU data and mark atlas slot as used
                if (visible)
                {
                    if (m_NumActiveWaterDecals >= m_MaxDecalCount)
                    {
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("Maximum amount of visible Water Decals reached. Some of them will be ignored.");
                        #endif
                        break;
                    }

                    ref var cpuData = ref m_VisibleDecals[m_NumActiveWaterDecals];
                    cpuData.materialId = decal.GetMaterialAtlasingId();
                    cpuData.decal = decal;

                    ref var gpuData = ref m_WaterDecalDataCPU[m_NumActiveWaterDecals];

                    float angle = -decal.transform.eulerAngles.y * Mathf.Deg2Rad;
                    gpuData.positionXZ.Set(posWS.x, posWS.z);
                    gpuData.forwardXZ.Set(Mathf.Cos(angle), Mathf.Sin(angle));
                    gpuData.regionSize = 2.0f * size;

                    gpuData.amplitude = decal.amplitude * scale.y;
                    gpuData.surfaceFoamDimmer = decal.surfaceFoamDimmer;
                    gpuData.deepFoamDimmer = decal.deepFoamDimmer;

                    if (decal.updateMode == CustomRenderTextureUpdateMode.Realtime)
                        decal.updateCount++;

                    bool ReserveAtlasSpace(WaterDecal.PassType passType, in VisibleDecalData cpuData) => m_DecalAtlas.ReserveSpace(cpuData.MaterialId(passType), cpuData.resX, cpuData.resY);

                    if (affectDeformation && !ReserveAtlasSpace(WaterDecal.PassType.Deformation, in cpuData))
                        needRelayout = true;
                    if (affectFoam && !ReserveAtlasSpace(WaterDecal.PassType.Foam, in cpuData))
                        needRelayout = true;
                    if (affectMask && !ReserveAtlasSpace(WaterDecal.PassType.SimulationMask, in cpuData))
                        needRelayout = true;
                    if (affectLarge && !ReserveAtlasSpace(WaterDecal.PassType.LargeCurrent, in cpuData))
                        needRelayout = true;
                    if (affectRipples && !ReserveAtlasSpace(WaterDecal.PassType.RipplesCurrent, in cpuData))
                        needRelayout = true;

                    m_NumActiveWaterDecals++;
                    m_ActiveMask |= affectMask;
                    m_ActiveDeformation |= affectDeformation;
                    m_ActiveFoam |= affectFoam;
                    m_ActiveLargeCurrent |= affectLarge;
                    m_ActiveRipplesCurrent |= affectRipples;
                    if (affectDeformation)
                        m_MaxWaterDeformation = Mathf.Max(m_MaxWaterDeformation, Mathf.Abs(decal.amplitude));
                }
            }

            // Relayout if needed
            if (needRelayout && !m_DecalAtlas.RelayoutEntries())
            {
                Debug.LogError($"No more space in the Water Decal Atlas. To solve this issue, increase the resolution of the atlas in the current HDRP asset.");
                m_NumActiveWaterDecals = 0;
                m_ActiveMask = m_ActiveDeformation = m_ActiveFoam = false;
                m_ActiveLargeCurrent = m_ActiveRipplesCurrent = false;
                return false;
            }
            return true;
        }

        void ProcessWaterDecals(CommandBuffer cmd)
        {
            // Render decals in atlas if needed
            void FetchCoords(in VisibleDecalData cpuData, WaterDecal.PassType passType, string passName, ref Vector4 scaleBias)
            {
                int id = cpuData.MaterialId(passType);
                if (!m_DecalAtlas.IsCached(out scaleBias, id))
                {
                    // Used in WaterDecal.shader to discard decal
                    scaleBias.x = -1;
                }
                else if (m_DecalAtlas.NeedsUpdate(id, cpuData.decal.updateCount, false))
                {
                    // It would be nice to somehow cache these
                    int pass = cpuData.decal.material.FindPass(passName);

                    cmd.SetRenderTarget(m_DecalAtlas.AtlasTexture);
                    cmd.SetViewport(new Rect(scaleBias.z * m_DecalAtlasSize, scaleBias.w * m_DecalAtlasSize, scaleBias.x * m_DecalAtlasSize, scaleBias.y * m_DecalAtlasSize));
                    cmd.DrawProcedural(Matrix4x4.identity, cpuData.decal.material, pass, MeshTopology.Triangles, 3, 1, cpuData.decal.mpb);
                }
            }

            for (int i = 0; i < m_NumActiveWaterDecals; ++i)
            {
                ref var cpuData = ref m_VisibleDecals[i];
                ref var gpuData = ref m_WaterDecalDataCPU[i];

                // Note: we could have all of these in the same pass like for regular SG decals since they render to the same atlas
                FetchCoords(in cpuData, WaterDecal.PassType.Deformation, k_DeformPassName, ref gpuData.deformScaleOffset);
                FetchCoords(in cpuData, WaterDecal.PassType.Foam, k_FoamPassName, ref gpuData.foamScaleOffset);
                FetchCoords(in cpuData, WaterDecal.PassType.SimulationMask, k_SimulationMaskPassName, ref gpuData.maskScaleOffset);
                FetchCoords(in cpuData, WaterDecal.PassType.LargeCurrent, k_LargeCurrentPassName, ref gpuData.largeCurrentScaleOffset);
                FetchCoords(in cpuData, WaterDecal.PassType.RipplesCurrent, k_RipplesCurrentPassName, ref gpuData.ripplesCurrentScaleOffset);
            }

            m_DecalAtlas.ResetRequestedTexture();
        }

        void UpdateWaterDecalData(CommandBuffer cmd)
        {
            if (!m_ActiveWaterDecals)
                return;

            if (CullWaterDecals())
                ProcessWaterDecals(cmd);
            m_WaterDecalData.SetData(m_WaterDecalDataCPU);
            cmd.SetGlobalBuffer(HDShaderIDs._WaterDecalData, m_WaterDecalData);
            cmd.SetGlobalTexture(HDShaderIDs._WaterDecalAtlas, m_DecalAtlas.AtlasTexture);
        }

        void UpdateWaterDecals(CommandBuffer cmd, WaterSurface currentWater)
        {
            if (!m_ActiveWaterDecals)
                return;

            var perSurfaceCB = m_ShaderVariablesWaterPerSurface[currentWater.surfaceIndex];
            currentWater.mpb.SetConstantBuffer(HDShaderIDs._ShaderVariablesWaterPerSurface, perSurfaceCB, 0, perSurfaceCB.stride);
            currentWater.CheckDeformationResources(HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportWaterHorizontalDeformation);
            currentWater.CheckFoamResources(cmd);
            currentWater.CheckMaskResources();
            currentWater.CheckCurrentResources();

            // Needed to see decals in wireframe mode
            bool wireframe = GL.wireframe;
            if (wireframe)
                cmd.SetWireframe(false);

            if (currentWater.foam)
            {
                ref var cb = ref m_ShaderVariablesPerSurfaceArray[currentWater.surfaceIndex];

                // Track if reprojected foam is still visible even when no generators are alive
                bool activeFoam = HasActiveFoam();
                if (m_ActiveFoam)
                    m_MaxInjectedFoamIntensity = 1.0f;
                else if (activeFoam)
                {
                    // Attenuation formula must be in sync with WaterDecal.shader
                    m_MaxInjectedFoamIntensity *= Mathf.Exp(-cb._DeltaTime * cb._FoamPersistenceMultiplier * 0.5f);
                }

                if (activeFoam)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterDecalFoam)))
                    {
                        RTHandle currentFoamBuffer = currentWater.FoamBuffer();

                        // Check if we need to reproj
                        if (currentWater.previousFoamRegionScaleOffset.x != cb._DecalRegionScale.x ||
                            currentWater.previousFoamRegionScaleOffset.y != cb._DecalRegionScale.y ||
                            currentWater.previousFoamRegionScaleOffset.z != cb._DecalRegionOffset.x ||
                            currentWater.previousFoamRegionScaleOffset.w != cb._DecalRegionOffset.y)
                        {
                            RTHandle tmpFoamBuffer = currentWater.TmpFoamBuffer();
                            BindPerSurfaceConstantBuffer(cmd, m_WaterFoamCS, perSurfaceCB);

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
                            currentWater.previousFoamRegionScaleOffset = new float4(cb._DecalRegionScale, cb._DecalRegionOffset);
                        }
                        else
                        {
                            // Attenuate the foam buffer
                            CoreUtils.SetRenderTarget(cmd, currentFoamBuffer);
                            cmd.DrawProcedural(Matrix4x4.identity, m_DecalMaterial, m_AttenuationPass, MeshTopology.Triangles, 3, 1, currentWater.mpb);
                        }

                        cmd.DrawProcedural(Matrix4x4.identity, m_DecalMaterial, m_FoamDecalPass, MeshTopology.Quads, 4, m_NumActiveWaterDecals, currentWater.mpb);
                    }
                }
            }

            if (currentWater.deformation && m_ActiveDeformation)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterDecalDeformation)))
                {
                    // Bind the constant buffers
                    BindPerSurfaceConstantBuffer(cmd, m_WaterDeformationCS, perSurfaceCB);

                    // Clear the render target to black and draw all the deformers to the texture
                    // We render in the SG buffer, blur pass will then output to deformation buffer
                    CoreUtils.SetRenderTarget(cmd, currentWater.deformationSGBuffer, clearFlag: ClearFlag.Color, Color.clear);
                    cmd.DrawProcedural(Matrix4x4.identity, m_DecalMaterial, m_DeformationDecalPass, MeshTopology.Quads, 4, m_NumActiveWaterDecals, currentWater.mpb);
                    currentWater.deformationBuffer.rt.IncrementUpdateCount(); // For the CPU Simulation

                    // Evaluate the normals
                    int numTiles = HDUtils.DivRoundUp((int)currentWater.deformationRes, 8);

                    cmd.SetKeyword(horizontalDeformationKeyword, HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportWaterHorizontalDeformation);
                 
                    // First we need to clear the edge pixel and blur the deformation a bit
                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_FilterDeformationKernel, HDShaderIDs._WaterDeformationBuffer, currentWater.deformationSGBuffer);
                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_FilterDeformationKernel, HDShaderIDs._WaterDeformationBufferRW, currentWater.deformationBuffer);
                    cmd.DispatchCompute(m_WaterDeformationCS, m_FilterDeformationKernel, numTiles, numTiles, 1);

                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_EvaluateDeformationSurfaceGradientKernel, HDShaderIDs._WaterDeformationBuffer, currentWater.deformationBuffer);
                    cmd.SetComputeTextureParam(m_WaterDeformationCS, m_EvaluateDeformationSurfaceGradientKernel, HDShaderIDs._WaterDeformationSGBufferRW, currentWater.deformationSGBuffer);
                    cmd.DispatchCompute(m_WaterDeformationCS, m_EvaluateDeformationSurfaceGradientKernel, numTiles, numTiles, 1);
                }
            }

            if (m_EnableDecalWorkflow)
            {
                if ((currentWater.simulationMask || currentWater.supportSimulationFoamMask) && m_ActiveMask)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterDecalMask)))
                    {
                        CoreUtils.SetRenderTarget(cmd, currentWater.maskBuffer, clearFlag: ClearFlag.Color, Color.white);

                        cmd.DrawProcedural(Matrix4x4.identity, m_DecalMaterial, m_MaskDecalPass, MeshTopology.Quads, 4, m_NumActiveWaterDecals, currentWater.mpb);
                        currentWater.maskBuffer.rt.IncrementUpdateCount(); // For the CPU Simulation
                    }
                }

                if (m_ActiveLargeCurrent || m_ActiveRipplesCurrent)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterDecalCurrent)))
                    {
                        if (currentWater.supportLargeCurrent && m_ActiveLargeCurrent)
                        {
                            CoreUtils.SetRenderTarget(cmd, currentWater.largeCurrentBuffer, clearFlag: ClearFlag.Color, Color.black);

                            cmd.DrawProcedural(Matrix4x4.identity, m_DecalMaterial, m_LargeCurrentDecalPass, MeshTopology.Quads, 4, m_NumActiveWaterDecals, currentWater.mpb);
                            currentWater.largeCurrentBuffer.rt.IncrementUpdateCount(); // For the CPU Simulation
                        }
                        if (currentWater.supportRipplesCurrent && m_ActiveRipplesCurrent)
                        {
                            CoreUtils.SetRenderTarget(cmd, currentWater.ripplesCurrentBuffer, clearFlag: ClearFlag.Color, Color.black);

                            cmd.DrawProcedural(Matrix4x4.identity, m_DecalMaterial, m_RipplesCurrentDecalPass, MeshTopology.Quads, 4, m_NumActiveWaterDecals, currentWater.mpb);
                            currentWater.ripplesCurrentBuffer.rt.IncrementUpdateCount(); // For the CPU Simulation
                        }
                    }
                }
            }

            // Reenable wireframe if needed
            if (wireframe)
                cmd.SetWireframe(true);
        }
    }
}

using System.Collections.Generic;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Flag that allows us to track if the water system is currently active
        bool m_ActiveWaterSystem = false;

        // Rendering kernels
        ComputeShader m_WaterLightingCS;
        int m_WaterClassifyTilesKernel;
        int m_WaterPrepareSSRIndirectKernel;
        int m_WaterClearIndirectKernel;
        int[] m_WaterIndirectDeferredKernels = new int[WaterConsts.k_NumWaterVariants];
        int m_WaterDeferredKernel;
        int m_WaterFogIndirectKernel;

        // Water evaluation
        ComputeShader m_WaterEvaluationCS;
        int m_FindVerticalDisplacementsKernel;

        // The shader passes used to render the water
        Material m_InternalWaterMaterial;
        Mesh m_GridMesh, m_RingMesh, m_RingMeshLow;
        GraphicsBuffer m_WaterIndirectDispatchBuffer;
        GraphicsBuffer m_WaterPatchDataBuffer;
        GraphicsBuffer m_WaterCameraFrustrumBuffer;
        FrustumGPU[] m_WaterCameraFrustumCPU = new FrustumGPU[1];

        internal const string k_WaterGBufferPass = "WaterGBuffer";
        internal const string k_WaterMaskPass = "WaterMask";
        internal const string k_LowResGBufferPass = "LowRes";
        internal const string k_TessellationPass = "Tessellation";
        readonly static string[] k_PassesGBuffer = new string[] { k_WaterGBufferPass, k_LowResGBufferPass };
        readonly static string[] k_PassesGBufferTessellation = new string[] { k_WaterGBufferPass + k_TessellationPass, k_LowResGBufferPass };
        readonly static string[] k_PassesWaterMask = new string[] { k_WaterMaskPass, k_WaterMaskPass + k_LowResGBufferPass };


        // Other internal rendering data
        MaterialPropertyBlock m_WaterMaterialPropertyBlock;
        ShaderVariablesWater m_ShaderVariablesWater = new ShaderVariablesWater();

        // Handles the water profiles
        const int k_MaxNumWaterSurfaceProfiles = 16;
        WaterSurfaceProfile[] m_WaterSurfaceProfileArray = new WaterSurfaceProfile[k_MaxNumWaterSurfaceProfiles];
        GraphicsBuffer m_WaterProfileArrayGPU;

        // Caustics data
        GraphicsBuffer m_CausticsGeometry;
        bool m_CausticsBufferGeometryInitialized;
        Material m_CausticsMaterial;

        // Water line and under water
        GraphicsBuffer m_WaterCameraHeightBuffer;

        // Local Currents
        Texture2D m_WaterSectorData;

        // Ambient probe evaluation for the water
        Vector4 m_WaterAmbientProbe;

        void InitializeWaterSystem()
        {
            m_ActiveWaterSystem = m_Asset.currentPlatformRenderPipelineSettings.supportWater;

            // These buffers are needed even when water is disabled
            m_DefaultWaterLineBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 3, sizeof(uint));
            m_DefaultWaterLineBuffer.SetData(new uint[] { 0xFFFFFFFF, 0, 2 });

            m_WaterProfileArrayGPU = new GraphicsBuffer(GraphicsBuffer.Target.Structured, k_MaxNumWaterSurfaceProfiles, System.Runtime.InteropServices.Marshal.SizeOf<WaterSurfaceProfile>());

            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_ActiveWaterSystem)
                return;

            // Water simulation
            InitializeWaterSimulation();

            // Water rendering
            m_WaterLightingCS = m_Asset.renderPipelineResources.shaders.waterLightingCS;
            m_WaterPrepareSSRIndirectKernel = m_WaterLightingCS.FindKernel("PrepareSSRIndirect");
            m_WaterClearIndirectKernel = m_WaterLightingCS.FindKernel("WaterClearIndirect");
            m_WaterClassifyTilesKernel = m_WaterLightingCS.FindKernel("WaterClassifyTiles");
            m_WaterIndirectDeferredKernels[0] = m_WaterLightingCS.FindKernel("WaterDeferredLighting_Variant0");
            m_WaterIndirectDeferredKernels[1] = m_WaterLightingCS.FindKernel("WaterDeferredLighting_Variant1");
            m_WaterIndirectDeferredKernels[2] = m_WaterLightingCS.FindKernel("WaterDeferredLighting_Variant2");
            m_WaterIndirectDeferredKernels[3] = m_WaterLightingCS.FindKernel("WaterDeferredLighting_Variant3");
            m_WaterIndirectDeferredKernels[4] = m_WaterLightingCS.FindKernel("WaterDeferredLighting_Variant4");
            m_WaterDeferredKernel = m_WaterLightingCS.FindKernel("WaterDeferredLighting");
            m_WaterFogIndirectKernel = m_WaterLightingCS.FindKernel("WaterFogIndirect");

            // Water evaluation
            m_WaterEvaluationCS = m_Asset.renderPipelineResources.shaders.waterEvaluationCS;
            m_FindVerticalDisplacementsKernel = m_WaterEvaluationCS.FindKernel("FindVerticalDisplacements");

            // Allocate the additional rendering data
            m_WaterMaterialPropertyBlock = new MaterialPropertyBlock();
            m_InternalWaterMaterial = defaultResources.materials.waterMaterial;
            InitializeInstancingData();

            // Create the caustics water geometry
            m_CausticsGeometry = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.Index, WaterConsts.k_WaterCausticsMeshNumQuads * 6, sizeof(int));
            m_CausticsBufferGeometryInitialized = false;
            m_CausticsMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.waterCausticsPS);

            // Waterline / Underwater
            // TODO: This should be entirely dynamic and depend on M_MaxViewCount
            m_WaterCameraHeightBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2 * 4, sizeof(float));

            // Make sure the under water surface index is invalidated
            m_UnderWaterSurfaceIndex = -1;

            // Make sure the base mesh is built
            BuildGridMeshes(ref m_GridMesh, ref m_RingMesh, ref m_RingMeshLow);

            // Water deformers initialization
            InitializeWaterDeformers();

            // Under water resources
            InitializeUnderWaterResources();

            // Faom resources
            InitializeWaterFoam();
        }

        void ReleaseWaterSystem()
        {
            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // Loop through them and display them
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                WaterSurface waterSurface = waterSurfaces[surfaceIdx];
                waterSurface.ReleaseResources();
            }

            // Release the default water line array
            CoreUtils.SafeRelease(m_DefaultWaterLineBuffer);

            // Release the water profile array
            CoreUtils.SafeRelease(m_WaterProfileArrayGPU);

            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_ActiveWaterSystem)
                return;

            // Faom resources
            ReleaseWaterFoam();

            // Water deformers release
            ReleaseWaterDeformers();

            // Make sure the CPU simulation stuff is properly freed
            ReleaseCPUWaterSimulation();

            // Release the waterline underwater data
            CoreUtils.SafeRelease(m_WaterCameraHeightBuffer);

            // Release the caustics geometry
            CoreUtils.Destroy(m_CausticsMaterial);
            CoreUtils.SafeRelease(m_CausticsGeometry);

            // Water rendering resources
            CoreUtils.SafeRelease(m_WaterCameraFrustrumBuffer);
            CoreUtils.SafeRelease(m_WaterPatchDataBuffer);
            CoreUtils.SafeRelease(m_WaterIndirectDispatchBuffer);

            // Simulation resources
            ReleaseWaterSimulation();

            // Free the meshes
            m_GridMesh = m_RingMesh = m_RingMeshLow = null;
        }

        void InitializeInstancingData()
        {
            // Allocate the indirect instancing buffer
            m_WaterIndirectDispatchBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, sizeof(int));

            // Initialize the parts of the buffer with valid values
            uint meshResolution = WaterConsts.k_WaterTessellatedMeshResolution;
            uint quadCount = 2 * ((meshResolution - meshResolution / 4) * (meshResolution / 4 - 1) + meshResolution / 4);
            uint triCount = quadCount + 3 * meshResolution / 2;

            uint[] indirectBufferCPU = new uint[5];
            indirectBufferCPU[0] = triCount * 3;

            // Push the values to the GPU
            m_WaterIndirectDispatchBuffer.SetData(indirectBufferCPU);

            // Allocate the per instance data
            m_WaterPatchDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 7 * 7, sizeof(float) * 2);

            // Allocate the frustum buffer
            m_WaterCameraFrustrumBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(FrustumGPU)));
        }

        void CheckWaterCurrentData()
        {
            if (m_WaterSectorData == null)
            {
                m_WaterSectorData = new Texture2D(16, 1, TextureFormat.RGBAFloat, -1, true);
                m_WaterSectorData.SetPixelData(WaterConsts.k_SectorSwizzlePacked, 0, 0);
                m_WaterSectorData.Apply();
            }
        }

        void UpdateShaderVariablesWater(WaterSurface currentWater, int surfaceIndex, ref ShaderVariablesWater cb)
        {
            // Resolution at which the simulation is evaluated
            cb._BandResolution = (uint)m_WaterBandResolution;

            // Per patch data
            cb._PatchGroup = currentWater.simulation.spectrum.patchGroup;
            cb._PatchOrientation = currentWater.simulation.spectrum.patchOrientation * Mathf.Deg2Rad;
            cb._PatchWindSpeed = currentWater.simulation.spectrum.patchWindSpeed;
            cb._PatchDirectionDampener = currentWater.simulation.spectrum.patchWindDirDampener;

            void PackBandData(WaterSimulationResources simulation, int bandIdx, out Vector4 scaleOffsetAmplitude, out float2 fade)
            {
                float invPatchSize = 1.0f / simulation.spectrum.patchSizes[bandIdx];
                float orientation = simulation.spectrum.patchOrientation[bandIdx] * Mathf.Deg2Rad;
                float bandScale = simulation.rendering.patchCurrentSpeed[bandIdx] * simulation.simulationTime * invPatchSize;
                scaleOffsetAmplitude = new Vector4(invPatchSize, Mathf.Cos(orientation) * bandScale, Mathf.Sin(orientation) * bandScale, simulation.rendering.patchAmplitudeMultiplier[bandIdx]);
                fade = new float2(simulation.rendering.patchFadeA[bandIdx], simulation.rendering.patchFadeB[bandIdx]);
            }

            PackBandData(currentWater.simulation, 0, out cb._Band0_ScaleOffset_AmplitudeMultiplier, out cb._Band0_Fade);
            PackBandData(currentWater.simulation, 1, out cb._Band1_ScaleOffset_AmplitudeMultiplier, out cb._Band1_Fade);
            PackBandData(currentWater.simulation, 2, out cb._Band2_ScaleOffset_AmplitudeMultiplier, out cb._Band2_Fade);

            cb._GroupOrientation = currentWater.simulation.spectrum.groupOrientation * Mathf.Deg2Rad;

            // Current simulation time
            cb._SimulationTime = currentWater.simulation.simulationTime;

            // Max wave height for the system
            float patchAmplitude = EvaluateMaxAmplitude(currentWater.simulation.spectrum.patchSizes.x, cb._PatchWindSpeed.x * WaterConsts.k_MeterPerSecondToKilometerPerHour);
            cb._MaxWaveHeight = patchAmplitude;
            cb._ScatteringWaveHeight = Mathf.Max(cb._MaxWaveHeight * WaterConsts.k_ScatteringRange, WaterConsts.k_MinScatteringAmplitude) + currentWater.maximumHeightOverride;

            // Horizontal displacement due to each band
            cb._MaxWaveDisplacement = cb._MaxWaveHeight * WaterConsts.k_WaterMaxChoppinessValue;

            // Water smoothness
            cb._WaterSmoothness = currentWater.startSmoothness;

            // Foam Jacobian offset depends on the number of bands
            if (currentWater.simulation.numActiveBands == 3)
                cb._SimulationFoamAmount = 12.0f * Mathf.Pow(0.8f + currentWater.simulationFoamAmount * 0.28f, 0.25f);
            else if (currentWater.simulation.numActiveBands == 2)
                cb._SimulationFoamAmount = 8.0f * Mathf.Pow(0.72f + currentWater.simulationFoamAmount * 0.28f, 0.25f);
            else
                cb._SimulationFoamAmount = 4.0f * Mathf.Pow(0.72f + currentWater.simulationFoamAmount * 0.28f, 0.25f);

            // Smoothness of the foam
            cb._FoamPersistenceMultiplier = 1.0f / Mathf.Lerp(0.25f, 2.0f, currentWater.foamPersistenceMultiplier);
            cb._FoamSmoothness = currentWater.foamSmoothness;
            cb._FoamTiling = currentWater.foamTextureTiling;

            // We currently only support properly up to 16 unique water surfaces
            cb._SurfaceIndex = surfaceIndex & 0xF;

            cb._ScatteringColorTips = currentWater.scatteringColor; /*alpha is unsused*/
            cb._DeltaTime = currentWater.timeMultiplier == 0.0f ? 1.0f : currentWater.simulation.deltaTime; // This is set to 1 when time is disabled to see the foam generators

            cb._MaxRefractionDistance = Mathf.Min(currentWater.absorptionDistance, currentWater.maxRefractionDistance);

            cb._OutScatteringCoefficient = -Mathf.Log(0.02f) / currentWater.absorptionDistance;
            cb._TransparencyColor = new Vector4(Mathf.Min(currentWater.refractionColor.r, 0.99f), Mathf.Min(currentWater.refractionColor.g, 0.99f), Mathf.Min(currentWater.refractionColor.b, 0.99f));
            cb._WaterUpDirection = new float4(currentWater.UpVector(), 1.0f);

            cb._AmbientScattering = currentWater.ambientScattering;
            cb._HeightBasedScattering = currentWater.heightScattering;
            cb._DisplacementScattering = currentWater.displacementScattering;

            // Foam
            var simulationFoamWindAttenuation = Mathf.Clamp(currentWater.simulationFoamWindCurve.Evaluate(currentWater.simulation.spectrum.patchWindSpeed.x / WaterConsts.k_SwellMaximumWindSpeedMpS), 0.0f, 1.0f);
            cb._FoamRegionOffset = currentWater.foamAreaOffset + new Vector2(currentWater.transform.position.x, currentWater.transform.position.z);
            cb._FoamRegionScale.Set(1.0f / currentWater.foamAreaSize.x, 1.0f / currentWater.foamAreaSize.y);
            cb._SimulationFoamIntensity = m_ActiveWaterFoam && currentWater.HasSimulationFoam() ? simulationFoamWindAttenuation : 0.0f;
            cb._SimulationFoamMaskOffset = currentWater.simulationFoamMaskOffset;
            cb._SimulationFoamMaskScale.Set(1.0f / currentWater.simulationFoamMaskExtent.x, 1.0f / currentWater.simulationFoamMaskExtent.y);
            cb._WaterFoamRegionResolution = currentWater.foam ? (int)currentWater.foamResolution : 0;

            // Water Mask
            cb._WaterMaskOffset = currentWater.waterMaskOffset;
            cb._WaterMaskScale.Set(1.0f / currentWater.waterMaskExtent.x, 1.0f / currentWater.waterMaskExtent.y);
            cb._WaterMaskRemap.Set(currentWater.waterMaskRemap.x, currentWater.waterMaskRemap.y - currentWater.waterMaskRemap.x);

            // Caustics
            cb._CausticsBandIndex = SanitizeCausticsBand(currentWater.causticsBand, currentWater.simulation.numActiveBands);
            cb._CausticsRegionSize = currentWater.simulation.spectrum.patchSizes[cb._CausticsBandIndex];

            // Deformation
            cb._WaterDeformationCenter = currentWater.deformationAreaOffset + new Vector2(currentWater.transform.position.x, currentWater.transform.position.z);
            cb._WaterDeformationExtent = currentWater.deformation ? currentWater.deformationAreaSize : new Vector2(-1, -1);
            cb._WaterDeformationResolution = (int)currentWater.deformationRes;
        }

        static float EvaluateCausticsMaxLOD(WaterSurface.WaterCausticsResolution resolution)
        {
            switch (resolution)
            {
                case WaterSurface.WaterCausticsResolution.Caustics256:
                    return 2.0f;
                case WaterSurface.WaterCausticsResolution.Caustics512:
                    return 3.0f;
                case WaterSurface.WaterCausticsResolution.Caustics1024:
                    return 4.0f;
            }
            return 2.0f;
        }

        /// <summary>
        /// Computes the screen space size of an edge after camera projection
        /// </summary>
        /// <param name="hdCamera">camera</param>
        /// <param name="cameraDistance">the distance between the edge and the camera</param>
        /// <param name="sizeWS">the size of the edge in world space</param>
        /// <returns>The screen space size</returns>
        float ComputeScreenSpaceSize(HDCamera hdCamera, float cameraDistance, float sizeWS)
        {
            float4 positionWS = new float4(sizeWS, 0.0f, cameraDistance, 1.0f);
            float4 positionCS = math.mul(hdCamera.mainViewConstants.nonJitteredProjMatrix, positionWS);
            return math.abs(positionCS.x / positionCS.w) * 0.5f * hdCamera.actualWidth;
        }

        void UpdateShaderVariablesWaterRendering(WaterSurface currentWater, HDCamera hdCamera, WaterRendering settings,
                                                bool instancedQuads, bool infinite, bool customMesh,
                                                float maxWaveDisplacement, float maxWaveHeight,
                                                ref ShaderVariablesWaterRendering cb)
        {
            float3 waterPosition = currentWater.transform.position;
            float2 extent = currentWater.IsProceduralGeometry() ? new Vector2(Mathf.Abs(currentWater.transform.lossyScale.x), Mathf.Abs(currentWater.transform.lossyScale.z)) : Vector2.one;

            cb._CausticsIntensity = currentWater.caustics ? currentWater.causticsIntensity : 0.0f;
            cb._CausticsShadowIntensity = currentWater.causticsDirectionalShadow ? currentWater.causticsDirectionalShadowDimmer : 1.0f;
            cb._CausticsPlaneBlendDistance = currentWater.causticsPlaneBlendDistance;
            cb._CausticsMaxLOD = EvaluateCausticsMaxLOD(currentWater.causticsResolution);
            cb._CausticsTilingFactor = 1.0f / currentWater.causticsTilingFactor;

            cb._MaxWaterDeformation = m_MaxWaterDeformation;
            cb._GridSizeMultiplier = 1.0f;

            // Rotation, size and offsets (patch, water mask and foam mask)
            if (instancedQuads)
            {
                Matrix4x4 worldToWater = currentWater.simulation.rendering.worldToWaterMatrix;

                // Compute the grid size to maintain a constant triangle size in screen space
                float distanceToCamera = maxWaveDisplacement + worldToWater.MultiplyPoint3x4(hdCamera.camera.transform.position).y;
                float vertexSpace = Mathf.Min(ComputeScreenSpaceSize(hdCamera, distanceToCamera, 1.0f / WaterConsts.k_WaterTessellatedMeshResolution), 1.0f);
                float gridSize = (1 << (int)(Mathf.Log(1.0f / vertexSpace, 2))) * settings.triangleSize.value;
                cb._GridSize = gridSize;

                // Move the patches with the camera in locksteps to reduce vertex wobbling
                int stableLODCount = 5;
                float cameraStep = (1 << stableLODCount) * gridSize / WaterConsts.k_WaterTessellatedMeshResolution;
                float cameraDirOffset = maxWaveDisplacement * (1.0f - settings.triangleSize.value / 200.0f);
                float3 patchOffset = worldToWater.MultiplyPoint3x4(hdCamera.camera.transform.position + hdCamera.camera.transform.forward * cameraDirOffset);
                cb._PatchOffset = math.round((waterPosition.xz + patchOffset.xz) / cameraStep) * cameraStep - waterPosition.xz;
            }
            else
            {
                cb._GridSize = extent;
                cb._PatchOffset = 0.0f;
            }

            // Used for non infinite surfaces
            if (instancedQuads && !infinite)
            {
                cb._RegionExtent = extent * 0.5f;

                // compute biggest lod that touches region
                float distance = math.max(
                    math.abs(waterPosition.x - cb._PatchOffset.x) + cb._RegionExtent.x,
                    math.abs(waterPosition.z - cb._PatchOffset.y) + cb._RegionExtent.y);
                int lod = (int)math.ceil(math.log2(math.max(distance * 2.0f / cb._GridSize.x, 1.0f)));
                // determine vertex step for this lod level
                float triangleSize = (1 << lod) * cb._GridSize.x / WaterConsts.k_WaterTessellatedMeshResolution;
                // align grid size on region extent
                float2 optimalTriangleSize = new float2(
                    extent.x / Mathf.Max(Mathf.Floor(extent.x / triangleSize), 1),
                    extent.y / Mathf.Max(Mathf.Floor(extent.y / triangleSize), 1));
                cb._GridSize = optimalTriangleSize * cb._GridSize.x / triangleSize;
                // align grid pos on one region corner
                float2 corner = -(cb._PatchOffset + 0.5f * cb._GridSize) - cb._RegionExtent;
                cb._PatchOffset = cb._PatchOffset + (corner - math.round(corner / optimalTriangleSize) * optimalTriangleSize);
            }
            else
                cb._RegionExtent = new float2(float.MaxValue, float.MaxValue);

            if (instancedQuads)
            {
                // Max offset for patch culling
                float3 cameraPosition = hdCamera.camera.transform.position;
                float maxOffsetRelative = math.max(math.abs(cb._PatchOffset.x - cameraPosition.x), math.abs(cb._PatchOffset.y - cameraPosition.z));
                float maxPatchOffset = -maxWaveDisplacement - maxOffsetRelative;

                // Compute last LOD with displacement
                float maxFadeDistance = currentWater.simulation.rendering.maxFadeDistance;
                if (maxFadeDistance != float.MaxValue)
                {
                    float cameraHeight = cameraPosition.y - (waterPosition.y + maxWaveHeight);
                    maxFadeDistance = Mathf.Sqrt(maxFadeDistance * maxFadeDistance - cameraHeight * cameraHeight) - maxPatchOffset;
                    maxFadeDistance = Mathf.Max(maxFadeDistance * 2.0f / Mathf.Max(cb._GridSize.x, cb._GridSize.y), 1);

                    cb._MaxLOD = (uint)Mathf.Ceil(Mathf.Log(maxFadeDistance, 2)); // only keep highest bit
                }
                else
                    cb._MaxLOD = 8; // we could support a maximum of 12 LODs (max 49 patches, 12 * 4 = 48), but this is enough

                cb._GridSizeMultiplier = (1 << (int)cb._MaxLOD) * 0.5f;
            }

            // Large Current
            cb._Group0CurrentRegionScaleOffset.Set(1.0f / currentWater.largeCurrentRegionExtent.x, 1.0f / currentWater.largeCurrentRegionExtent.y, currentWater.largeCurrentRegionOffset.x, -currentWater.largeCurrentRegionOffset.y);
            if (currentWater.ripplesMotionMode == WaterPropertyOverrideMode.Inherit)
            {
                cb._Group1CurrentRegionScaleOffset = cb._Group0CurrentRegionScaleOffset;
                cb._CurrentMapInfluence.Set(currentWater.largeCurrentMapInfluence, currentWater.largeCurrentMapInfluence);
            }
            else
            {
                cb._Group1CurrentRegionScaleOffset.Set(1.0f / currentWater.ripplesCurrentRegionExtent.x, 1.0f / currentWater.ripplesCurrentRegionExtent.y, currentWater.ripplesCurrentRegionOffset.x, -currentWater.ripplesCurrentRegionOffset.y);
                cb._CurrentMapInfluence.Set(currentWater.largeCurrentMapInfluence, currentWater.ripplesCurrentMapInfluence);
            }

            // Tessellation
            cb._WaterMaxTessellationFactor = currentWater.maxTessellationFactor;
            cb._WaterTessellationFadeStart = currentWater.tessellationFactorFadeStart;
            cb._WaterTessellationFadeRange = currentWater.tessellationFactorFadeRange;

            // Bind the rendering layer data for decal layers
            cb._WaterRenderingLayer = (uint)currentWater.renderingLayerMask;

            // Evaluate the matrices
            cb._WaterSurfaceTransform = currentWater.simulation.rendering.waterToWorldMatrix;
            cb._WaterSurfaceTransform_Inverse = currentWater.simulation.rendering.worldToWaterMatrix;
            cb._WaterCustomTransform_Inverse = currentWater.simulation.rendering.worldToWaterMatrixCustom;
        }

        void UpdateWaterSurface(CommandBuffer cmd, WaterSurface currentWater, int surfaceIndex)
        {
            if (!m_ActiveWaterSimulationCPU && currentWater.scriptInteractions)
                InitializeCPUWaterSimulation();

            // If the function returns false, this means the resources were just created and they need to be initialized.
            bool validGPUResources, validCPUResources, validHistory;
            currentWater.CheckResources((int)m_WaterBandResolution, m_ActiveWaterFoam, m_GPUReadbackMode, out validGPUResources, out validCPUResources, out validHistory);

            // Update the simulation time (include timescale)
            currentWater.simulation.Update(currentWater.timeMultiplier);

            // Update the constant buffer
            UpdateShaderVariablesWater(currentWater, surfaceIndex, ref m_ShaderVariablesWater);
            ConstantBuffer.UpdateData(cmd, m_ShaderVariablesWater);

            // Update the GPU simulation for the water
            UpdateGPUWaterSimulation(cmd, currentWater, !validGPUResources);

            // Here we replicate the ocean simulation on the CPU (if requested)
            UpdateCPUWaterSimulation(currentWater, !validCPUResources);

            // Update the foam texture
            UpdateWaterFoamSimulation(cmd, currentWater);

            // Update the deformation data
            UpdateWaterDeformation(cmd, currentWater);

            // Here we need to replicate the water CPU Buffers
            UpdateCPUBuffers(cmd, currentWater);

            // Render the caustics from the current simulation state if required
            if (currentWater.caustics)
                EvaluateWaterCaustics(cmd, currentWater);
            else
                currentWater.simulation.CheckCausticsResources(false, 0);
        }

        void UpdateWaterSurfaces(CommandBuffer cmd)
        {
            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // If water surface simulation is disabled, skip.
            if (!m_ActiveWaterSystem || numWaterSurfaces == 0)
                return;

            // Make sure that all the deformers are on the GPU
            UpdateWaterDeformersData(cmd);

            // Make sure that all the foam generators are on the GPU
            UpdateWaterGeneratorsData(cmd);

            // In case we had a scene switch, it is possible the resource became null
            if (m_GridMesh == null)
                BuildGridMeshes(ref m_GridMesh, ref m_RingMesh, ref m_RingMeshLow);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceUpdate)))
            {
                // Bind the noise textures
                GetBlueNoiseManager().BindDitheredRNGData1SPP(cmd);

                // Loop through them and update them
                for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
                {
                    // Grab the current water surface
                    WaterSurface currentWater = waterSurfaces[surfaceIdx];

                    // Update the water surface (dispersion)
                    UpdateWaterSurface(cmd, currentWater, surfaceIdx);
                }
            }
        }

        void EvaluateWaterRenderingData(WaterSurface currentWater, out bool instancedQuads, out bool infinite, out bool customMesh, out List<MeshRenderer> meshRenderers)
        {
            instancedQuads = currentWater.IsInstancedQuads();
            if (instancedQuads)
            {
                // See if the surface is infinite or clamped
                infinite = currentWater.IsInfinite();

                // We're not using custom meshes in this case
                customMesh = false;
                meshRenderers = null;
            }
            else
            {
                infinite = false;
                if (currentWater.geometryType == WaterGeometryType.Quad || currentWater.meshRenderers.Count == 0)
                {
                    // We're not using custom meshes in this case
                    customMesh = false;
                    meshRenderers = null;
                }
                else
                {
                    customMesh = true;
                    meshRenderers = currentWater.meshRenderers;
                }
            }
        }

        struct WaterRenderingParameters
        {
            // Camera parameters
            public Vector3 cameraPosition;
            public Frustum cameraFrustum;
            public int viewCount;

            // Geometry parameters
            public bool drawInfiniteMesh;
            public bool tessellation;
            public int numActiveBands;
            public Vector3 center;
            public Vector2 waterMaskOffset;
            public bool exclusion;

            // Geometry properties
            public bool instancedQuads;
            public bool infinite;
            public bool customMesh;
            public Mesh tessellableMesh, ringMesh, ringMeshLow;
            public List<MeshRenderer> meshRenderers;

            // Deformation data
            public bool deformation;

            // Foam data
            public bool foam;

            // Underwater data
            public bool evaluateCameraPosition;

            // Caustics parameters
            public bool simulationCaustics;

            // Water patches
            public ComputeShader waterSimulation;
            public int patchEvaluation;

            // Water Mask
            public Texture waterMask;

            // Current
            public bool activeCurrent;
            public Texture largeCurrentMap;
            public Texture ripplesCurrentMap;
            public Texture2D sectorDataBuffer;

            // Foam data
            public Texture2D surfaceFoamTexture;
            public Texture2D simulationFoamMask;
            public Vector2 simulationFoamMaskOffset;

            // Material data
            public Material waterMaterial;
            public MaterialPropertyBlock mbp;

            // Constant buffer
            public ShaderVariablesWater waterCB;
            public ShaderVariablesWaterRendering waterRenderingCB;

            // Water elevation
            public ComputeShader evaluationCS;
            public int findVerticalDisplKernel;
        }

        WaterRenderingParameters PrepareWaterRenderingParameters(HDCamera hdCamera, WaterRendering settings, WaterSurface currentWater, int surfaceIndex, bool insideUnderWaterVolume)
        {
            WaterRenderingParameters parameters = new WaterRenderingParameters();

            // Camera parameters
            parameters.cameraPosition = hdCamera.camera.transform.position;
            parameters.cameraFrustum = hdCamera.frustum;
            parameters.viewCount = hdCamera.viewCount;

            // Geometry parameters
            parameters.drawInfiniteMesh = currentWater.simulation.rendering.maxFadeDistance != float.MaxValue;
            parameters.tessellation = currentWater.tessellation;
            parameters.numActiveBands = currentWater.simulation.numActiveBands;
            parameters.center = currentWater.transform.position;
            parameters.simulationFoamMaskOffset = currentWater.simulationFoamMaskOffset;
            parameters.waterMaskOffset = currentWater.waterMaskOffset;
            parameters.exclusion = hdCamera.frameSettings.IsEnabled(FrameSettingsField.WaterExclusion);

            // Evaluate which mesh shall be used, etc
            EvaluateWaterRenderingData(currentWater, out parameters.instancedQuads, out parameters.infinite, out parameters.customMesh, out parameters.meshRenderers);
            parameters.tessellableMesh = m_GridMesh;
            parameters.ringMesh = m_RingMesh;
            parameters.ringMeshLow = m_RingMeshLow;

            // At the moment indirect buffer for instanced mesh draw with tessellation does not work on metal
            if (parameters.instancedQuads && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                parameters.tessellation = false;

            // Under water data
            parameters.evaluateCameraPosition = insideUnderWaterVolume;

            // Patch evaluation parameters
            parameters.waterSimulation = m_WaterSimulationCS;
            parameters.patchEvaluation = parameters.infinite ? m_EvaluateInstanceDataInfiniteKernel : m_EvaluateInstanceDataKernel;

            // Deformation parameters
            parameters.deformation = hdCamera.frameSettings.IsEnabled(FrameSettingsField.WaterDeformation) && currentWater.deformation;

            // Foam parameters
            parameters.foam = m_ActiveWaterFoam && currentWater.foam;

            // Caustics parameters
            parameters.simulationCaustics = currentWater.caustics;

            // All the required global textures
            parameters.waterMask = currentWater.waterMask != null ? currentWater.waterMask : Texture2D.whiteTexture;
            parameters.surfaceFoamTexture = m_Asset.renderPipelineResources.textures.foamMask;
            parameters.simulationFoamMask = currentWater.simulationFoamMask != null ? currentWater.simulationFoamMask : Texture2D.whiteTexture;

            // Current
            parameters.activeCurrent = currentWater.largeCurrentMap != null || currentWater.ripplesCurrentMap != null;
            parameters.largeCurrentMap = currentWater.largeCurrentMap != null ? currentWater.largeCurrentMap : Texture2D.blackTexture;
            parameters.ripplesCurrentMap = currentWater.ripplesCurrentMap != null ? currentWater.ripplesCurrentMap : Texture2D.blackTexture;
            parameters.sectorDataBuffer = m_WaterSectorData;

            // Water material
            parameters.waterMaterial = currentWater.customMaterial != null ? currentWater.customMaterial : m_InternalWaterMaterial;

            // Property bloc used for binding the textures
            parameters.mbp = m_WaterMaterialPropertyBlock;

            // Setup the simulation water constant buffer
            UpdateShaderVariablesWater(currentWater, surfaceIndex, ref parameters.waterCB);

            // Setup the rendering water constant buffer
            UpdateShaderVariablesWaterRendering(currentWater, hdCamera, settings,
                parameters.instancedQuads, parameters.infinite, parameters.customMesh,
                parameters.waterCB._MaxWaveDisplacement, parameters.waterCB._MaxWaveHeight,
                ref parameters.waterRenderingCB);

            // Waterline & underwater
            parameters.evaluationCS = m_WaterEvaluationCS;
            parameters.findVerticalDisplKernel = m_FindVerticalDisplacementsKernel;

            return parameters;
        }

        struct WaterRenderingDeferredParameters
        {
            // Camera parameters
            public bool pbsActive;

            // Deferred Lighting
            public ComputeShader waterLighting;
            public int[] indirectLightingKernels;
            public int lightingKernel;
            public int numVariants;
            public int waterFogKernel;

            // Other
            public ShaderVariablesWaterRendering waterRenderingCB;
        }

        WaterRenderingDeferredParameters PrepareWaterRenderingDeferredParameters(HDCamera hdCamera)
        {
            WaterRenderingDeferredParameters parameters = new WaterRenderingDeferredParameters();

            // Is the physically based sky active? (otherwise we need to bind some fall back textures)
            var visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
            parameters.pbsActive = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased;

            // Deferred Lighting
            parameters.waterLighting = m_WaterLightingCS;
            parameters.indirectLightingKernels = m_WaterIndirectDeferredKernels;
            parameters.lightingKernel = m_WaterDeferredKernel;
            parameters.numVariants = WaterConsts.k_NumWaterVariants;
            parameters.waterFogKernel = m_WaterFogIndirectKernel;

            // Ambient probe
            parameters.waterRenderingCB._WaterAmbientProbe = m_WaterAmbientProbe;

            return parameters;
        }

        class WaterRenderingGBufferData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingParameters parameters;

            // Simulation & Deformation buffers
            public TextureHandle displacementTexture;
            public TextureHandle additionalData;
            public TextureHandle causticsData;
            public TextureHandle foamData;
            public TextureHandle deformationBuffer;
            public TextureHandle deformationSGBuffer;

            // Other resources
            public BufferHandle indirectBuffer;
            public BufferHandle patchDataBuffer;
            public BufferHandle frustumBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle depthPyramid;

            // Water rendered to this buffer
            public TextureHandle colorPyramid;

            // Light Cluster data
            public BufferHandle layeredOffsetsBuffer;
            public BufferHandle logBaseBuffer;

            public bool decalsEnabled;
            public BufferHandle heightBuffer;
        }

        class WaterPrepareLightingData
        {
            // Camera data
            public int width;
            public int height;
            public int viewCount;
            public int tileX;
            public int tileY;
            public int numTiles;
            public bool transparentSSR;

            // Shader data
            public ComputeShader waterLighting;
            public int clearIndirectKernel;
            public int classifyTilesKernel;
            public int prepareSSRKernel;

            // Input textures
            public TextureHandle depthBuffer;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer3;
            public BufferHandle waterSurfaceProfiles;
            public BufferHandle perVoxelOffset;
            public BufferHandle perTileLogBaseTweak;

            // Output texture
            public TextureHandle normalBuffer;
            public BufferHandle indirectBuffer;
            public BufferHandle tileBuffer;
        }

        unsafe Vector4 EvaluateWaterAmbientProbe(HDCamera hdCamera, float ambientProbeDimmer)
        {
            SphericalHarmonicsL2 probeSH = m_SkyManager.GetAmbientProbe(hdCamera);
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, ambientProbeDimmer);
            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsProbe, probeSH);
            Vector4 ambient = EvaluateAmbientProbe(m_PackedCoeffsProbe, Vector3.up);
            return new Vector4(ambient.x, ambient.y, ambient.z, ambient.x * 0.2126729f + ambient.y * 0.7151522f + ambient.z * 0.072175f);
        }

        void FillWaterSurfaceProfile(HDCamera hdCamera, WaterSurface waterSurface, int waterSurfaceIndex)
        {
            WaterSurfaceProfile profile = new WaterSurfaceProfile();
            profile.bodyScatteringHeight = waterSurface.directLightBodyScattering;
            profile.tipScatteringHeight = waterSurface.directLightTipScattering;
            profile.maxRefractionDistance = Mathf.Min(waterSurface.absorptionDistance, waterSurface.maxRefractionDistance);
            profile.renderingLayers = (uint)waterSurface.renderingLayerMask;
            profile.cameraUnderWater = waterSurfaceIndex == m_UnderWaterSurfaceIndex ? 1 : 0;
            profile.transparencyColor = new Vector3(Mathf.Min(waterSurface.refractionColor.r, 0.99f),
                                                    Mathf.Min(waterSurface.refractionColor.g, 0.99f),
                                                    Mathf.Min(waterSurface.refractionColor.b, 0.99f));
            profile.outScatteringCoefficient = -Mathf.Log(0.02f) / waterSurface.absorptionDistance;
            profile.scatteringColor = new Vector3(waterSurface.scatteringColor.r, waterSurface.scatteringColor.g, waterSurface.scatteringColor.b);
            profile.envPerceptualRoughness = waterSurface.surfaceType == WaterSurfaceType.Pool ? 0.0f : Mathf.Lerp(0.0f, 0.15f, Mathf.Clamp(waterSurface.largeWindSpeed / WaterConsts.k_EnvRoughnessWindSpeed, 0.0f, 1.0f));
            profile.disableIOR = waterSurface.underWaterRefraction ? 0 : 1;

            // Smoothness fade
            profile.smoothnessFadeStart = waterSurface.smoothnessFadeStart;
            profile.smoothnessFadeDistance = waterSurface.smoothnessFadeDistance;
            profile.roughnessEndValue = 1.0f - waterSurface.endSmoothness;
            profile.upDirection = waterSurface.UpVector();

            profile.foamColor = new Vector3(waterSurface.foamColor.r, waterSurface.foamColor.g, waterSurface.foamColor.b);

            // Under water stuff
            profile.underWaterAmbientProbeContribution = waterSurface.underWaterAmbientProbeContribution;
            profile.absorptionDistanceMultiplier = 1.0f / waterSurface.absorptionDistanceMultiplier;

            // Profile has been filled, we're done
            m_WaterSurfaceProfileArray[waterSurfaceIndex] = profile;
        }

        static void SetupCommonRenderingData(CommandBuffer cmd,
            RTHandle displacementBuffer, RTHandle additionalDataBuffer, RTHandle causticsBuffer,
            RTHandle foamBuffer, Texture deformationBuffer, Texture deformationSGBuffer,
            WaterRenderingParameters parameters)
        {
            ConstantBuffer.UpdateData(cmd, parameters.waterCB);
            ConstantBuffer.UpdateData(cmd, parameters.waterRenderingCB);

            // Raise the keywords for band count
            SetupWaterShaderKeyword(cmd, parameters.numActiveBands, parameters.activeCurrent);

            // Prepare the material property block for the rendering
            parameters.mbp.SetTexture(HDShaderIDs._WaterDisplacementBuffer, displacementBuffer);
            parameters.mbp.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, additionalDataBuffer);
            parameters.mbp.SetTexture(HDShaderIDs._WaterCausticsDataBuffer, causticsBuffer);
            parameters.mbp.SetTexture(HDShaderIDs._WaterFoamBuffer, foamBuffer);
            parameters.mbp.SetTexture(HDShaderIDs._WaterDeformationBuffer, deformationBuffer);
            parameters.mbp.SetTexture(HDShaderIDs._WaterDeformationSGBuffer, deformationSGBuffer);

            // Bind the global water textures
            parameters.mbp.SetTexture(HDShaderIDs._WaterMask, parameters.waterMask);
            parameters.mbp.SetTexture(HDShaderIDs._FoamTexture, parameters.surfaceFoamTexture);
            parameters.mbp.SetTexture(HDShaderIDs._SimulationFoamMask, parameters.simulationFoamMask);
            if (parameters.activeCurrent)
            {
                parameters.mbp.SetTexture(HDShaderIDs._Group0CurrentMap, parameters.largeCurrentMap);
                parameters.mbp.SetTexture(HDShaderIDs._Group1CurrentMap, parameters.ripplesCurrentMap);
                parameters.mbp.SetTexture(HDShaderIDs._WaterSectorData, parameters.sectorDataBuffer);
            }
        }

        static void RenderWaterSurface(CommandBuffer cmd,
            RTHandle displacementBuffer, RTHandle additionalDataBuffer, RTHandle causticsBuffer, RTHandle foamBuffer,
            Texture deformationBuffer, Texture deformationSGBuffer,
            RTHandle normalBuffer, RTHandle depthPyramid,
            GraphicsBuffer layeredOffsetsBuffer, GraphicsBuffer logBaseBuffer,
            GraphicsBuffer cameraHeightBuffer, GraphicsBuffer patchDataBuffer, GraphicsBuffer indirectBuffer, GraphicsBuffer cameraFrustumBuffer,
            WaterRenderingParameters parameters)
        {
            SetupCommonRenderingData(cmd, displacementBuffer, additionalDataBuffer, causticsBuffer, foamBuffer, deformationBuffer, deformationSGBuffer, parameters);

            // First we need to evaluate if we are in the underwater region of this water surface if the camera
            // is above of under water. This will need to be done on the CPU later
            if (parameters.evaluateCameraPosition)
            {
                ConstantBuffer.Set<ShaderVariablesWater>(cmd, parameters.evaluationCS, HDShaderIDs._ShaderVariablesWater);
                ConstantBuffer.Set<ShaderVariablesWaterRendering>(cmd, parameters.evaluationCS, HDShaderIDs._ShaderVariablesWaterRendering);

                // Evaluate the camera height, should be done on the CPU later
                cmd.SetComputeBufferParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._WaterCameraHeightBufferRW, cameraHeightBuffer);
                cmd.SetComputeTextureParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._WaterDisplacementBuffer, displacementBuffer);
                cmd.SetComputeTextureParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._WaterDeformationBuffer, deformationBuffer);
                cmd.SetComputeTextureParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._WaterMask, parameters.waterMask);
                if (parameters.activeCurrent)
                {
                    cmd.SetComputeTextureParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._WaterSectorData, parameters.sectorDataBuffer);
                    cmd.SetComputeTextureParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._Group0CurrentMap, parameters.largeCurrentMap);
                    cmd.SetComputeTextureParam(parameters.evaluationCS, parameters.findVerticalDisplKernel, HDShaderIDs._Group1CurrentMap, parameters.ripplesCurrentMap);
                }
                cmd.DispatchCompute(parameters.evaluationCS, parameters.findVerticalDisplKernel, 1, 1, parameters.viewCount);
            }

            // Light cluster data
            if (layeredOffsetsBuffer != null)
                parameters.mbp.SetBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, layeredOffsetsBuffer);
            if (logBaseBuffer != null)
                parameters.mbp.SetBuffer(HDShaderIDs.g_logBaseBuffer, logBaseBuffer);

            // Normally we should bind this into the material property block, but on metal there seems to be an issue. This fixes it.
            parameters.mbp.SetBuffer(HDShaderIDs._WaterPatchData, patchDataBuffer);
            parameters.mbp.SetBuffer(HDShaderIDs._WaterCameraHeightBuffer, cameraHeightBuffer);
            parameters.mbp.SetTexture(HDShaderIDs._CameraDepthTexture, depthPyramid);
            parameters.mbp.SetTexture(HDShaderIDs._NormalBufferTexture, normalBuffer);

            // Raise the right stencil flags
            cmd.SetGlobalFloat(HDShaderIDs._StencilWaterRefGBuffer, (int)(StencilUsage.WaterSurface | StencilUsage.TraceReflectionRay));
            cmd.SetGlobalFloat(HDShaderIDs._StencilWaterWriteMaskGBuffer, (int)(StencilUsage.WaterSurface | StencilUsage.TraceReflectionRay));
            cmd.SetGlobalFloat(HDShaderIDs._StencilWaterReadMaskGBuffer, parameters.exclusion ? (int)(StencilUsage.WaterExclusion) : 0);
            cmd.SetGlobalFloat(HDShaderIDs._CullWaterMask, parameters.evaluateCameraPosition ? (int)CullMode.Off : (int)CullMode.Back);

            var passNames = parameters.tessellation ? k_PassesGBufferTessellation : k_PassesGBuffer;
            DrawWaterSurface(cmd, parameters, passNames, patchDataBuffer, indirectBuffer, cameraFrustumBuffer);

            // Reset the keywords
            ResetWaterShaderKeyword(cmd);
        }

        void RenderWaterSurfaceGBuffer(RenderGraph renderGraph, HDCamera hdCamera,
                                        WaterSurface currentWater, WaterRendering settings, int surfaceIdx, bool evaluateCameraPos,
                                        TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle depthPyramid, BufferHandle layeredOffsetsBuffer, BufferHandle logBaseBuffer,
                                        TextureHandle WaterGbuffer0, TextureHandle WaterGbuffer1, TextureHandle WaterGbuffer2, TextureHandle WaterGbuffer3, BufferHandle cameraHeightBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<WaterRenderingGBufferData>("Render Water Surface GBuffer", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingGBuffer)))
            {
                // Prepare all the internal parameters
                passData.parameters = PrepareWaterRenderingParameters(hdCamera, settings, currentWater, surfaceIdx, evaluateCameraPos);
                passData.decalsEnabled = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);

                // Request the output textures
                builder.UseColorBuffer(WaterGbuffer0, 0);
                builder.UseColorBuffer(WaterGbuffer1, 1);
                builder.UseColorBuffer(WaterGbuffer2, 2);
                builder.UseColorBuffer(WaterGbuffer3, 3);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.heightBuffer = builder.WriteBuffer(cameraHeightBuffer);

                // Import all the textures into the system
                passData.displacementTexture = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.displacementBuffer);
                passData.additionalData = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.additionalDataBuffer);
                passData.causticsData = passData.parameters.simulationCaustics ? renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.causticsBuffer) : renderGraph.defaultResources.blackTexture;
                passData.foamData = passData.parameters.foam ? renderGraph.ImportTexture(currentWater.FoamBuffer()) : renderGraph.defaultResources.blackTexture;
                passData.deformationBuffer = passData.parameters.deformation ? renderGraph.ImportTexture(currentWater.deformationBuffer) : renderGraph.defaultResources.blackTexture;
                passData.deformationSGBuffer = passData.parameters.deformation ? renderGraph.ImportTexture(currentWater.deformationSGBuffer) : renderGraph.defaultResources.blackTexture;
                passData.indirectBuffer = renderGraph.ImportBuffer(m_WaterIndirectDispatchBuffer);
                passData.patchDataBuffer = renderGraph.ImportBuffer(m_WaterPatchDataBuffer);
                passData.frustumBuffer = renderGraph.ImportBuffer(m_WaterCameraFrustrumBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.layeredOffsetsBuffer = builder.ReadBuffer(layeredOffsetsBuffer);
                passData.logBaseBuffer = builder.ReadBuffer(logBaseBuffer);

                builder.SetRenderFunc(
                    (WaterRenderingGBufferData data, RenderGraphContext ctx) =>
                    {
                        if (data.decalsEnabled)
                            DecalSystem.instance.SetAtlas(ctx.cmd);

                        // Render the water surface
                        RenderWaterSurface(ctx.cmd,
                            data.displacementTexture, data.additionalData, data.causticsData, data.foamData,
                            data.deformationBuffer, data.deformationSGBuffer,
                            data.normalBuffer, data.depthPyramid,
                            data.layeredOffsetsBuffer, data.logBaseBuffer,
                            data.heightBuffer, data.patchDataBuffer, data.indirectBuffer, data.frustumBuffer, data.parameters);
                    });
            }
        }

        void PropagateFrustumDataToGPU(HDCamera hdCamera)
        {
            // Plane 0
            FrustumGPU frustum;
            frustum.normal0 = hdCamera.frustum.planes[0].normal;
            frustum.dist0 = hdCamera.frustum.planes[0].distance;

            // Plane 1
            frustum.normal1 = hdCamera.frustum.planes[1].normal;
            frustum.dist1 = hdCamera.frustum.planes[1].distance;

            // Plane 2
            frustum.normal2 = hdCamera.frustum.planes[2].normal;
            frustum.dist2 = hdCamera.frustum.planes[2].distance;

            // Plane 3
            frustum.normal3 = hdCamera.frustum.planes[3].normal;
            frustum.dist3 = hdCamera.frustum.planes[3].distance;

            // Plane 4
            frustum.normal4 = hdCamera.frustum.planes[4].normal;
            frustum.dist4 = hdCamera.frustum.planes[4].distance;

            // Plane 4
            frustum.normal5 = hdCamera.frustum.planes[5].normal;
            frustum.dist5 = hdCamera.frustum.planes[5].distance;

            // Corners
            frustum.corner0 = hdCamera.frustum.corners[0];
            frustum.corner1 = hdCamera.frustum.corners[1];
            frustum.corner2 = hdCamera.frustum.corners[2];
            frustum.corner3 = hdCamera.frustum.corners[3];
            frustum.corner4 = hdCamera.frustum.corners[4];
            frustum.corner5 = hdCamera.frustum.corners[5];
            frustum.corner6 = hdCamera.frustum.corners[6];
            frustum.corner7 = hdCamera.frustum.corners[7];

            // Copy the data to the GPU
            m_WaterCameraFrustumCPU[0] = frustum;
            m_WaterCameraFrustrumBuffer.SetData(m_WaterCameraFrustumCPU);
        }

        internal struct WaterGBuffer
        {
            // Flag that defines if at least one water surface was rendered into the gbuffer
            public bool valid;

            // Flag that defines if at least one water surface will need to be rendered as debug
            public bool debugRequired;

            // GBuffer targets
            public TextureHandle waterGBuffer0;
            public TextureHandle waterGBuffer1;
            public TextureHandle waterGBuffer2;
            public TextureHandle waterGBuffer3;

            // Indirect dispatch and tile data
            public BufferHandle indirectBuffer;
            public BufferHandle tileBuffer;

            public BufferHandle cameraHeight;
        }

        bool ShouldRenderWater(HDCamera hdCamera)
        {
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            return !(!settings.enable.value
                || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water)
                || WaterSurface.instanceCount == 0);
        }

        WaterGBuffer RenderWaterGBuffer(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera,
                                        TextureHandle depthBuffer, TextureHandle normalBuffer,
                                        TextureHandle colorPyramid, TextureHandle depthPyramid,
                                        BufferHandle waterSurfaceProfiles,
                                        in BuildGPULightListOutput lightLists)
        {
            // Allocate the return structure
            WaterGBuffer outputGBuffer = new WaterGBuffer();
            outputGBuffer.valid = false;
            outputGBuffer.debugRequired = false;

            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = Mathf.Min(WaterSurface.instanceCount, k_MaxNumWaterSurfaceProfiles);

            // Tile sizes
            int tileX = (hdCamera.actualWidth + 7) / 8;
            int tileY = (hdCamera.actualHeight + 7) / 8;
            int numTiles = tileX * tileY;

            // Make sure the current data is valid
            CheckWaterCurrentData();

            // We need to tag the stencil for water rejection
            WaterRejectionTag(renderGraph, cull, hdCamera, depthBuffer);

            // Copy the frustum data to the GPU
            PropagateFrustumDataToGPU(hdCamera);

            // Request all the gbuffer textures we will need
            TextureHandle WaterGbuffer0 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Water GBuffer 0", fallBackToBlackTexture = true });
            TextureHandle WaterGbuffer1 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "Water GBuffer 1", fallBackToBlackTexture = true });
            TextureHandle WaterGbuffer2 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "Water GBuffer 2", fallBackToBlackTexture = true });
            TextureHandle WaterGbuffer3 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "Water GBuffer 3", fallBackToBlackTexture = true });
            BufferHandle indirectBuffer = renderGraph.CreateBuffer(new BufferDesc((WaterConsts.k_NumWaterVariants + 1) * 3,
                sizeof(uint), GraphicsBuffer.Target.IndirectArguments) { name = "Water Deferred Indirect" });
            BufferHandle tileBuffer = renderGraph.CreateBuffer(new BufferDesc((WaterConsts.k_NumWaterVariants + 1) * numTiles * m_MaxViewCount, sizeof(uint)) { name = "Water Deferred Tiles" });

            // Set the textures handles for the water gbuffer
            outputGBuffer.waterGBuffer0 = WaterGbuffer0;
            outputGBuffer.waterGBuffer1 = WaterGbuffer1;
            outputGBuffer.waterGBuffer2 = WaterGbuffer2;
            outputGBuffer.waterGBuffer3 = WaterGbuffer3;
            outputGBuffer.indirectBuffer = indirectBuffer;
            outputGBuffer.tileBuffer = tileBuffer;
            outputGBuffer.cameraHeight = renderGraph.ImportBuffer(m_WaterCameraHeightBuffer);

            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();

            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // At least one surface will need to be rendered as a debug view.
                if (currentWater.debugMode != WaterDebugMode.None)
                {
                    outputGBuffer.debugRequired = true;
                    continue;
                }

                // Only render the water surface if it is included in the layers that the camera requires
                int waterCullingMask = 1 << currentWater.gameObject.layer;
                if (hdCamera.camera.cullingMask != 0 && (waterCullingMask & hdCamera.camera.cullingMask) == 0)
                    continue;

#if UNITY_EDITOR
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (!CoreUtils.IsSceneViewPrefabStageContextHidden() && stage != null && stage.mode == PrefabStage.Mode.InContext)
                {
                    bool isInPrefabScene = stage.scene == currentWater.gameObject.scene;
                    if ((hdCamera.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.Off && isInPrefabScene)
                        || (hdCamera.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered && !isInPrefabScene))
                    {
                        continue;
                    }
                }

                if (currentWater.customMaterial != null && !WaterSurface.IsWaterMaterial(currentWater.customMaterial))
                    continue;
#endif
                // One surface needs to pass the resource tests for the gbuffer to be valid
                outputGBuffer.valid = true;

                // Fill the water surface profile
                FillWaterSurfaceProfile(hdCamera, currentWater, surfaceIdx);

                // Render the water surface
                RenderWaterSurfaceGBuffer(renderGraph, hdCamera, currentWater, settings, surfaceIdx, surfaceIdx == m_UnderWaterSurfaceIndex,
                                depthBuffer, normalBuffer, depthPyramid, lightLists.perVoxelOffset, lightLists.perTileLogBaseTweak,
                                WaterGbuffer0, WaterGbuffer1, WaterGbuffer2, WaterGbuffer3, outputGBuffer.cameraHeight);
            }

            // If no water surface wrote to the water gbuffer, we can exit right now.
            if (!outputGBuffer.valid)
                return outputGBuffer;

            using (var builder = renderGraph.AddRenderPass<WaterPrepareLightingData>("Prepare water for lighting", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfacePrepareLighting)))
            {
                // Camera parameters
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.tileX = (passData.width + 7) / 8;
                passData.tileY = (passData.height + 7) / 8;
                passData.numTiles = passData.tileX * passData.tileY;
                passData.transparentSSR = hdCamera.IsSSREnabled(true);

                // CS and kernels
                passData.waterLighting = m_WaterLightingCS;
                passData.prepareSSRKernel = m_WaterPrepareSSRIndirectKernel;
                passData.clearIndirectKernel = m_WaterClearIndirectKernel;
                passData.classifyTilesKernel = m_WaterClassifyTilesKernel;

                // Input resources
                passData.gbuffer1 = builder.ReadTexture(WaterGbuffer1);
                passData.gbuffer3 = builder.ReadTexture(WaterGbuffer3);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.waterSurfaceProfiles = builder.ReadBuffer(renderGraph.ImportBuffer(m_WaterProfileArrayGPU));
                passData.perVoxelOffset = builder.ReadBuffer(lightLists.perVoxelOffset);
                passData.perTileLogBaseTweak = builder.ReadBuffer(lightLists.perTileLogBaseTweak);

                // Output resources
                passData.normalBuffer = builder.WriteTexture(normalBuffer);
                passData.indirectBuffer = builder.WriteBuffer(indirectBuffer);
                passData.tileBuffer = builder.WriteBuffer(tileBuffer);

                builder.SetRenderFunc(
                    (WaterPrepareLightingData data, RenderGraphContext ctx) =>
                    {
                        // Clear indirect args
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, data.clearIndirectKernel, HDShaderIDs._WaterDispatchIndirectBuffer, data.indirectBuffer);
                        ctx.cmd.DispatchCompute(data.waterLighting, data.clearIndirectKernel, 1, 1, 1);

                        // Bind the input gbuffer data
                        int kernel = data.classifyTilesKernel;
                        ctx.cmd.SetComputeIntParam(data.waterLighting, HDShaderIDs._WaterNumTiles, data.numTiles);
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, kernel, HDShaderIDs._WaterDispatchIndirectBuffer, data.indirectBuffer);
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, kernel, HDShaderIDs._WaterTileBufferRW, data.tileBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, kernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, kernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, kernel, HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, kernel, HDShaderIDs.g_logBaseBuffer, data.perTileLogBaseTweak);
                        ctx.cmd.DispatchCompute(data.waterLighting, kernel, data.tileX, data.tileY, data.viewCount);

                        if (data.transparentSSR)
                        {
                            // Prepare the normal buffer for SSR
                            ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterGBufferTexture1, data.gbuffer1);
                            ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterGBufferTexture3, data.gbuffer3);
                            ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                            ctx.cmd.SetComputeBufferParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                            ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._NormalBufferRW, data.normalBuffer);
                            ctx.cmd.SetComputeBufferParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterTileBuffer, data.tileBuffer);
                            ctx.cmd.DispatchCompute(data.waterLighting, data.prepareSSRKernel, data.indirectBuffer, (uint)WaterConsts.k_NumWaterVariants * 3 * sizeof(uint));
                        }
                    });
            }

            return outputGBuffer;
        }

        void RenderWaterDebug(RenderGraph renderGraph, HDCamera hdCamera, bool msaa, TextureHandle colorBuffer, TextureHandle depthBuffer, in BuildGPULightListOutput lightLists)
        {
            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = Mathf.Min(WaterSurface.instanceCount, k_MaxNumWaterSurfaceProfiles);

            // If the water is disabled, no need to render or simulate
            if (!ShouldRenderWater(hdCamera))
                return;

            // Make sure the current data is valid
            CheckWaterCurrentData();
            // Copy the frustum data to the GPU
            PropagateFrustumDataToGPU(hdCamera);

            // Request all the gbuffer textures we will need
            TextureHandle WaterGbuffer0 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                bindTextureMS = msaa,
                msaaSamples = hdCamera.msaaSamples,
                clearColor = Color.clear,
                name = msaa ? "WaterGBuffer0MSAA" : "WaterGBuffer0"
            });

            TextureHandle WaterGbuffer2 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                bindTextureMS = msaa,
                msaaSamples = hdCamera.msaaSamples,
                clearColor = Color.clear,
                name = msaa ? "WaterGBuffer2MSAA" : "WaterGBuffer2"
            });

            TextureHandle WaterGbuffer3 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R16G16_UInt,
                bindTextureMS = msaa,
                msaaSamples = hdCamera.msaaSamples,
                clearColor = Color.clear,
                name = msaa ? "WaterGBuffer3MSAA" : "WaterGBuffer3"
            });

            BufferHandle cameraHeight = renderGraph.ImportBuffer(m_WaterCameraHeightBuffer);
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();

            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // Render the water surface
                RenderWaterSurfaceMask(renderGraph, hdCamera, currentWater, settings, surfaceIdx, colorBuffer, depthBuffer);
            }
        }

        class WaterRenderingDeferredData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingDeferredParameters parameters;

            // GBuffer Data
            public BufferHandle indirectBuffer;
            public BufferHandle tileBuffer;
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public TextureHandle depthBuffer;
            public TextureHandle depthPyramid;

            // Profiles
            public BufferHandle waterSurfaceProfiles;

            // Lighting textures/buffers
            public TextureHandle scatteringFallbackTexture;
            public TextureHandle volumetricLightingTexture;
            public TextureHandle transparentSSRLighting;
            public BufferHandle perVoxelOffset;
            public BufferHandle perTileLogBaseTweak;
            public BufferHandle cameraHeightBuffer;
            public BufferHandle waterLine;

            // Temporary buffers
            public TextureHandle waterLightingBuffer;

            // Water rendered to this buffer
            public TextureHandle colorBuffer;
        }

        void RenderWaterLighting(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle depthPyramid,
            TextureHandle volumetricLightingTexture, TextureHandle ssrLighting,
            in TransparentPrepassOutput prepassOutput, in BuildGPULightListOutput lightLists)
        {
            // We do not render the deferred lighting if:
            // - Water rendering is disabled.
            // - The water gbuffer was never written.
            if (!ShouldRenderWater(hdCamera) || !prepassOutput.waterGBuffer.valid)
                return;

            // Push the water profiles to the GPU for the deferred lighting pass
            m_WaterProfileArrayGPU.SetData(m_WaterSurfaceProfileArray);

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<WaterRenderingDeferredData>("Render Water Surfaces Deferred", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingDeferred)))
            {
                // Prepare all the internal parameters
                passData.parameters = PrepareWaterRenderingDeferredParameters(hdCamera);

                // GBuffer data
                passData.indirectBuffer = builder.ReadBuffer(prepassOutput.waterGBuffer.indirectBuffer);
                passData.tileBuffer = builder.ReadBuffer(prepassOutput.waterGBuffer.tileBuffer);
                passData.gbuffer0 = builder.ReadTexture(prepassOutput.waterGBuffer.waterGBuffer0);
                passData.gbuffer1 = builder.ReadTexture(prepassOutput.waterGBuffer.waterGBuffer1);
                passData.gbuffer2 = builder.ReadTexture(prepassOutput.waterGBuffer.waterGBuffer2);
                passData.gbuffer3 = builder.ReadTexture(prepassOutput.waterGBuffer.waterGBuffer3);

                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.waterSurfaceProfiles = builder.ReadBuffer(prepassOutput.waterSurfaceProfiles);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;
                passData.volumetricLightingTexture = builder.ReadTexture(volumetricLightingTexture);
                passData.transparentSSRLighting = builder.ReadTexture(ssrLighting);
                passData.perVoxelOffset = builder.ReadBuffer(lightLists.perVoxelOffset);
                passData.perTileLogBaseTweak = builder.ReadBuffer(lightLists.perTileLogBaseTweak);
                passData.cameraHeightBuffer = builder.ReadBuffer(prepassOutput.waterGBuffer.cameraHeight);
                passData.waterLine = builder.ReadBuffer(prepassOutput.waterLine);

                // Request the output textures
                passData.waterLightingBuffer = builder.CreateTransientTexture(colorBuffer);
                passData.colorBuffer = builder.WriteTexture(colorBuffer);

                // Run the deferred lighting
                builder.SetRenderFunc(
                    (WaterRenderingDeferredData data, RenderGraphContext ctx) =>
                    {
                        // Make sure the constant buffer is pushed
                        ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.waterLighting, HDShaderIDs._ShaderVariablesWaterRendering);

                        if (!data.parameters.pbsActive)
                        {
                            // This has to be done in the global space given that the "correct" one happens in the global space.
                            // If we do it in the local space, there are some cases when the previous frames local take precedence over the current frame global one.
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, data.scatteringFallbackTexture);
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, data.scatteringFallbackTexture);
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, data.scatteringFallbackTexture);
                        }

                        for (int variantIdx = 0; variantIdx < data.parameters.numVariants; ++variantIdx)
                        {
                            // Kernel to be used for the target variant
                            int kernel = data.parameters.indirectLightingKernels[variantIdx];

                            // Bind the input gbuffer data
                            ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, kernel, HDShaderIDs._WaterTileBuffer, data.tileBuffer);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._WaterGBufferTexture0, data.gbuffer0);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._WaterGBufferTexture1, data.gbuffer1);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._WaterGBufferTexture2, data.gbuffer2);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._WaterGBufferTexture3, data.gbuffer3);
                            ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, kernel, HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._SsrLightingTexture, data.transparentSSRLighting);
                            ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, kernel, HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);
                            ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, kernel, HDShaderIDs.g_logBaseBuffer, data.perTileLogBaseTweak);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._ColorPyramidTexture, data.colorBuffer); // caution, this is not a pyramid, we can't access LODs

                            // Bind the output texture
                            ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, kernel, HDShaderIDs._CameraColorTextureRW, data.waterLightingBuffer);

                            // Run the lighting
                            ctx.cmd.DispatchCompute(data.parameters.waterLighting, kernel, data.indirectBuffer, (uint)variantIdx * 3 * sizeof(uint));
                        }

                        // Evaluate the fog
                        int fogKernel = data.parameters.waterFogKernel;
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._WaterLineBuffer, data.waterLine);
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._WaterTileBuffer, data.tileBuffer);
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._WaterGBufferTexture3, data.gbuffer3);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._VBufferLighting, data.volumetricLightingTexture);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._CameraColorTexture, data.waterLightingBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, fogKernel, HDShaderIDs._CameraColorTextureRW, data.colorBuffer);
                        ctx.cmd.DispatchCompute(data.parameters.waterLighting, fogKernel, data.indirectBuffer, (uint)WaterConsts.k_NumWaterVariants * 3 * sizeof(uint));
                    });
            }
        }

        class WaterExclusionPassData
        {
            public FrameSettings frameSettings;
            public TextureHandle depthBuffer;
            public RendererListHandle opaqueRenderList;
        }

        void WaterRejectionTag(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, TextureHandle depthBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.WaterExclusion))
                return;

            using (var builder = renderGraph.AddRenderPass<WaterExclusionPassData>("Water Exclusion", out var passData, ProfilingSampler.Get(HDProfileId.WaterExclusion)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.opaqueRenderList = builder.UseRendererList(renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_WaterStencilTagNames, stateBlock: m_DepthStateNoWrite)));

                builder.SetRenderFunc(
                    (WaterExclusionPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalInteger(HDShaderIDs._StencilWriteMaskStencilTag, (int)StencilUsage.WaterExclusion);
                        ctx.cmd.SetGlobalInteger(HDShaderIDs._StencilRefMaskStencilTag, (int)StencilUsage.WaterExclusion);
                        DrawOpaqueRendererList(ctx.renderContext, ctx.cmd, data.frameSettings, data.opaqueRenderList);
                    });
            }
        }
    }
}

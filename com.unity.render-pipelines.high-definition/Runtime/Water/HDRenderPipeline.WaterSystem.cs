using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Number of bands when the water is rendered at high band count
        const int k_WaterHighBandCount = 4;
        // Number of bands when the water is rendered at low band count
        const int k_WaterLowBandCount = 2;
        // Minimal size that a patch can reach (in meters)
        const float k_MinPatchSize = 5.0f;
        // Minimal quad resolution when rendering a water patch
        const int k_WaterMinGridSize = 2;

        // Earth gravitational constant (at the surface)
        const float k_PhillipsGravityConstant = 9.81f;
        const float k_PhillipsWindScalar = 1.0f / k_PhillipsGravityConstant; // Is this a coincidence? Found '0.10146f' by curve fitting
        const float k_PhillipsWindFalloffCoefficient = 0.00034060072f; // PI/(9.8^4);

        // Maximum height of a wave
        const float k_WaterAmplitudeNormalization = 10.0f;

        // Currently the max wave height function is broken and need to be evaluated in a better way
        // This values has been found experimentally
        const float k_MaxWaterSurfaceElevation = 16.0f;

        // Maximum choppiness value
        const float k_WaterMaxChoppinessValue = 3.0f;

        // Constant that converts km/h to m/s
        const float k_KilometerPerHourToMeterPerSecond = 0.277778f;

        // Resolution of the mesh used to render the caustics grid
        const int k_WaterCausticsMesh = 256;

        // Resolution of the mesh used to render the tessellated water surface
        const int k_WaterTessellatedMeshResolution = 128;

        // Simulation shader and kernels
        ComputeShader m_WaterSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsFoamKernel;
        int m_CopyAdditionalDataKernel;
        int m_FindVerticalDisplacementsKernel;
        int m_PrepareCausticsGeometryKernel;
        int m_EvaluateInstanceDataKernel;

        // FFT shader and kernels
        ComputeShader m_FourierTransformCS;
        int m_RowPassTi_Kernel;
        int m_ColPassTi_Kernel;

        // Rendering kernels
        ComputeShader m_WaterLightingCS;
        int m_WaterPrepareSSRKernel;
        int m_WaterDeferredLightingKernel;
        int m_UnderWaterKernel;

        // Intermediate RTHandles used to render the water
        RTHandle m_HtRs = null;
        RTHandle m_HtIs = null;
        RTHandle m_FFTRowPassRs = null;
        RTHandle m_FFTRowPassIs = null;
        RTHandle m_AdditionalData = null;

        // The shader passes used to render the water
        Material m_InternalWaterMaterial;
        const int k_WaterGBuffer = 0;
        Mesh m_TessellableMesh;
        ComputeBuffer m_WaterIndirectDispatchBuffer;
        ComputeBuffer m_WaterPatchDataBuffer;
        ComputeBuffer m_WaterCameraFrustrumBuffer;
        FrustumGPU[] m_WaterCameraFrustumCPU = new FrustumGPU[1];

        // Other internal rendering data
        bool m_ActiveWaterSimulation = false;
        MaterialPropertyBlock m_WaterMaterialPropertyBlock;
        ShaderVariablesWater m_ShaderVariablesWater = new ShaderVariablesWater();
        WaterSimulationResolution m_WaterBandResolution = WaterSimulationResolution.Medium128;

        // Handles the water profiles
        const int k_MaxNumWaterSurfaceProfiles = 16;
        WaterSurfaceProfile[] m_WaterSurfaceProfileArray = new WaterSurfaceProfile[k_MaxNumWaterSurfaceProfiles];
        ComputeBuffer m_WaterProfileArrayGPU;

        // Caustics data
        GraphicsBuffer m_CausticsGeometry;
        bool m_CausticsBufferGeometryInitialized;
        Material m_CausticsMaterial;

        // Water line and under water
        ComputeBuffer m_WaterCameraHeightBuffer;

        void InitializeWaterSystem()
        {
            m_ActiveWaterSimulation = m_Asset.currentPlatformRenderPipelineSettings.supportWater;

            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_ActiveWaterSimulation)
                return;

            m_WaterBandResolution = m_Asset.currentPlatformRenderPipelineSettings.waterSimulationResolution;

            // Simulation shader and kernels
            m_WaterSimulationCS = m_Asset.renderPipelineResources.shaders.waterSimulationCS;
            m_InitializePhillipsSpectrumKernel = m_WaterSimulationCS.FindKernel("InitializePhillipsSpectrum");
            m_EvaluateDispersionKernel = m_WaterSimulationCS.FindKernel("EvaluateDispersion");
            m_EvaluateNormalsFoamKernel = m_WaterSimulationCS.FindKernel("EvaluateNormalsFoam");
            m_CopyAdditionalDataKernel = m_WaterSimulationCS.FindKernel("CopyAdditionalData");
            m_FindVerticalDisplacementsKernel = m_WaterSimulationCS.FindKernel("FindVerticalDisplacements");
            m_PrepareCausticsGeometryKernel = m_WaterSimulationCS.FindKernel("PrepareCausticsGeometry");
            m_EvaluateInstanceDataKernel = m_WaterSimulationCS.FindKernel("EvaluateInstanceData");

            // Water rendering
            m_WaterLightingCS = m_Asset.renderPipelineResources.shaders.waterLightingCS;
            m_WaterPrepareSSRKernel = m_WaterLightingCS.FindKernel("WaterPrepareSSR");
            m_WaterDeferredLightingKernel = m_WaterLightingCS.FindKernel("WaterDeferredLighting");
            m_UnderWaterKernel = m_WaterLightingCS.FindKernel("UnderWater");

            // FFT shader and kernels
            m_FourierTransformCS = m_Asset.renderPipelineResources.shaders.fourierTransformCS;
            GetFFTKernels(m_FourierTransformCS, m_WaterBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

            // Allocate all the RTHanles required for the water rendering
            int textureRes = (int)m_WaterBandResolution;
            m_HtRs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtIs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassRs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassIs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_AdditionalData = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);

            // Allocate the additional rendering data
            m_WaterMaterialPropertyBlock = new MaterialPropertyBlock();
            m_InternalWaterMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.waterPS);
            InitializeInstancingData();

            // Water profile management
            m_WaterProfileArrayGPU = new ComputeBuffer(k_MaxNumWaterSurfaceProfiles, System.Runtime.InteropServices.Marshal.SizeOf<WaterSurfaceProfile>());

            // Create the caustics water geometry
            m_CausticsGeometry = new GraphicsBuffer(GraphicsBuffer.Target.Raw, k_WaterCausticsMesh * k_WaterCausticsMesh * 6, sizeof(int));
            m_CausticsBufferGeometryInitialized = false;
            m_CausticsMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.waterCausticsPS);

            // Waterline / Underwater
            m_WaterCameraHeightBuffer = new ComputeBuffer(1 * 4, sizeof(float));

            // Make sure the CPU simulation stuff is properly initialized
            InitializeCPUWaterSimulation();
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
                if (waterSurface.simulation != null)
                {
                    waterSurface.simulation.ReleaseSimulationResources();
                    waterSurface.simulation = null;
                }
            }

            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_ActiveWaterSimulation)
                return;

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
            CoreUtils.Destroy(m_InternalWaterMaterial);

            // Release the water profile array
            CoreUtils.SafeRelease(m_WaterProfileArrayGPU);

            // Release all the RTHandles
            RTHandles.Release(m_AdditionalData);
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
        }

        void InitializeInstancingData()
        {
            // Allocate the indirect instancing buffer
            m_WaterIndirectDispatchBuffer = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);

            // Initialize the parts of the buffer with valid values
            uint[] indirectBufferCPU = new uint[5];
            indirectBufferCPU[0] = k_WaterTessellatedMeshResolution * k_WaterTessellatedMeshResolution * 6;
            indirectBufferCPU[1] = 1;
            indirectBufferCPU[2] = 0;
            indirectBufferCPU[3] = 0;
            indirectBufferCPU[4] = 0;

            // Push the values to the GPU
            m_WaterIndirectDispatchBuffer.SetData(indirectBufferCPU);

            // Allocate the per instance data
            m_WaterPatchDataBuffer = new ComputeBuffer(7 * 7, sizeof(float) * 4, ComputeBufferType.Structured);

            // Allocate the frustum buffer
            m_WaterCameraFrustrumBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(FrustumGPU)));
        }

        void UpdateShaderVariablesWater(WaterSurface currentWater, int surfaceIndex, ref ShaderVariablesWater cb)
        {
            // Resolution at which the simulation is evaluated
            cb._BandResolution = (uint)m_WaterBandResolution;

            // Amplitude multiplier (per band)
            cb._WaveAmplitude = currentWater.simulation.waveAmplitude;

            // Max wave height for the system
            cb._MaxWaveHeight = currentWater.simulation.maxWaveHeight;

            // Choppiness factor
            float actualChoppiness = currentWater.choppiness * k_WaterMaxChoppinessValue;
            cb._Choppiness = actualChoppiness;

            // Horizontal displacement due to each band
            cb._WaveDisplacement = cb._WaveAmplitude * actualChoppiness;
            cb._MaxWaveDisplacement = cb._MaxWaveHeight * actualChoppiness;

            // Current simulation time
            cb._SimulationTime = currentWater.simulation.simulationTime;

            // Controls how much the wind affect the current of the waves
            cb._DirectionDampener = 1.0f - currentWater.windAffectCurrent;

            // Water smoothness
            cb._WaterSmoothness = currentWater.waterSmoothness;

            // Compute the size of the patches and their scale
            cb._BandPatchSize = currentWater.simulation.patchSizes;

            // Wind direction
            cb._WindDirection = OrientationToDirection(currentWater.windOrientation);

            // Manually set wind by the user
            cb._WindSpeed = currentWater.simulation.patchWindSpeed;
            cb._WindSpeedMultiplier = currentWater.windSpeed / 100.0f;

            // Foam Jacobian offset depends on the number of bands
            if (currentWater.highBandCount)
                cb._SimulationFoamAmount = 10.0f * Mathf.Pow(0.72f + currentWater.simulationFoamAmount * 0.28f, 0.25f);
            else
                cb._SimulationFoamAmount = 8.0f * Mathf.Pow(0.72f + currentWater.simulationFoamAmount * 0.28f, 0.25f);

            // Amount of drag for the foam
            cb._JacobianDrag = currentWater.simulationFoamDrag == 0.0f ? 0.0f : Mathf.Lerp(0.96f, 0.991f, currentWater.simulationFoamDrag);

            // Smoothness of the foam
            cb._SimulationFoamSmoothness = currentWater.simulationFoamSmoothness;
            cb._FoamTilling = 1.0f / (currentWater.waterMaxPatchSize / 25.0f);
            float foamSpeed = currentWater.simulation.simulationTime * Mathf.Sqrt(cb._WindSpeed.x * k_PhillipsGravityConstant) * currentWater.windAffectCurrent;
            cb._FoamOffsets = new Vector2(cb._WindDirection.x * foamSpeed * 0.5f, cb._WindDirection.y * foamSpeed * 0.5f);
            cb._WindFoamAttenuation = Mathf.Clamp(currentWater.windFoamCurve.Evaluate(currentWater.windSpeed / 100.0f), 0.0f, 1.0f);

            // We currently only support properly up to 16 unique water surfaces
            cb._SurfaceIndex = surfaceIndex & 0xF;

            cb._SSSMaskCoefficient = 1000.0f;

            Color remappedScattering = RemapScatteringColor(currentWater.scatteringColor);
            cb._ScatteringColorTips = new Vector4(remappedScattering.r, remappedScattering.g, remappedScattering.b, 0 /*Unsused*/);
            cb._DeltaTime = currentWater.simulation.deltaTime;

            cb._MaxRefractionDistance = Mathf.Min(currentWater.absorptionDistance, currentWater.maxRefractionDistance);

            cb._OutScatteringCoefficient = -Mathf.Log(0.02f) / currentWater.absorptionDistance;
            cb._TransparencyColor = new Vector3(Mathf.Min(currentWater.refractionColor.r, 0.99f), Mathf.Min(currentWater.refractionColor.g, 0.99f), Mathf.Min(currentWater.refractionColor.b, 0.99f));

            cb._HeightBasedScattering = currentWater.heightScattering;
            cb._DisplacementScattering = currentWater.displacementScattering * 0.25f;

            float scatteringLambertLightingNear = 0.6f;
            float scatteringLambertLightingFar = 0.06f;
            cb._ScatteringLambertLighting = new Vector4(scatteringLambertLightingNear, scatteringLambertLightingFar, Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingNear), Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingFar));

            // Defines the amount of foam based on the wind speed.
            cb._FoamJacobianLambda = new Vector4(cb._BandPatchSize.x, cb._BandPatchSize.y, cb._BandPatchSize.z, cb._BandPatchSize.w);

            cb._CausticsRegionSize = cb._BandPatchSize[currentWater.causticsBand];

            // Values that guarantee the simulation coherence independently of the resolution
            cb._WaterRefSimRes = (int)WaterSimulationResolution.High256;
            cb._WaterSampleOffset = EvaluateWaterNoiseSampleOffset(m_WaterBandResolution);
            cb._WaterSpectrumOffset = EvaluateFrequencyOffset(m_WaterBandResolution);
            cb._WaterBoundCount = currentWater.highBandCount ? 4 : 2;
        }

        void UpdateGPUWaterSimulation(CommandBuffer cmd, WaterSurface currentWater, bool gpuResourcesInvalid, ShaderVariablesWater shaderVariablesWater)
        {
            // Bind the constant buffer
            ConstantBuffer.Push(cmd, shaderVariablesWater, m_WaterSimulationCS, HDShaderIDs._ShaderVariablesWater);

            // Evaluate the band count
            int bandCount = currentWater.highBandCount ? k_WaterHighBandCount : k_WaterLowBandCount;

            // Number of tiles we will need to dispatch
            int tileCount = (int)m_WaterBandResolution / 8;

            // Do we need to re-evaluate the Phillips spectrum?
            if (gpuResourcesInvalid)
            {
                // Convert the noise to the Phillips spectrum
                cmd.SetComputeTextureParam(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, HDShaderIDs._H0BufferRW, currentWater.simulation.gpuBuffers.phillipsSpectrumBuffer);
                cmd.DispatchCompute(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, tileCount, tileCount, bandCount);
            }

            // Execute the dispersion
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._H0Buffer, currentWater.simulation.gpuBuffers.phillipsSpectrumBuffer);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtRealBufferRW, m_HtRs);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtImaginaryBufferRW, m_HtIs);
            cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateDispersionKernel, tileCount, tileCount, bandCount);

            // Make sure to define properly if this is the initial frame
            shaderVariablesWater._WaterInitialFrame = gpuResourcesInvalid ? 1 : 0;

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, shaderVariablesWater, m_FourierTransformCS, HDShaderIDs._ShaderVariablesWater);

            // First pass of the FFT
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_HtRs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_HtIs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, m_FFTRowPassRs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBufferRW, m_FFTRowPassIs);
            cmd.DispatchCompute(m_FourierTransformCS, m_RowPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

            // Second pass of the FFT
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_FFTRowPassRs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_FFTRowPassIs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, currentWater.simulation.gpuBuffers.displacementBuffer);
            cmd.DispatchCompute(m_FourierTransformCS, m_ColPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

            // Evaluate water surface additional data (combining it with the previous values)
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, HDShaderIDs._WaterDisplacementBuffer, currentWater.simulation.gpuBuffers.displacementBuffer);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, HDShaderIDs._PreviousWaterAdditionalDataBuffer, currentWater.simulation.gpuBuffers.additionalDataBuffer);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, HDShaderIDs._WaterAdditionalDataBufferRW, m_AdditionalData);
            cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, tileCount, tileCount, bandCount);

            // Copy the result back into the water surface's texture
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_CopyAdditionalDataKernel, HDShaderIDs._WaterAdditionalDataBuffer, m_AdditionalData);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_CopyAdditionalDataKernel, HDShaderIDs._WaterAdditionalDataBufferRW, currentWater.simulation.gpuBuffers.additionalDataBuffer);
            cmd.DispatchCompute(m_WaterSimulationCS, m_CopyAdditionalDataKernel, tileCount, tileCount, bandCount);

            // Make sure the mip-maps are generated
            currentWater.simulation.gpuBuffers.additionalDataBuffer.rt.GenerateMips();
        }

        void UpdateWaterSurface(CommandBuffer cmd, WaterSurface currentWater, int surfaceIndex)
        {
            // If the function returns false, this means the resources were just created and they need to be initialized.
            bool validGPUResources, validCPUResources;
            currentWater.CheckResources((int)m_WaterBandResolution, k_WaterHighBandCount, m_ActiveWaterSimulationCPU, out validGPUResources, out validCPUResources);

            // Update the simulation time
            currentWater.simulation.Update(Time.realtimeSinceStartup, currentWater.timeMultiplier);

            // Update the constant buffer
            UpdateShaderVariablesWater(currentWater, surfaceIndex, ref m_ShaderVariablesWater);

            // Update the GPU simulation for the water
            UpdateGPUWaterSimulation(cmd, currentWater, !validGPUResources, m_ShaderVariablesWater);

            // Here we replicate the ocean simulation on the CPU (if requested)
            UpdateCPUWaterSimulation(currentWater, !validCPUResources, m_ShaderVariablesWater);
        }

        void EvaluateWaterCaustics(CommandBuffer cmd, WaterSurface currentWater)
        {
            // Initialize the indices buffer
            if (!m_CausticsBufferGeometryInitialized)
            {
                int meshTileCount = (int)k_WaterCausticsMesh / 8;
                cmd.SetComputeIntParam(m_WaterSimulationCS, HDShaderIDs._CausticGeometryResolution, k_WaterCausticsMesh);
                cmd.SetComputeBufferParam(m_WaterSimulationCS, m_PrepareCausticsGeometryKernel, "_CauticsGeometryRW", m_CausticsGeometry);
                cmd.DispatchCompute(m_WaterSimulationCS, m_PrepareCausticsGeometryKernel, meshTileCount, meshTileCount, 1);
                m_CausticsBufferGeometryInitialized = true;
            }

            // Make sure that the caustics texture is allocated
            int causticsResolution = (int)currentWater.causticsResolution;
            currentWater.simulation.CheckCausticsResources(true, causticsResolution);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_CausticsMaterial, HDShaderIDs._ShaderVariablesWater);

            // Render the caustics
            CoreUtils.SetRenderTarget(cmd, currentWater.simulation.gpuBuffers.causticsBuffer, clearFlag: ClearFlag.Color, Color.black);
            m_WaterMaterialPropertyBlock.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, currentWater.simulation.gpuBuffers.additionalDataBuffer);
            m_WaterMaterialPropertyBlock.SetFloat(HDShaderIDs._CausticsVirtualPlane, currentWater.virtualPlaneDistance);
            m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticsNormalsMipOffset, EvaluateNormalMipOffset(m_WaterBandResolution));
            m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticGeometryResolution, k_WaterCausticsMesh);
            m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticsBandIndex, currentWater.causticsBand);
            cmd.DrawProcedural(m_CausticsGeometry, Matrix4x4.identity, m_CausticsMaterial, 0, MeshTopology.Triangles, k_WaterCausticsMesh * k_WaterCausticsMesh * 6, 1, m_WaterMaterialPropertyBlock);

            // Make sure the mip-maps are generated
            currentWater.simulation.gpuBuffers.causticsBuffer.rt.GenerateMips();
        }

        void UpdateWaterSurfaces(CommandBuffer cmd)
        {
            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // If water surface simulation is disabled, skip.
            if (!m_ActiveWaterSimulation || numWaterSurfaces == 0)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceSimulation)))
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

                    // Render the caustics from the current simulation state if required
                    if (currentWater.caustics && currentWater.causticsAlgorithm == WaterSurface.WaterCausticsType.Simulation)
                        EvaluateWaterCaustics(cmd, currentWater);
                    else
                        currentWater.simulation.CheckCausticsResources(false, 0);
                }
            }
        }

        struct WaterRenderingGBufferParameters
        {
            // Camera parameters
            public Vector3 cameraPosition;
            public Frustum cameraFrustum;
            public float cameraFarPlane;

            // Geometry parameters
            public uint numLODs;
            public float gridSize;
            public bool highBandCount;
            public Vector3 center;
            public Vector2 extent;
            public float rotation;
            public Vector2 foamMaskOffset;
            public Vector2 waterMaskOffset;
            public bool infinite;
            public bool customMesh;
            public Mesh targetMesh;

            // Underwater data
            public bool evaluateCameraPosition;

            // Caustics parameters
            public bool causticsEnabled;
            public bool simulationCaustics;

            // Water patches
            public ComputeShader waterSimulation;
            public int patchEvaluation;

            // Water Mask
            public Texture2D waterMask;

            // Foam data
            public Texture2D foamMask;
            public Texture2D surfaceFoam;

            // Material data
            public Material waterMaterial;
            public MaterialPropertyBlock mbp;

            // Constant buffer
            public ShaderVariablesWater waterCB;
            public ShaderVariablesWaterRendering waterRenderingCB;

            // Waterline
            public ComputeShader simulationCS;
            public int findVerticalDisplKernel;
        }

        WaterRenderingGBufferParameters PrepareWaterRenderingGBufferParameters(HDCamera hdCamera, WaterRendering settings, WaterSurface currentWater, int surfaceIndex, bool insideUnderWaterVolume)
        {
            WaterRenderingGBufferParameters parameters = new WaterRenderingGBufferParameters();

            // Camera parameters
            parameters.cameraPosition = hdCamera.camera.transform.position;
            parameters.cameraFrustum = hdCamera.frustum;
            parameters.cameraFarPlane = hdCamera.camera.farClipPlane;

            // Geometry parameters
            parameters.numLODs = (uint)settings.numLevelOfDetails.value;
            parameters.gridSize = settings.gridSize.value;
            parameters.highBandCount = currentWater.highBandCount;
            parameters.center = currentWater.transform.position;
            parameters.extent = new Vector2(currentWater.transform.lossyScale.x, currentWater.transform.lossyScale.z);
            parameters.rotation = -currentWater.transform.eulerAngles.y * Mathf.Deg2Rad;
            parameters.foamMaskOffset = currentWater.foamMaskOffset;
            parameters.waterMaskOffset = currentWater.waterMaskOffset;

            // For now, we are disabling infinite surfaces for metal as there seems to be a bug in the API that
            // breaks on the DrawMeshInstancedIndirect function.
            if (currentWater.infinite && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal)
            {
                parameters.infinite = true;
                parameters.targetMesh = m_TessellableMesh;
            }
            else
            {
                parameters.infinite = false;
                if (currentWater.geometryType == WaterSurface.WaterGeometryType.Quad || currentWater.geometry == null)
                {
                    parameters.customMesh = false;
                    parameters.targetMesh = m_TessellableMesh;
                }
                else
                {
                    parameters.customMesh = true;
                    parameters.targetMesh = currentWater.geometry;
                }
            }

            // Under water data
            parameters.evaluateCameraPosition = insideUnderWaterVolume;

            // Patch evaluation parameters
            parameters.waterSimulation = m_WaterSimulationCS;
            parameters.patchEvaluation = m_EvaluateInstanceDataKernel;

            // Caustics parameters
            parameters.causticsEnabled = currentWater.caustics;
            parameters.simulationCaustics = currentWater.caustics && currentWater.causticsAlgorithm == WaterSurface.WaterCausticsType.Simulation;

            // All the required global textures
            parameters.waterMask = currentWater.waterMask != null ? currentWater.waterMask : Texture2D.whiteTexture;
            parameters.surfaceFoam = m_Asset.renderPipelineResources.textures.foamSurface;
            parameters.foamMask = currentWater.foamMask != null ? currentWater.foamMask : Texture2D.whiteTexture;

            // Water material
            parameters.waterMaterial = currentWater.customMaterial != null ? currentWater.customMaterial : m_InternalWaterMaterial;

            // Property bloc used for binding the textures
            parameters.mbp = m_WaterMaterialPropertyBlock;

            // Setup the simulation water constant buffer
            UpdateShaderVariablesWater(currentWater, surfaceIndex, ref parameters.waterCB);

            // Setup the water rendering constant buffers (parameters that we can setup
            parameters.waterRenderingCB._WaterAmbientProbe = EvaluateWaterAmbientProbe(hdCamera, settings.ambientProbeDimmer.value);
            parameters.waterRenderingCB._CausticsIntensity = currentWater.causticsIntensity;
            parameters.waterRenderingCB._CausticsTiling = currentWater.causticsTiling;
            parameters.waterRenderingCB._CausticsPlaneBlendDistance = currentWater.causticsPlaneBlendDistance;
            parameters.waterRenderingCB._InfiniteSurface = currentWater.infinite ? 1 : 0;
            parameters.waterRenderingCB._WaterCausticsType = parameters.causticsEnabled ? (parameters.simulationCaustics ? 0 : 1) : 0;
            parameters.waterRenderingCB._CameraInUnderwaterRegion = insideUnderWaterVolume ? 1 : 0;

            // Rotation, size and offsets (patch, water mask and foam mask)
            if (parameters.infinite)
            {
                parameters.waterRenderingCB._GridSize.Set(parameters.gridSize, parameters.gridSize);
                parameters.waterRenderingCB._WaterRotation.Set(1.0f, 0.0f);
                parameters.waterRenderingCB._PatchOffset.Set(parameters.cameraPosition.x, parameters.center.y, parameters.cameraPosition.z, 0.0f);

            }
            else
            {
                parameters.waterRenderingCB._GridSize.Set(parameters.extent.x, parameters.extent.y);
                parameters.waterRenderingCB._WaterRotation.Set(Mathf.Cos(parameters.rotation), Mathf.Sin(parameters.rotation));
                parameters.waterRenderingCB._PatchOffset = parameters.center;

            }
            parameters.waterRenderingCB._WaterMaskOffset.Set(parameters.waterMaskOffset.x, -parameters.waterMaskOffset.y);
            parameters.waterRenderingCB._WaterMaskScale.Set(1.0f / currentWater.waterMaskExtent.x, 1.0f / currentWater.waterMaskExtent.y);
            parameters.waterRenderingCB._FoamMaskOffset.Set(parameters.foamMaskOffset.x, -parameters.foamMaskOffset.y);
            parameters.waterRenderingCB._FoamMaskScale.Set(1.0f / currentWater.foamMaskExtent.x, 1.0f / currentWater.foamMaskExtent.y);

            // Tessellation
            parameters.waterRenderingCB._WaterMaxTessellationFactor = settings.maxTessellationFactor.value;
            parameters.waterRenderingCB._WaterTessellationFadeStart = settings.tessellationFactorFadeStart.value;
            parameters.waterRenderingCB._WaterTessellationFadeRange = settings.tessellationFactorFadeRange.value;

            // Set up the LOD Data
            if (parameters.infinite)
            {
                parameters.waterRenderingCB._WaterLODCount = parameters.numLODs;
                parameters.waterRenderingCB._NumWaterPatches = EvaluateNumberWaterPatches(parameters.numLODs);
            }

            // Tessellation
            parameters.waterRenderingCB._WaterMaxTessellationFactor = settings.maxTessellationFactor.value;
            parameters.waterRenderingCB._WaterTessellationFadeStart = settings.tessellationFactorFadeStart.value;
            parameters.waterRenderingCB._WaterTessellationFadeRange = settings.tessellationFactorFadeRange.value;

            // Compute the caustics offsets
            float causticsOffset = currentWater.simulation.simulationTime * currentWater.causticsSpeed * k_KilometerPerHourToMeterPerSecond;
            Vector2 causticOrientation = OrientationToDirection(currentWater.windOrientation);
            parameters.waterRenderingCB._CausticsOffset.Set(causticsOffset * causticOrientation.x, causticsOffset * causticOrientation.y);

            // Bind the decal layer data
            parameters.waterRenderingCB._WaterDecalLayer = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers) ? ((uint)currentWater.decalLayerMask) : ShaderVariablesGlobal.DefaultDecalLayers;

            // Waterline & underwater
            parameters.simulationCS = m_WaterSimulationCS;
            parameters.findVerticalDisplKernel = m_FindVerticalDisplacementsKernel;

            return parameters;
        }

        struct WaterRenderingDeferredParameters
        {
            // Camera parameters
            public int width;
            public int height;
            public int viewCount;
            public bool pbsActive;

            // Material data
            public ComputeShader waterLighting;
            public int waterLightingKernel;
        }

        WaterRenderingDeferredParameters PrepareWaterRenderingDeferredParameters(HDCamera hdCamera)
        {
            WaterRenderingDeferredParameters parameters = new WaterRenderingDeferredParameters();

            // Keep track of the camera data
            parameters.width = hdCamera.actualWidth;
            parameters.height = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            // Is the physically based sky active? (otherwise we need to bind some fall back textures)
            var visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
            parameters.pbsActive = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased;

            parameters.waterLighting = m_WaterLightingCS;
            parameters.waterLightingKernel = m_WaterDeferredLightingKernel;

            return parameters;
        }

        class WaterRenderingGBufferData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingGBufferParameters parameters;

            // Simulation buffers
            public TextureHandle displacementTexture;
            public TextureHandle additionalData;
            public TextureHandle causticsData;

            // Other resources
            public ComputeBufferHandle indirectBuffer;
            public ComputeBufferHandle patchDataBuffer;
            public ComputeBufferHandle frustumBuffer;
            public TextureHandle depthPyramid;

            // Water rendered to this buffer
            public TextureHandle colorPyramid;
            public TextureHandle depthBuffer;

            // Output buffers
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public ComputeBufferHandle heightBuffer;
        }

        class WaterRenderingSSRData
        {
            // Camera data
            public int width;
            public int height;
            public int viewCount;

            // Shader data
            public ComputeShader waterLighting;
            public int prepareSSRKernel;

            // Input textures
            public TextureHandle depthBuffer;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer3;
            public ComputeBufferHandle heightBuffer;
            public ComputeBufferHandle waterSurfaceProfiles;

            // Output texture
            public TextureHandle normalBuffer;
        }

        unsafe Vector3 EvaluateWaterAmbientProbe(HDCamera hdCamera, float ambientProbeDimmer)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, ambientProbeDimmer);
            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsProbe, probeSH);
            return EvaluateAmbientProbe(m_PackedCoeffsProbe, Vector3.down);
        }

        void FillWaterSurfaceProfile(HDCamera hdCamera, WaterRendering settings, WaterSurface waterSurface, int waterSurfaceIndex)
        {
            WaterSurfaceProfile profile = new WaterSurfaceProfile();
            profile.bodyScatteringHeight = waterSurface.directLightBodyScattering;
            profile.tipScatteringHeight = waterSurface.directLightTipScattering;
            profile.waterAmbientProbe = EvaluateWaterAmbientProbe(hdCamera, settings.ambientProbeDimmer.value);
            profile.maxRefractionDistance = Mathf.Min(waterSurface.absorptionDistance, waterSurface.maxRefractionDistance);
            profile.lightLayers = (uint)waterSurface.lightLayerMask;
            profile.cameraUnderWater = waterSurfaceIndex == m_UnderWaterSurfaceIndex ? 1 : 0;
            profile.transparencyColor = new Vector3(Mathf.Min(waterSurface.refractionColor.r, 0.99f),
                                                    Mathf.Min(waterSurface.refractionColor.g, 0.99f),
                                                    Mathf.Min(waterSurface.refractionColor.b, 0.99f));
            profile.outScatteringCoefficient = -Mathf.Log(0.02f) / waterSurface.absorptionDistance;
            profile.scatteringColor = new Vector3(waterSurface.scatteringColor.r, waterSurface.scatteringColor.g, waterSurface.scatteringColor.b);
            m_WaterSurfaceProfileArray[waterSurfaceIndex] = profile;
        }

        void RenderWaterSurfaceGBuffer(RenderGraph renderGraph, HDCamera hdCamera,
                                        WaterSurface currentWater, WaterRendering settings, int surfaceIdx, bool evaluateCameraPos,
                                        TextureHandle depthBuffer, TextureHandle depthPyramid,
                                        TextureHandle WaterGbuffer0, TextureHandle WaterGbuffer1, TextureHandle WaterGbuffer2, TextureHandle WaterGbuffer3)
        {
            using (var builder = renderGraph.AddRenderPass<WaterRenderingGBufferData>("Render Water Surface GBuffer", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingGBuffer)))
            {
                // Make sure the mesh is there if needed
                if (m_TessellableMesh == null)
                    BuildGridMesh(ref m_TessellableMesh);

                // Prepare all the internal parameters
                passData.parameters = PrepareWaterRenderingGBufferParameters(hdCamera, settings, currentWater, surfaceIdx, evaluateCameraPos);

                // Allocate all the intermediate textures
                passData.gbuffer0 = builder.UseColorBuffer(WaterGbuffer0, 0);
                passData.gbuffer1 = builder.UseColorBuffer(WaterGbuffer1, 1);
                passData.gbuffer2 = builder.UseColorBuffer(WaterGbuffer2, 2);
                passData.gbuffer3 = builder.UseColorBuffer(WaterGbuffer3, 3);
                passData.heightBuffer = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterCameraHeightBuffer));

                // Import all the textures into the system
                passData.displacementTexture = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.displacementBuffer);
                passData.additionalData = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.additionalDataBuffer);
                passData.causticsData = passData.parameters.simulationCaustics ? renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.causticsBuffer) : renderGraph.defaultResources.blackTexture;
                passData.indirectBuffer = renderGraph.ImportComputeBuffer(m_WaterIndirectDispatchBuffer);
                passData.patchDataBuffer = renderGraph.ImportComputeBuffer(m_WaterPatchDataBuffer);
                passData.frustumBuffer = renderGraph.ImportComputeBuffer(m_WaterCameraFrustrumBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);

                // Request the output textures
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                    (WaterRenderingGBufferData data, RenderGraphContext ctx) =>
                    {
                        // Raise the keyword if it should be raised
                        CoreUtils.SetKeyword(ctx.cmd, "HIGH_RESOLUTION_WATER", data.parameters.highBandCount);

                        // First we need to evaluate if we are in the underwater region of this water surface if the camera
                        // is above of under water. This will need to be done on the CPU later
                        if (data.parameters.evaluateCameraPosition)
                        {
                            // Makes both constant buffers are properly injected
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.simulationCS, HDShaderIDs._ShaderVariablesWater);
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.simulationCS, HDShaderIDs._ShaderVariablesWaterRendering);

                            // Evaluate the camera height, should be done on the CPU later
                            ctx.cmd.SetComputeBufferParam(data.parameters.simulationCS, data.parameters.findVerticalDisplKernel, HDShaderIDs._WaterCameraHeightBufferRW, data.heightBuffer);
                            ctx.cmd.SetComputeTextureParam(data.parameters.simulationCS, data.parameters.findVerticalDisplKernel, HDShaderIDs._WaterDisplacementBuffer, data.displacementTexture);
                            ctx.cmd.SetComputeTextureParam(data.parameters.simulationCS, data.parameters.findVerticalDisplKernel, HDShaderIDs._WaterMask, data.parameters.waterMask);
                            ctx.cmd.DispatchCompute(data.parameters.simulationCS, data.parameters.findVerticalDisplKernel, 1, 1, 1);
                        }

                        // Prepare the material property block for the rendering
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterDisplacementBuffer, data.displacementTexture);
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, data.additionalData);
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterCausticsDataBuffer, data.causticsData);

                        // Bind the global water textures
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterMask, data.parameters.waterMask);
                        data.parameters.mbp.SetTexture(HDShaderIDs._FoamTexture, data.parameters.surfaceFoam);
                        data.parameters.mbp.SetTexture(HDShaderIDs._FoamMask, data.parameters.foamMask);
                        data.parameters.mbp.SetBuffer(HDShaderIDs._WaterPatchData, data.patchDataBuffer);
                        data.parameters.mbp.SetBuffer(HDShaderIDs._WaterCameraHeightBuffer, data.heightBuffer);
                        data.parameters.mbp.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramid);

                        // Raise the right stencil flags
                        ctx.cmd.SetGlobalFloat("_StencilWaterRefGBuffer", (int)(StencilUsage.WaterSurface | StencilUsage.TraceReflectionRay));
                        ctx.cmd.SetGlobalFloat("_StencilWaterWriteMaskGBuffer", (int)(StencilUsage.WaterSurface | StencilUsage.TraceReflectionRay));
                        ctx.cmd.SetGlobalFloat("_CullWaterMask", data.parameters.evaluateCameraPosition ? (int)CullMode.Off : (int)CullMode.Back);

                        // Raise the keyword if it should be raised
                        CoreUtils.SetKeyword(ctx.cmd, "HIGH_RESOLUTION_WATER", data.parameters.highBandCount);

                        // Bind the water constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWater);

                        // Bind the render targets
                        var mrtArray = ctx.renderGraphPool.GetTempArray<RenderTargetIdentifier>(4);
                        mrtArray[0] = data.gbuffer0;
                        mrtArray[1] = data.gbuffer1;
                        mrtArray[2] = data.gbuffer2;
                        mrtArray[3] = data.gbuffer3;
                        CoreUtils.SetRenderTarget(ctx.cmd, mrtArray, data.depthBuffer);

                        // Bind the two constant buffers
                        ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWater);
                        ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);

                        if (data.parameters.infinite)
                        {
                            // Makes both constant buffers are properly injected
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.waterSimulation, HDShaderIDs._ShaderVariablesWater);
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.waterSimulation, HDShaderIDs._ShaderVariablesWaterRendering);

                            ctx.cmd.SetComputeBufferParam(data.parameters.waterSimulation, data.parameters.patchEvaluation, HDShaderIDs._FrustumGPUBuffer, data.frustumBuffer);
                            ctx.cmd.SetComputeBufferParam(data.parameters.waterSimulation, data.parameters.patchEvaluation, HDShaderIDs._WaterPatchDataRW, data.patchDataBuffer);
                            ctx.cmd.SetComputeBufferParam(data.parameters.waterSimulation, data.parameters.patchEvaluation, HDShaderIDs._WaterInstanceDataRW, data.indirectBuffer);
                            ctx.cmd.DispatchCompute(data.parameters.waterSimulation, data.parameters.patchEvaluation, 1, 1, 1);

                            // Draw all the patches
                            ctx.cmd.DrawMeshInstancedIndirect(data.parameters.targetMesh, 0, data.parameters.waterMaterial, k_WaterGBuffer, data.indirectBuffer, 0, data.parameters.mbp);
                        }
                        else
                        {
                            // Based on if this is a custom mesh or not trigger the right geometry/geometries and shader pass
                            if (!data.parameters.customMesh)
                                ctx.cmd.DrawMesh(data.parameters.targetMesh, Matrix4x4.identity, data.parameters.waterMaterial, 0, k_WaterGBuffer, data.parameters.mbp);
                            else
                            {
                                int numSubMeshes = data.parameters.targetMesh.subMeshCount;
                                for (int subMeshIdx = 0; subMeshIdx < numSubMeshes; ++subMeshIdx)
                                    ctx.cmd.DrawMesh(data.parameters.targetMesh, Matrix4x4.identity, data.parameters.waterMaterial, subMeshIdx, k_WaterGBuffer, data.parameters.mbp);
                            }
                        }
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

        struct WaterGBuffer
        {
            public TextureHandle waterGBuffer0;
            public TextureHandle waterGBuffer1;
            public TextureHandle waterGBuffer2;
            public TextureHandle waterGBuffer3;
        }

        WaterGBuffer RenderWaterGBuffer(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle colorPyramid, TextureHandle depthPyramid)
        {
            // Allocate the return structure
            WaterGBuffer outputGBuffer = new WaterGBuffer();

            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // If the water is disabled, no need to render or simulate
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water) || numWaterSurfaces == 0)
            {
                // Set the textures handles for the water gbuffer
                outputGBuffer.waterGBuffer0 = renderGraph.defaultResources.blackTextureXR;
                outputGBuffer.waterGBuffer1 = renderGraph.defaultResources.blackTextureXR;
                outputGBuffer.waterGBuffer2 = renderGraph.defaultResources.blackTextureXR;
                outputGBuffer.waterGBuffer3 = renderGraph.defaultResources.blackTextureXR;
                return outputGBuffer;
            }

            // Evaluate which surface will have under water rendering
            EvaluateUnderWaterSurface(hdCamera);

            // Copy the frustum data to the GPU
            PropagateFrustumDataToGPU(hdCamera);

            // Request all the gbuffer textures we will need
            TextureHandle WaterGbuffer0 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Water GBuffer 0" });
            TextureHandle WaterGbuffer1 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "Water GBuffer 1" });
            TextureHandle WaterGbuffer2 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "Water GBuffer 2" });
            TextureHandle WaterGbuffer3 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "Water GBuffer 3" });

            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // If the resources are invalid, we cannot render this surface
                if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, k_WaterHighBandCount))
                    continue;

                // Fill the water surface profile
                FillWaterSurfaceProfile(hdCamera, settings, currentWater, surfaceIdx);

                // Render the water surface
                RenderWaterSurfaceGBuffer(renderGraph, hdCamera, currentWater, settings, surfaceIdx, surfaceIdx == m_UnderWaterSurfaceIndex,
                                depthBuffer, depthPyramid, WaterGbuffer0, WaterGbuffer1, WaterGbuffer2, WaterGbuffer3);
            }

            using (var builder = renderGraph.AddRenderPass<WaterRenderingSSRData>("Prepare water for SSR", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingSSR)))
            {
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualWidth;
                passData.viewCount = hdCamera.viewCount;
                passData.waterLighting = m_WaterLightingCS;
                passData.prepareSSRKernel = m_WaterPrepareSSRKernel;
                passData.gbuffer1 = builder.ReadTexture(WaterGbuffer1);
                passData.gbuffer3 = builder.ReadTexture(WaterGbuffer3);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.WriteTexture(normalBuffer);
                passData.heightBuffer = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterCameraHeightBuffer));
                passData.waterSurfaceProfiles = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterProfileArrayGPU));

                builder.SetRenderFunc(
                    (WaterRenderingSSRData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int tileX = (data.width + 7) / 8;
                        int tileY = (data.height + 7) / 8;

                        // Bind the input gbuffer data
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterGBufferTexture1, data.gbuffer1);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterGBufferTexture3, data.gbuffer3);
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterCameraHeightBuffer, data.heightBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._NormalBufferRW, data.normalBuffer);

                        // Run the lighting
                        ctx.cmd.DispatchCompute(data.waterLighting, data.prepareSSRKernel, tileX, tileY, data.viewCount);
                    });
            }

            // Set the textures handles for the water gbuffer
            outputGBuffer.waterGBuffer0 = WaterGbuffer0;
            outputGBuffer.waterGBuffer1 = WaterGbuffer1;
            outputGBuffer.waterGBuffer2 = WaterGbuffer2;
            outputGBuffer.waterGBuffer3 = WaterGbuffer3;
            return outputGBuffer;
        }

        void RenderWaterDebug(RenderGraph renderGraph, HDCamera hdCamera, bool msaa, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // If the water is disabled, no need to render or simulate
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water) || numWaterSurfaces == 0)
                return;

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

            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // If the resources are invalid, we cannot render this surface
                if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, k_WaterHighBandCount))
                    continue;

                // Fill the water surface profile
                FillWaterSurfaceProfile(hdCamera, settings, currentWater, surfaceIdx);

                // Render the water surface
                RenderWaterSurfaceGBuffer(renderGraph, hdCamera, currentWater, settings, surfaceIdx, false, depthBuffer, renderGraph.defaultResources.blackTextureXR, WaterGbuffer0, colorBuffer, WaterGbuffer2, WaterGbuffer3);
            }
        }

        class WaterRenderingDeferredData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingDeferredParameters parameters;

            // Input data
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public TextureHandle depthBuffer;
            public ComputeBufferHandle waterSurfaceProfiles;
            public TextureHandle scatteringFallbackTexture;
            public TextureHandle volumetricLightingTexture;
            public ComputeBufferHandle heightBuffer;

            // Water rendered to this buffer
            public TextureHandle colorBuffer;
        }

        void RenderWaterLighting(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle colorPyramid, TextureHandle volumetricLightingTexture, WaterGBuffer waterGBuffer)
        {
            // If the water is disabled, no need to render
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water) || WaterSurface.instanceCount == 0)
                return;

            // Push the water profiles to the GPU for the deferred lighting pass
            m_WaterProfileArrayGPU.SetData(m_WaterSurfaceProfileArray);

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<WaterRenderingDeferredData>("Render Water Surfaces Deferred", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingDeferred)))
            {
                // Prepare all the internal parameters
                passData.parameters = PrepareWaterRenderingDeferredParameters(hdCamera);

                // All the required textures
                passData.gbuffer0 = builder.ReadTexture(waterGBuffer.waterGBuffer0);
                passData.gbuffer1 = builder.ReadTexture(waterGBuffer.waterGBuffer1);
                passData.gbuffer2 = builder.ReadTexture(waterGBuffer.waterGBuffer2);
                passData.gbuffer3 = builder.ReadTexture(waterGBuffer.waterGBuffer3);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.waterSurfaceProfiles = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterProfileArrayGPU));
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;
                passData.volumetricLightingTexture = builder.ReadTexture(volumetricLightingTexture);
                passData.heightBuffer = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterCameraHeightBuffer));

                // Request the output textures
                passData.colorBuffer = builder.WriteTexture(colorBuffer);

                // Run the deferred lighting
                builder.SetRenderFunc(
                    (WaterRenderingDeferredData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int tileX = (data.parameters.width + 7) / 8;
                        int tileY = (data.parameters.height + 7) / 8;

                        if (!data.parameters.pbsActive)
                        {
                            // This has to be done in the global space given that the "correct" one happens in the global space.
                            // If we do it in the local space, there are some cases when the previous frames local take precedence over the current frame global one.
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, data.scatteringFallbackTexture);
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, data.scatteringFallbackTexture);
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, data.scatteringFallbackTexture);
                        }

                        // Bind the input gbuffer data
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterGBufferTexture0, data.gbuffer0);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterGBufferTexture1, data.gbuffer1);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterGBufferTexture2, data.gbuffer2);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterGBufferTexture3, data.gbuffer3);
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterCameraHeightBuffer, data.heightBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._VBufferLighting, data.volumetricLightingTexture);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._CameraColorTextureRW, data.colorBuffer);

                        // Run the lighting
                        ctx.cmd.DispatchCompute(data.parameters.waterLighting, data.parameters.waterLightingKernel, tileX, tileY, data.parameters.viewCount);
                    });
            }
        }
    }
}

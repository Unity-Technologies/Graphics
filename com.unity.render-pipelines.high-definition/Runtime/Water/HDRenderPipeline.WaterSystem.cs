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

        // Constant that converts km/h to m/s
        const float k_KilometerPerHourToMeterPerSecond = 0.277778f;

        /*
        1 * 0.5 + 2 * 0.5
        1 * 0.5 + 2 + 4 * 0.5
        1 * 0.5 + 2 + 4 + 8 * 0.5
        1 * 0.5 + 2 + 4 + 8 + 16 * 0.5
        1 * 0.5 + 2 + 4 + 8 + 16 + 32 * 0.5
        */
        static float[] offsets = { 0.0f, 1.5f, 4.5f, 10.5f, 22.5f };

        // Resolution of the mesh used to render the caustics grid
        const int k_WaterCausticsMesh = 256;

        // Simulation shader and kernels
        ComputeShader m_WaterSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsFoamKernel;
        int m_CopyAdditionalDataKernel;
        int m_PrepareCausticsGeometryKernel;

        // FFT shader and kernels
        ComputeShader m_FourierTransformCS;
        int m_RowPassTi_Kernel;
        int m_ColPassTi_Kernel;

        // Rendering kernels
        ComputeShader m_WaterLightingCS;
        int m_WaterPrepareSSRKernel;
        int m_WaterDeferredLightingKernel;

        // Intermediate RTHandles used to render the water
        RTHandle m_HtRs = null;
        RTHandle m_HtIs = null;
        RTHandle m_FFTRowPassRs = null;
        RTHandle m_FFTRowPassIs = null;
        RTHandle m_AdditionalData = null;

        // The shader passes used to render the water
        Material m_InternalWaterMaterial;
        const int k_WaterGBufferProcedural = 0;
        const int k_WaterGBufferMesh = 1;

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

        void GetFFTKernels(WaterSimulationResolution resolution, out int rowKernel, out int columnKernel)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.Ultra512:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_512");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_512");
                }
                break;
                case WaterSimulationResolution.High256:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_256");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_256");
                }
                break;
                case WaterSimulationResolution.Medium128:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_128");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_128");
                }
                break;
                case WaterSimulationResolution.Low64:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
                default:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
            }
        }

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
            m_PrepareCausticsGeometryKernel = m_WaterSimulationCS.FindKernel("PrepareCausticsGeometry");

            // Water rendering
            m_WaterLightingCS = m_Asset.renderPipelineResources.shaders.waterLightingCS;
            m_WaterPrepareSSRKernel = m_WaterLightingCS.FindKernel("WaterPrepareSSR");
            m_WaterDeferredLightingKernel = m_WaterLightingCS.FindKernel("WaterDeferredLighting");

            // FFT shader and kernels
            m_FourierTransformCS = m_Asset.renderPipelineResources.shaders.fourierTransformCS;
            GetFFTKernels(m_WaterBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

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

            // Water profile management
            m_WaterProfileArrayGPU = new ComputeBuffer(k_MaxNumWaterSurfaceProfiles, System.Runtime.InteropServices.Marshal.SizeOf<WaterSurfaceProfile>());

            // Create the caustics water geometry
            m_CausticsGeometry = new GraphicsBuffer(GraphicsBuffer.Target.Raw, k_WaterCausticsMesh * k_WaterCausticsMesh * 6, sizeof(int));
            m_CausticsBufferGeometryInitialized = false;
            m_CausticsMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.waterCausticsPS);
        }

        void ReleaseWaterSystem()
        {
            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_ActiveWaterSimulation)
                return;

            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // Loop through them and display them
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                WaterSurface waterSurface = waterSurfaces[surfaceIdx];
                if (waterSurface.simulation != null)
                    waterSurface.simulation.ReleaseSimulationResources();
            }

            // Release the caustics geometry
            CoreUtils.Destroy(m_CausticsMaterial);
            CoreUtils.SafeRelease(m_CausticsGeometry);

            // Release the water profile array
            CoreUtils.SafeRelease(m_WaterProfileArrayGPU);

            // Release all the RTHandles
            RTHandles.Release(m_AdditionalData);
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
            CoreUtils.Destroy(m_InternalWaterMaterial);
        }

        void UpdateShaderVariablesWater(WaterSurface currentWater, int surfaceIndex, ref ShaderVariablesWater cb)
        {
            // Resolution at which the simulation is evaluated
            cb._BandResolution = (uint)m_WaterBandResolution;

            // Maximal possible wave height of the current setup
            ComputeMaximumWaveHeight(currentWater.amplitude, currentWater.simulation.patchWindSpeed.x, currentWater.highBandCount, out cb._WaveAmplitude, out cb._MaxWaveHeight);

            // Choppiness factor
            cb._Choppiness = currentWater.choppiness;

            // Horizontal displacement due to each band
            cb._WaveDisplacement = cb._WaveAmplitude * currentWater.choppiness;
            cb._MaxWaveDisplacement = cb._MaxWaveHeight * currentWater.choppiness;

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

            cb._MaxRefractionDistance = Mathf.Min(currentWater.maxAbsorptionDistance, currentWater.maxRefractionDistance);

            cb._OutScatteringCoefficient = -Mathf.Log(0.02f) / currentWater.maxAbsorptionDistance;
            cb._TransparencyColor = new Vector3(Mathf.Min(currentWater.refractionColor.r, 0.99f), Mathf.Min(currentWater.refractionColor.g, 0.99f), Mathf.Min(currentWater.refractionColor.b, 0.99f));

            cb._HeightBasedScattering = currentWater.heightScattering;
            cb._DisplacementScattering = currentWater.displacementScattering * 0.25f;

            float scatteringLambertLightingNear = 0.6f;
            float scatteringLambertLightingFar = 0.06f;
            cb._ScatteringLambertLighting = new Vector4(scatteringLambertLightingNear, scatteringLambertLightingFar, Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingNear), Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingFar));

            // Defines the amount of foam based on the wind speed.
            cb._FoamJacobianLambda = new Vector4(cb._BandPatchSize.x, cb._BandPatchSize.y, cb._BandPatchSize.z, cb._BandPatchSize.w);

            cb._CausticsRegionSize = cb._BandPatchSize[currentWater.causticsBand];
        }

        void UpdateWaterSurface(CommandBuffer cmd, WaterSurface currentWater, int surfaceIndex)
        {
            // If the function returns false, this means the resources were just created and they need to be initialized.
            bool validResources = currentWater.CheckResources(cmd, (int)m_WaterBandResolution, k_WaterHighBandCount);

            // Update the simulation time
            currentWater.simulation.Update(Time.realtimeSinceStartup, currentWater.timeMultiplier);

            // Update the constant buffer
            UpdateShaderVariablesWater(currentWater, surfaceIndex, ref m_ShaderVariablesWater);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_WaterSimulationCS, HDShaderIDs._ShaderVariablesWater);

            // Evaluate the band count
            int bandCount = currentWater.highBandCount ? k_WaterHighBandCount : k_WaterLowBandCount;

            // Number of tiles we will need to dispatch
            int tileCount = (int)m_WaterBandResolution / 8;

            // Do we need to re-evaluate the Phillips spectrum?
            if (!validResources)
            {
                // Convert the noise to the Phillips spectrum
                cmd.SetComputeTextureParam(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, HDShaderIDs._H0BufferRW, currentWater.simulation.phillipsSpectrumBuffer);
                cmd.DispatchCompute(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, tileCount, tileCount, bandCount);
            }

            // Execute the dispersion
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._H0Buffer, currentWater.simulation.phillipsSpectrumBuffer);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtRealBufferRW, m_HtRs);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtImaginaryBufferRW, m_HtIs);
            cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateDispersionKernel, tileCount, tileCount, bandCount);

            // Make sure to define properly if this is the initial frame
            m_ShaderVariablesWater._WaterInitialFrame = !validResources ? 1 : 0;

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_FourierTransformCS, HDShaderIDs._ShaderVariablesWater);

            // First pass of the FFT
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_HtRs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_HtIs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, m_FFTRowPassRs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBufferRW, m_FFTRowPassIs);
            cmd.DispatchCompute(m_FourierTransformCS, m_RowPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

            // Second pass of the FFT
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_FFTRowPassRs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_FFTRowPassIs);
            cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, currentWater.simulation.displacementBuffer);
            cmd.DispatchCompute(m_FourierTransformCS, m_ColPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

            // Evaluate water surface additional data (combining it with the previous values)
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, HDShaderIDs._WaterDisplacementBuffer, currentWater.simulation.displacementBuffer);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, HDShaderIDs._PreviousWaterAdditionalDataBuffer, currentWater.simulation.additionalDataBuffer);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, HDShaderIDs._WaterAdditionalDataBufferRW, m_AdditionalData);
            cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateNormalsFoamKernel, tileCount, tileCount, bandCount);

            // Copy the result back into the water surface's texture
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_CopyAdditionalDataKernel, HDShaderIDs._WaterAdditionalDataBuffer, m_AdditionalData);
            cmd.SetComputeTextureParam(m_WaterSimulationCS, m_CopyAdditionalDataKernel, HDShaderIDs._WaterAdditionalDataBufferRW, currentWater.simulation.additionalDataBuffer);
            cmd.DispatchCompute(m_WaterSimulationCS, m_CopyAdditionalDataKernel, tileCount, tileCount, bandCount);

            // Make sure the mip-maps are generated
            currentWater.simulation.additionalDataBuffer.rt.GenerateMips();
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
            CoreUtils.SetRenderTarget(cmd, currentWater.simulation.causticsBuffer, clearFlag: ClearFlag.Color, Color.black);
            m_WaterMaterialPropertyBlock.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, currentWater.simulation.additionalDataBuffer);
            m_WaterMaterialPropertyBlock.SetFloat(HDShaderIDs._CausticsVirtualPlane, currentWater.virtualPlaneDistance);
            m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticsNormalsMipOffset, EvaluateNormalMipOffset(m_WaterBandResolution));
            m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticGeometryResolution, k_WaterCausticsMesh);
            m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticsBandIndex, currentWater.causticsBand);
            cmd.DrawProcedural(m_CausticsGeometry, Matrix4x4.identity, m_CausticsMaterial, 0, MeshTopology.Triangles, k_WaterCausticsMesh * k_WaterCausticsMesh * 6, 1, m_WaterMaterialPropertyBlock);

            // Make sure the mip-maps are generated
            currentWater.simulation.causticsBuffer.rt.GenerateMips();
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
            public int gridResolution;
            public int numLODs;
            public float gridSize;
            public bool highBandCount;
            public bool infinite;
            public Vector3 center;
            public Vector2 extent;
            public Mesh targetMesh;
            public float rotation;
            public Vector2 foamMaskOffset;
            public Vector2 waterMaskOffset;

            // Caustics parameters
            public bool causticsEnabled;
            public bool simulationCaustics;

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
        }

        WaterRenderingGBufferParameters PrepareWaterRenderingGBufferParameters(HDCamera hdCamera, WaterRendering settings, WaterSurface currentWater, int surfaceIndex)
        {
            WaterRenderingGBufferParameters parameters = new WaterRenderingGBufferParameters();

            // Camera parameters
            parameters.cameraPosition = hdCamera.camera.transform.position;
            parameters.cameraFrustum = hdCamera.frustum;
            parameters.cameraFarPlane = hdCamera.camera.farClipPlane;

            // Geometry parameters
            parameters.gridResolution = (int)settings.gridResolution.value;
            parameters.numLODs = settings.numLevelOfDetails.value;
            parameters.gridSize = Mathf.Min(settings.gridSize.value, hdCamera.camera.farClipPlane / offsets[settings.numLevelOfDetails.value]);
            parameters.highBandCount = currentWater.highBandCount;
            parameters.infinite = currentWater.infinite;
            parameters.center = currentWater.transform.position;
            parameters.extent = new Vector2(currentWater.transform.lossyScale.x, currentWater.transform.lossyScale.z);
            parameters.targetMesh = currentWater.geometryType == WaterSurface.WaterGeometryType.Custom ? currentWater.geometry : null;
            parameters.rotation = -currentWater.transform.eulerAngles.y * Mathf.Deg2Rad;
            parameters.foamMaskOffset = currentWater.foamMaskOffset;
            parameters.waterMaskOffset = currentWater.waterMaskOffset;

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
            parameters.waterRenderingCB._WaterMaskScale.Set(1.0f / currentWater.waterMaskExtent.x, 1.0f / currentWater.waterMaskExtent.y);
            parameters.waterRenderingCB._FoamMaskScale.Set(1.0f / currentWater.foamMaskExtent.x, 1.0f / currentWater.foamMaskExtent.y);
            parameters.waterRenderingCB._WaterAmbientProbe = EvaluateWaterAmbientProbe(hdCamera, settings.ambientProbeDimmer.value);
            parameters.waterRenderingCB._CausticsIntensity = currentWater.causticsIntensity;
            parameters.waterRenderingCB._CausticsTiling = currentWater.causticsTiling;
            parameters.waterRenderingCB._CausticsPlaneOffset = currentWater.causticsPlaneOffset;
            parameters.waterRenderingCB._CausticsPlaneBlendDistance = currentWater.causticsPlaneBlendDistance;
            parameters.waterRenderingCB._EarthRadius = currentWater.infinite ? currentWater.earthRadius : 6371000.0f;
            parameters.waterRenderingCB._InfiniteSurface = currentWater.infinite ? 1 : 0;

            // Compute the caustics offsets
            float causticsOffset = currentWater.simulation.simulationTime * currentWater.causticsSpeed * k_KilometerPerHourToMeterPerSecond;
            Vector2 causticOrientation = OrientationToDirection(currentWater.windOrientation);
            parameters.waterRenderingCB._CausticsOffset.Set(causticsOffset * causticOrientation.x, causticsOffset * causticOrientation.y);

            // Bind the decal layer data
            parameters.waterRenderingCB._WaterDecalLayer = ((uint)currentWater.decalLayerMask);

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
            public TextureHandle displacementBuffer;
            public TextureHandle additionalData;
            public TextureHandle causticsData;

            // Water rendered to this buffer
            public TextureHandle colorPyramid;
            public TextureHandle depthBuffer;

            // Output buffers
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
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
            public TextureHandle gbuffer2;

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
            profile.maxRefractionDistance = Mathf.Min(waterSurface.maxAbsorptionDistance, waterSurface.maxRefractionDistance);
            profile.lightLayers = (uint)waterSurface.lightLayerMask;
            profile.transparencyColor = new Vector3(Mathf.Min(waterSurface.refractionColor.r, 0.99f),
                                                    Mathf.Min(waterSurface.refractionColor.g, 0.99f),
                                                    Mathf.Min(waterSurface.refractionColor.b, 0.99f));
            profile.outScatteringCoefficient = -Mathf.Log(0.02f) / waterSurface.maxAbsorptionDistance;
            m_WaterSurfaceProfileArray[waterSurfaceIndex] = profile;
        }

        void RenderWaterSurfaceGBuffer(RenderGraph renderGraph, HDCamera hdCamera,
                                        WaterSurface currentWater, WaterRendering settings, int surfaceIdx,
                                        TextureHandle depthBuffer,
                                        TextureHandle WaterGbuffer0, TextureHandle WaterGbuffer1, TextureHandle WaterGbuffer2)
        {
            using (var builder = renderGraph.AddRenderPass<WaterRenderingGBufferData>("Render Water Surface GBuffer", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingGBuffer)))
            {
                // Prepare all the internal parameters
                passData.parameters = PrepareWaterRenderingGBufferParameters(hdCamera, settings, currentWater, surfaceIdx);

                // Allocate all the intermediate textures
                passData.gbuffer0 = builder.UseColorBuffer(WaterGbuffer0, 0);
                passData.gbuffer1 = builder.UseColorBuffer(WaterGbuffer1, 1);
                passData.gbuffer2 = builder.UseColorBuffer(WaterGbuffer2, 2);

                // Import all the textures into the system
                passData.displacementBuffer = renderGraph.ImportTexture(currentWater.simulation.displacementBuffer);
                passData.additionalData = renderGraph.ImportTexture(currentWater.simulation.additionalDataBuffer);
                passData.causticsData = passData.parameters.simulationCaustics ? renderGraph.ImportTexture(currentWater.simulation.causticsBuffer) : renderGraph.defaultResources.blackTexture;

                // Request the output textures
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                    (WaterRenderingGBufferData data, RenderGraphContext ctx) =>
                    {
                        // Raise the right stencil flags
                        ctx.cmd.SetGlobalFloat("_StencilWaterRefGBuffer", (int)(StencilUsage.WaterSurface | StencilUsage.TraceReflectionRay));
                        ctx.cmd.SetGlobalFloat("_StencilWaterWriteMaskGBuffer", (int)(StencilUsage.WaterSurface | StencilUsage.TraceReflectionRay));

                        // Prepare the material property block for the rendering
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterDisplacementBuffer, data.displacementBuffer);
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, data.additionalData);
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterCausticsDataBuffer, data.causticsData);
                        data.parameters.waterRenderingCB._WaterCausticsType = data.parameters.causticsEnabled ? (data.parameters.simulationCaustics ? 0 : 1) : 0;

                        // Bind the global water textures
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterMask, data.parameters.waterMask);
                        data.parameters.mbp.SetTexture(HDShaderIDs._FoamTexture, data.parameters.surfaceFoam);
                        data.parameters.mbp.SetTexture(HDShaderIDs._FoamMask, data.parameters.foamMask);

                        // Raise the keyword if it should be raised
                        CoreUtils.SetKeyword(ctx.cmd, "HIGH_RESOLUTION_WATER", data.parameters.highBandCount);
                        CoreUtils.SetKeyword(ctx.cmd, "WATER_PROCEDURAL_GEOMETRY", data.parameters.infinite || data.parameters.targetMesh == null);

                        // Bind the water constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWater);

                        // Bind the render targets
                        var mrtArray = ctx.renderGraphPool.GetTempArray<RenderTargetIdentifier>(3);
                        mrtArray[0] = data.gbuffer0;
                        mrtArray[1] = data.gbuffer1;
                        mrtArray[2] = data.gbuffer2;
                        CoreUtils.SetRenderTarget(ctx.cmd, mrtArray, data.depthBuffer);
                        data.parameters.waterRenderingCB._WaterMaskOffset.Set(data.parameters.waterMaskOffset.x, -data.parameters.waterMaskOffset.y);
                        data.parameters.waterRenderingCB._FoamMaskOffset.Set(data.parameters.foamMaskOffset.x, -data.parameters.foamMaskOffset.y);

                        if (data.parameters.infinite)
                        {
                            // Need to inject at water and foam mask rotations and offsets
                            data.parameters.waterRenderingCB._FoamMaskOffset.Set(data.parameters.foamMaskOffset.x, data.parameters.foamMaskOffset.y);
                            data.parameters.waterRenderingCB._WaterRotation.Set(1.0f, 0.0f);

                            // Loop through the patches
                            for (int y = -data.parameters.numLODs; y <= data.parameters.numLODs; ++y)
                            {
                                for (int x = -data.parameters.numLODs; x <= data.parameters.numLODs; ++x)
                                {
                                    // Compute the bounds of the patch
                                    Vector3 center;
                                    Vector2 size;
                                    ComputeGridBounds(x, y, data.parameters.numLODs, data.parameters.gridSize, data.parameters.center, data.parameters.cameraFarPlane, out center, out size);

                                    // Account for the maximal horizontal displacement
                                    size.x = size.x + data.parameters.waterCB._MaxWaveDisplacement * 2;
                                    size.y = size.y + data.parameters.waterCB._MaxWaveDisplacement * 2;

                                    // Build the OOBB
                                    var obb = new OrientedBBox(Matrix4x4.TRS(center + new Vector3(data.parameters.cameraPosition.x, 0, data.parameters.cameraPosition.z), Quaternion.identity, new Vector3(size.x, k_WaterAmplitudeNormalization * 2.0f, size.y)));
                                    obb.center -= data.parameters.cameraPosition;

                                    // is this patch visible by the camera?
                                    if (GeometryUtils.Overlap(obb, data.parameters.cameraFrustum, 6, 8))
                                    {
                                        // Offset position of the patch
                                        data.parameters.waterRenderingCB._PatchOffset = center + new Vector3(data.parameters.cameraPosition.x, 0, data.parameters.cameraPosition.z);

                                        // Size of the patch
                                        data.parameters.waterRenderingCB._GridSize = size;

                                        // Resolution of the patch
                                        int pachResolution = GetPatchResolution(x, y, data.parameters.gridResolution);
                                        data.parameters.waterRenderingCB._GridRenderingResolution = (uint)pachResolution;

                                        // Evaluate the tessellation mask (used to adapt to neighboring patches)
                                        int tesselationMasks = EvaluateTesselationMask(x, y, data.parameters.gridResolution);
                                        data.parameters.waterRenderingCB._TesselationMasks = (uint)tesselationMasks;

                                        // Push the draw-depending constant buffer
                                        ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);

                                        // Draw the target patch
                                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.waterMaterial, k_WaterGBufferProcedural, MeshTopology.Triangles, 6 * pachResolution * pachResolution, 1, data.parameters.mbp);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Set the masks offsets and rotations
                            data.parameters.waterRenderingCB._WaterRotation.Set(Mathf.Cos(data.parameters.rotation), Mathf.Sin(data.parameters.rotation));

                            // Patch offset and grid size
                            data.parameters.waterRenderingCB._GridSize.Set(data.parameters.extent.x, data.parameters.extent.y);
                            data.parameters.waterRenderingCB._PatchOffset = data.parameters.center;
                            data.parameters.waterRenderingCB._GridRenderingResolution = (uint)data.parameters.gridResolution;

                            // Push the draw-depending constant buffer
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);

                            if (data.parameters.targetMesh == null)
                                ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.waterMaterial, k_WaterGBufferProcedural, MeshTopology.Triangles, 6 * data.parameters.gridResolution * data.parameters.gridResolution, 1, data.parameters.mbp);
                            else
                            {
                                int numSubMeshes = data.parameters.targetMesh.subMeshCount;
                                for (int subMeshIdx = 0; subMeshIdx < numSubMeshes; ++subMeshIdx)
                                    ctx.cmd.DrawMesh(data.parameters.targetMesh, Matrix4x4.identity, data.parameters.waterMaterial, subMeshIdx, k_WaterGBufferMesh, data.parameters.mbp);
                            }
                        }
                    });
            }
        }

        struct WaterGBuffer
        {
            public TextureHandle waterGBuffer0;
            public TextureHandle waterGBuffer1;
            public TextureHandle waterGBuffer2;
        }

        WaterGBuffer RenderWaterGBuffer(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle colorPyramid)
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
                return outputGBuffer;
            }

            // Request all the gbuffer textures we will need
            TextureHandle WaterGbuffer0 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R16G16_UInt, enableRandomWrite = true, name = "Water GBuffer 0" });
            TextureHandle WaterGbuffer1 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R16G16B16A16_UInt, enableRandomWrite = true, name = "Water GBuffer 1" });
            TextureHandle WaterGbuffer2 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R16G16_UInt, enableRandomWrite = true, name = "Water GBuffer 2" });

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
                RenderWaterSurfaceGBuffer(renderGraph, hdCamera, currentWater, settings, surfaceIdx,
                                depthBuffer, WaterGbuffer0, WaterGbuffer1, WaterGbuffer2);

            }

            using (var builder = renderGraph.AddRenderPass<WaterRenderingSSRData>("Prepare water for SSR", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingSSR)))
            {
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualWidth;
                passData.viewCount = hdCamera.viewCount;
                passData.waterLighting = m_WaterLightingCS;
                passData.prepareSSRKernel = m_WaterPrepareSSRKernel;
                passData.gbuffer1 = builder.ReadTexture(WaterGbuffer1);
                passData.gbuffer2 = builder.ReadTexture(WaterGbuffer2);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.WriteTexture(normalBuffer);

                builder.SetRenderFunc(
                    (WaterRenderingSSRData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int tileX = (data.width + 7) / 8;
                        int tileY = (data.height + 7) / 8;

                        // Bind the input gbuffer data
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterGBufferTexture1, data.gbuffer1);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._WaterGBufferTexture2, data.gbuffer2);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLighting, data.prepareSSRKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);

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
                colorFormat = GraphicsFormat.R16G16B16A16_UInt,
                bindTextureMS = msaa,
                msaaSamples = hdCamera.msaaSamples,
                clearColor = Color.clear,
                name = msaa ? "WaterGBuffer0MSAA" : "WaterGBuffer0"
            });

            TextureHandle WaterGbuffer2 = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R16G16B16A16_UInt,
                bindTextureMS = msaa,
                msaaSamples = hdCamera.msaaSamples,
                clearColor = Color.clear,
                name = msaa ? "WaterGBuffer2MSAA" : "WaterGBuffer2"
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
                RenderWaterSurfaceGBuffer(renderGraph, hdCamera, currentWater, settings, surfaceIdx, depthBuffer, WaterGbuffer0, colorBuffer, WaterGbuffer2);
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
            public TextureHandle depthBuffer;
            public ComputeBufferHandle waterSurfaceProfiles;
            public TextureHandle scatteringFallbackTexture;

            // Water rendered to this buffer
            public TextureHandle colorBuffer;
        }

        void RenderWaterLighting(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle colorPyramid, WaterGBuffer waterGBuffer)
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
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.waterSurfaceProfiles = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterProfileArrayGPU));
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

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
                        ctx.cmd.SetComputeBufferParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.parameters.waterLighting, data.parameters.waterLightingKernel, HDShaderIDs._CameraColorTextureRW, data.colorBuffer);

                        // Run the lighting
                        ctx.cmd.DispatchCompute(data.parameters.waterLighting, data.parameters.waterLightingKernel, tileX, tileY, data.parameters.viewCount);
                    });
            }
        }
    }
}

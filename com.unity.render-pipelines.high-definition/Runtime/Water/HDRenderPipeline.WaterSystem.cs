using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // Enum that defines the sets of resolution at which the water simulation can be evaluated
    public enum WaterSimulationResolution
    {
        Low64 = 64,
        Medium128 = 128,
        High256 = 256,
        Ultra512 = 512
    }

    internal class WaterSiumulationResources
    {
        // Overall time that has passed since Unity has been initialized
        private float m_Time = 0;

        // Current simulation time (used to compute the dispersion of the Phillips spectrum)
        public float simulationTime = 0;

        // Delta time of the current frame
        public float deltaTime = 0;

        // Texture that holds the Phillips spectrum
        public RTHandle phillipsSpectrumBuffer = null;

        // Texture that holds the displacement buffers
        public RTHandle displacementBuffer = null;

        // Texture that holds the additional data buffers (normal + foam)
        public RTHandle additionalDataBuffer = null;

        // Resolution at which the water system is ran
        public int simulationResolution = 0;

        // The number bands that we will be running the simulation at
        public int numBands = 0;

        // The wind speed, orientation and weight used to evaluate the Phillips spectrum
        public float windSpeed = 0;
        public float windOrientation = 0;
        public float windAffectCurrent = 0;

        // Value that defines the patch sizes of the bands (up to 4)
        public Vector4 patchSizes = Vector4.zero;

        // Value that defines the wind speed that is applied to each patch (up to 4)
        public Vector4 patchWindSpeed = Vector4.zero;

        // Function that allocates the resources and keep track of the resolution and number of bands
        public void AllocateSmmulationResources(int simulationRes, int nbBands)
        {
            // Keep track of the values that constraint the texture allocation.
            simulationResolution = simulationRes;
            numBands = nbBands;

            // Allocate the buffers
            phillipsSpectrumBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            displacementBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            additionalDataBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
        }

        // Function that validates the resources (size and if allocated)
        public bool ValidResources(int simulationRes, int nbBands)
        {
            return (simulationRes == simulationResolution)
                && (nbBands == numBands)
                && AllocatedTextures();
        }

        // Function that makes sure that all the textures are allocated
        public bool AllocatedTextures()
        {
            return (phillipsSpectrumBuffer != null)
                && (displacementBuffer != null)
                && (additionalDataBuffer != null);
        }

        // Function that computes the delta time for the frame
        public void Update(float totalTime, float timeMultiplier)
        {
            deltaTime = (totalTime - m_Time) * timeMultiplier;
            simulationTime += deltaTime;
            m_Time = totalTime;
        }

        // Function that releases the resources and resets all the internal variables
        public void ReleaseSmmulationResources()
        {
            // Release the textures
            RTHandles.Release(additionalDataBuffer);
            additionalDataBuffer = null;
            RTHandles.Release(displacementBuffer);
            displacementBuffer = null;
            RTHandles.Release(phillipsSpectrumBuffer);
            phillipsSpectrumBuffer = null;

            // Reset the resolution data
            simulationResolution = 0;
            numBands = 0;

            // Reset the simulation time
            m_Time = 0;
            simulationTime = 0;
            deltaTime = 0;

            // Reset the simulation parameters
            windSpeed = 0;
            windOrientation = 0;
            windAffectCurrent = 0;
            patchSizes = Vector4.zero;
        }
    }

    public partial class HDRenderPipeline
    {
        // Number of bands when the water is rendered at high band count
        const int k_WaterHighBandCount = 4;
        // Number of bands when the water is rendered at low band count
        const int k_WaterLowBandCount = 2;
        // Minimal size that a patch can reach (in meters)
        const float k_MinPatchSize = 10.0f;
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

        // Simulation shader and kernels
        ComputeShader m_WaterSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsKernel;
        int m_BlurFoamAndCombineKernel;

        // FFT shader and kernels
        ComputeShader m_FourierTransformCS;
        int m_RowPassTi_Kernel;
        int m_ColPassTi_Kernel;

        // Intermediate RTHandles used to render the water
        RTHandle m_HtRs = null;
        RTHandle m_HtIs = null;
        RTHandle m_FFTRowPassRs;
        RTHandle m_FFTRowPassIs;
        RTHandle m_SurfaceGradient;
        RTHandle m_FoamBuffer;

        // Other internal rendering data
        bool m_ActiveWaterSimulation = false;
        Material m_InternalWaterMaterial;
        MaterialPropertyBlock m_WaterMaterialPropertyBlock;
        ShaderVariablesWater m_ShaderVariablesWater = new ShaderVariablesWater();
        WaterSimulationResolution m_WaterBandResolution = WaterSimulationResolution.Medium128;

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
            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportWater)
                return;

            m_ActiveWaterSimulation = true;
            m_WaterBandResolution = m_Asset.currentPlatformRenderPipelineSettings.waterSimulationResolution;

            // Simulation shader and kernels
            m_WaterSimulationCS = m_Asset.renderPipelineResources.shaders.waterSimulationCS;
            m_InitializePhillipsSpectrumKernel = m_WaterSimulationCS.FindKernel("InitializePhillipsSpectrum");
            m_EvaluateDispersionKernel = m_WaterSimulationCS.FindKernel("EvaluateDispersion");
            m_EvaluateNormalsKernel = m_WaterSimulationCS.FindKernel("EvaluateNormals");
            m_BlurFoamAndCombineKernel = m_WaterSimulationCS.FindKernel("BlurFoamAndCombine");

            // FFT shader and kernels
            m_FourierTransformCS = m_Asset.renderPipelineResources.shaders.fourierTransformCS;
            GetFFTKernels(m_WaterBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

            // Allocate all the RTHanles required for the water rendering
            int textureRes = (int)m_WaterBandResolution;
            m_HtRs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtIs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassRs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassIs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_SurfaceGradient = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FoamBuffer = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);

            // Allocate the additional rendering data
            m_WaterMaterialPropertyBlock = new MaterialPropertyBlock();
            m_InternalWaterMaterial = defaultResources.materials.defaultWaterMaterial;
        }

        void ReleaseWaterSystem()
        {
            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportWater)
                return;
            // Grab all the water surfaces in the scene
            var waterSurfaces = GameObject.FindObjectsOfType<WaterSurface>();

            // Loop through them and display them
            int numWaterSurfaces = waterSurfaces.Length;
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                WaterSurface waterSurface = waterSurfaces[surfaceIdx];
                if (waterSurface.simulation != null)
                    waterSurface.simulation.ReleaseSmmulationResources();
            }

            // Release all the RTHandles
            RTHandles.Release(m_FoamBuffer);
            RTHandles.Release(m_SurfaceGradient);
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
        }

        Vector2 OrientationToDirection(float orientation)
        {
            float orientationRad = orientation * Mathf.Deg2Rad;
            float directionX = Mathf.Cos(orientationRad);
            float directionY = Mathf.Sin(orientationRad);
            return new Vector2(directionX, directionY);
        }

        // Function that guesses the maximal wave height from the wind speed
        static public float MaximumWaveHeightFunction(float windSpeed)
        {
            return 1.0f - Mathf.Exp(-k_PhillipsWindFalloffCoefficient * windSpeed * windSpeed);
        }

        // Function that loops thought all the current waves and computes the maximal wave height
        public float ComputeMaximumWaveHeight(Vector4 normalizedWaveAmplitude, float waterWindSpeed, int numBands)
        {
            float maxiumumWaveHeight = 0.01f;
            for (int i = 0; i < numBands; ++i)
            {
                float A = k_WaterAmplitudeNormalization * normalizedWaveAmplitude[i];
                maxiumumWaveHeight = Mathf.Max(A * MaximumWaveHeightFunction(waterWindSpeed), maxiumumWaveHeight);
            }
            return maxiumumWaveHeight;
        }

        static public float MaximumWindForPatch(float patchSize)
        {
            float a = Mathf.Sqrt(-1.0f / Mathf.Log(0.999f * 0.999f));
            float b = (0.001f * Mathf.PI * 2.0f) / patchSize;
            float c = k_PhillipsWindScalar * Mathf.Sqrt((1.0f / k_PhillipsGravityConstant) * (a / b));
            return c;
        }

        static float EvaluatePolynomial(float x, float k_4, float k_3, float k_2, float k_1, float k_0)
        {
            float x2 = x * x;
            float x3 = x2 * x;
            float x4 = x2 * x2;
            return x4 * k_4 + x3 * k_3 + x2 * k_2 + x * k_1 + k_0;
        }

        static internal Vector4 ComputeBandPatchSizes(float maxPatchSize)
        {
            float range = maxPatchSize - k_MinPatchSize;
            float b0 = maxPatchSize;
            float b1 = maxPatchSize - 7.0f / 8.0f * range;
            float b2 = maxPatchSize - 31.0f / 32.0f * range;
            float b3 = maxPatchSize - 63.0f / 64.0f * range;
            return new Vector4(b0, b1, b2, b3);
        }

        static internal Vector4 ComputeWindSpeeds(float windSpeed, Vector4 patchSizes)
        {
            float normalizedWindSpeed = Mathf.Sqrt(windSpeed / 100.0f);
            float b0 = MaximumWindForPatch(patchSizes.x) * normalizedWindSpeed;
            float b1 = MaximumWindForPatch(patchSizes.y) * normalizedWindSpeed;
            float b2 = MaximumWindForPatch(patchSizes.z) * normalizedWindSpeed;
            float b3 = MaximumWindForPatch(patchSizes.w) * normalizedWindSpeed;
            return new Vector4(b0, b1, b2, b3);
        }

        void UpdateShaderVariablesWater(WaterSurface currentWater, ref ShaderVariablesWater cb)
        {
            // Resolution at which the simulation is evaluated
            cb._BandResolution = (uint)m_WaterBandResolution;

            // Maximal possible wave height of the current setup
            cb._MaxWaveHeight = ComputeMaximumWaveHeight(currentWater.amplitude, currentWater.simulation.patchWindSpeed.x, currentWater.highBandCount ? k_WaterHighBandCount : k_WaterLowBandCount);

            // Current simulation time
            cb._SimulationTime = currentWater.simulation.simulationTime;

            // Controls how much the wind affect the current of the waves
            cb._DirectionDampener = 1.0f - currentWater.windAffectCurrent;

            // Combine the wave amplitude with the maximal wave height we can reach
            cb._WaveAmplitude = currentWater.amplitude * cb._MaxWaveHeight;

            // Choppiness factor
            cb._Choppiness = currentWater.choppiness;

            // Water smoothness
            cb._WaterSmoothness = currentWater.waterSmoothness;

            // Compute the size of the patches and their scale
            cb._BandPatchSize = currentWater.simulation.patchSizes;
            cb._BandPatchUVScale = new Vector4(1.0f, currentWater.simulation.patchSizes.x / currentWater.simulation.patchSizes.y,
                                                    currentWater.simulation.patchSizes.x / currentWater.simulation.patchSizes.z,
                                                    currentWater.simulation.patchSizes.x / currentWater.simulation.patchSizes.w);

            // Wind direction
            cb._WindDirection = OrientationToDirection(currentWater.windOrientation);

            // Manually set wind by the user
            cb._WindSpeed = currentWater.simulation.patchWindSpeed;

            // Foam data
            cb._FoamSmoothness = currentWater.surfaceFoamSmoothness;
            cb._SurfaceFoamIntensity = currentWater.surfaceFoamIntensity;
            cb._SurfaceFoamAmount = currentWater.surfaceFoamAmount;
            cb._FoamTilling = currentWater.surfaceFoamTiling * 0.025f;
            cb._DeepFoamAmount = 0.05f * currentWater.deepFoam;
            cb._DeepFoamColor = new Vector3(currentWater.deepFoamColor.r, currentWater.deepFoamColor.g, currentWater.deepFoamColor.b);
            float foamSpeed = currentWater.simulation.simulationTime * Mathf.Sqrt(cb._WindSpeed.x * k_PhillipsGravityConstant) * currentWater.windAffectCurrent;
            cb._FoamOffsets = new Vector2(cb._WindDirection.x * foamSpeed * 0.5f, cb._WindDirection.y * foamSpeed * 0.5f);

            cb._SSSMaskCoefficient = 1000.0f;

            cb._ScatteringColorTips = new Vector3(currentWater.scatteringColor.r, currentWater.scatteringColor.g, currentWater.scatteringColor.b);
            cb._BodyScatteringWeight = currentWater.directLightBodyScattering;
            cb._TipScatteringWeight = currentWater.directLightTipScattering;
            cb._DeltaTime = currentWater.simulation.deltaTime;

            cb._Refraction = 0.5f;
            cb._RefractionLow = 2.0f;
            cb._MaxAbsorptionDistance = currentWater.maxAbsorptionDistance;
            cb._MaxRefractionDistance = Mathf.Min(currentWater.maxAbsorptionDistance, currentWater.maxRefractionDistance);

            cb._OutScatteringCoefficient = -Mathf.Log(0.02f) / currentWater.maxAbsorptionDistance;
            cb._TransparencyColor = new Vector3(Mathf.Min(currentWater.transparentColor.r, 0.99f), Mathf.Min(currentWater.transparentColor.g, 0.99f), Mathf.Min(currentWater.transparentColor.b, 0.99f));

            cb._ScatteringIntensity = currentWater.scatteringFactor * 0.5f;
            cb._HeightBasedScattering = currentWater.heightScattering;
            cb._DisplacementScattering = currentWater.displacementScattering;

            cb._CloudTexturedAmount = 1.0f;

            float scatteringLambertLightingNear = 0.6f;
            float scatteringLambertLightingFar = 0.06f;
            cb._ScatteringLambertLighting = new Vector4(scatteringLambertLightingNear, scatteringLambertLightingFar, Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingNear), Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingFar));

            // Defines the amount of foam based on the wind speed.
            cb._WindFoamAttenuation = Mathf.Clamp(currentWater.windFoamCurve.Evaluate(currentWater.windSpeed / 100.0f), 0.0f, 1.0f);
            cb._FoamJacobianLambda = new Vector4(cb._BandPatchSize.x, cb._BandPatchSize.y * 16.0f, 0.0f, 0.0f);
        }

        void UpdateWaterSurfaces(CommandBuffer cmd)
        {
            // If water surface simulation is disabled, skip.
            if (!m_ActiveWaterSimulation)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceSimulation)))
            {
                // Number of tiles we will need to dispatch
                int tileCount = (int)m_WaterBandResolution / 8;

                // Bind the noise textures
                GetBlueNoiseManager().BindDitheredRNGData1SPP(cmd);

                // Grab all the water surfaces in the scene
                var waterSurfaces = GameObject.FindObjectsOfType<WaterSurface>();

                // Loop through them and update them
                int numWaterSurfaces = waterSurfaces.Length;
                for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
                {
                    // Grab the current water surface
                    WaterSurface currentWater = waterSurfaces[surfaceIdx];

                    // If the function returns false, this means the resources were just created and they need to be initialized.
                    bool initialAllocation = false;
                    bool validResources = currentWater.CheckResources(cmd, (int)m_WaterBandResolution, k_WaterHighBandCount, ref initialAllocation);

                    // Update the simulation time
                    currentWater.simulation.Update(Time.realtimeSinceStartup, currentWater.timeMultiplier);

                    // Update the constant buffer
                    UpdateShaderVariablesWater(currentWater, ref m_ShaderVariablesWater);

                    // Bind the constant buffer
                    ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_WaterSimulationCS, HDShaderIDs._ShaderVariablesWater);

                    // Evaluate the band count
                    int bandCount = currentWater.highBandCount ? k_WaterHighBandCount : k_WaterLowBandCount;

                    if (!validResources)
                    {
                        // If the texture has just been allocated, we clear the texture to black to avoid using nans.
                        if (initialAllocation)
                            CoreUtils.SetRenderTarget(cmd, currentWater.simulation.additionalDataBuffer, clearFlag: ClearFlag.Color, Color.black);

                        // Convert the noise to the Phillips spectrum
                        cmd.SetComputeTextureParam(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, HDShaderIDs._H0BufferRW, currentWater.simulation.phillipsSpectrumBuffer);
                        cmd.DispatchCompute(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, tileCount, tileCount, bandCount);
                    }

                    // Execute the dispersion
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._H0Buffer, currentWater.simulation.phillipsSpectrumBuffer);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtRealBufferRW, m_HtRs);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtImaginaryBufferRW, m_HtIs);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateDispersionKernel, tileCount, tileCount, bandCount);

                    // Bind the constant buffer
                    ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_FourierTransformCS, HDShaderIDs._ShaderVariablesWater);

                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_HtRs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_HtIs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, m_FFTRowPassRs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBufferRW, m_FFTRowPassIs);
                    cmd.DispatchCompute(m_FourierTransformCS, m_RowPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_FFTRowPassRs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_FFTRowPassIs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, currentWater.simulation.displacementBuffer);
                    cmd.DispatchCompute(m_FourierTransformCS, m_ColPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsKernel, HDShaderIDs._WaterDisplacementBuffer, currentWater.simulation.displacementBuffer);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsKernel, HDShaderIDs._WaterSurfaceGradientBufferRW, m_SurfaceGradient);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsKernel, HDShaderIDs._FoamBufferRW, m_FoamBuffer);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateNormalsKernel, tileCount, tileCount, bandCount);

                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_BlurFoamAndCombineKernel, HDShaderIDs._WaterSurfaceGradientBuffer, m_SurfaceGradient);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_BlurFoamAndCombineKernel, HDShaderIDs._FoamBuffer, m_FoamBuffer);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_BlurFoamAndCombineKernel, HDShaderIDs._WaterAdditionalDataBufferRW, currentWater.simulation.additionalDataBuffer);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_BlurFoamAndCombineKernel, tileCount, tileCount, bandCount);

                    // Make sure the mip-maps are generated
                    currentWater.simulation.additionalDataBuffer.rt.GenerateMips();
                }
            }
        }

        struct WaterRenderingParameters
        {
            // Camera parameters
            public uint width;
            public uint height;
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

            // Water Mask
            public Texture2D waterMask;

            // Foam data
            public Texture2D foamMask;
            public Texture2D surfaceFoam;
            public Texture2D foamNormals;

            // Material data
            public Material waterMaterial;
            public MaterialPropertyBlock mbp;

            // Constant buffer
            public ShaderVariablesWater waterCB;
            public ShaderVariablesWaterRendering waterRenderingCB;
        }

        WaterRenderingParameters PrepareWaterRenderingParameters(HDCamera hdCamera, WaterRendering settings, WaterSurface currentWater)
        {
            WaterRenderingParameters parameters = new WaterRenderingParameters();

            parameters.gridResolution = (int)settings.gridResolution.value;
            parameters.numLODs = settings.numLevelOfDetails.value;
            parameters.cameraPosition = hdCamera.camera.transform.position;
            parameters.gridSize = Mathf.Min(settings.gridSize.value, hdCamera.camera.farClipPlane / offsets[settings.numLevelOfDetails.value]);
            parameters.cameraFrustum = hdCamera.frustum;
            parameters.highBandCount = currentWater.highBandCount;
            parameters.cameraFarPlane = hdCamera.camera.farClipPlane;

            parameters.infinite = currentWater.infinite;
            parameters.center = currentWater.transform.position;
            parameters.extent = new Vector2(currentWater.transform.localScale.x, currentWater.transform.localScale.z);
            parameters.targetMesh = currentWater.geometryType == WaterSurface.WaterGeometryType.Custom ? currentWater.geometry : null;
            parameters.rotation = -currentWater.transform.eulerAngles.y * Mathf.Deg2Rad;
            parameters.foamMaskOffset = currentWater.foamMaskOffset;
            parameters.waterMaskOffset = currentWater.waterMaskOffset;

            // All the required global textures
            parameters.waterMask = currentWater.waterMask != null ? currentWater.waterMask : Texture2D.whiteTexture;
            parameters.surfaceFoam = m_Asset.renderPipelineResources.textures.foamSurface;
            parameters.foamNormals = m_Asset.renderPipelineResources.textures.foamNormals;
            parameters.foamMask = currentWater.foamMask != null ? currentWater.foamMask : Texture2D.whiteTexture;

            // Water material
            parameters.waterMaterial = currentWater.material != null ? currentWater.material : m_InternalWaterMaterial;

            // Property bloc used for binding the textures
            parameters.mbp = m_WaterMaterialPropertyBlock;

            // Setup the simulation water constant buffer
            UpdateShaderVariablesWater(currentWater, ref parameters.waterCB);

            // Setup the water rendering constant buffers (parameters that we can setup
            parameters.waterRenderingCB._WaterMaskScale.Set(1.0f / currentWater.waterMaskExtent.x, 1.0f / currentWater.waterMaskExtent.y);
            parameters.waterRenderingCB._FoamMaskScale.Set(1.0f / currentWater.foamMaskExtent.x, 1.0f / currentWater.foamMaskExtent.y);
            parameters.waterRenderingCB._WaterAmbientProbe = EvaluateWaterAmbientProbe(hdCamera, settings.ambientProbeDimmer.value);
            parameters.waterRenderingCB._CausticsIntensity = currentWater.causticsIntensity;
            parameters.waterRenderingCB._CausticsTiling = currentWater.causticsTiling;
            parameters.waterRenderingCB._CausticsPlaneOffset = currentWater.causticsPlaneOffset;
            parameters.waterRenderingCB._EarthRadius = currentWater.infinite ? currentWater.earthRadius: 6371000.0f;

            // Compute the caustics offsets
            float causticsOffset = currentWater.simulation.simulationTime * currentWater.causticsSpeed * k_KilometerPerHourToMeterPerSecond;
            Vector2 causticOrientation = OrientationToDirection(currentWater.windOrientation);
            parameters.waterRenderingCB._CausticsOffset.Set(causticsOffset * causticOrientation.x, causticsOffset * causticOrientation.y);

            return parameters;
        }

        class WaterRenderingData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingParameters parameters;

            // Simulation buffers
            public TextureHandle displacementBuffer;
            public TextureHandle additionalData;

            // Water rendered to this buffer
            public TextureHandle colorPyramid;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        // Compute the resolution of the water patch based on it's distance to the center patch
        static int GetPatchResolution(int x, int y, int maxResolution)
        {
            return Mathf.Max((maxResolution >> (Mathf.Abs(x) + Mathf.Abs(y))), k_WaterMinGridSize);
        }

        // Evaluate the mask that allows us to adapt the tessellation at patch edges
        static int EvaluateTesselationMask(int x, int y, int maxResolution)
        {
            int center = GetPatchResolution(x, y, maxResolution);
            int up = GetPatchResolution(x, y + 1, maxResolution);
            int down = GetPatchResolution(x, y - 1, maxResolution);
            int right = GetPatchResolution(x - 1, y, maxResolution);
            int left = GetPatchResolution(x + 1, y, maxResolution);
            int mask = 0;
            mask |= (center > right) ? 0x1 : 0;
            mask |= (center > up) ? 0x2 : 0;
            mask |= (center > left) ? 0x4 : 0;
            mask |= (center > down) ? 0x8 : 0;
            return mask;
        }

        static void ComputeGridBounds(int x, int y, int numLODS, float centerGridSize, Vector3 centerGridPos, float farPlane, out Vector3 center, out Vector2 size)
        {
            int absX = Mathf.Abs(x);
            int absY = Mathf.Abs(y);
            float signX = Mathf.Sign(x);
            float signY = Mathf.Sign(y);

            // Offset position of the patch
            center = new Vector3(signX * offsets[absX] * centerGridSize, centerGridPos.y, signY * offsets[absY] * centerGridSize);

            // Size of the patch
            size = new Vector2(centerGridSize * (1 << absX), centerGridSize * (1 << absY));
        }

        unsafe Vector3 EvaluateWaterAmbientProbe(HDCamera hdCamera, float ambientProbeDimmer)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, ambientProbeDimmer);
            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsProbe, probeSH);
            return EvaluateAmbientProbe(m_PackedCoeffsProbe, Vector3.down);
        }

        void RenderWaterSurfaces(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle colorPyramid)
        {
            // If the water is disabled, no need to render or simulate
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water))
                return;

            // Grab all the water surfaces in the scene
            var waterSurfaces = GameObject.FindObjectsOfType<WaterSurface>();

            // Loop through them and display them
            int numWaterSurfaces = waterSurfaces.Length;
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // If the resources are invalid, we cannot render this surface
                if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, k_WaterHighBandCount))
                    continue;

                using (var builder = renderGraph.AddRenderPass<WaterRenderingData>("Render Water Surfaces", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRendering)))
                {
                    builder.EnableAsyncCompute(false);

                    // Prepare all the internal parameters
                    passData.parameters = PrepareWaterRenderingParameters(hdCamera, settings, currentWater);

                    // Import all the textures into the system
                    passData.displacementBuffer = renderGraph.ImportTexture(currentWater.simulation.displacementBuffer);
                    passData.additionalData = renderGraph.ImportTexture(currentWater.simulation.additionalDataBuffer);
                    passData.colorPyramid = builder.ReadTexture(colorPyramid);

                    // Request the output textures
                    passData.colorBuffer = builder.WriteTexture(colorBuffer);
                    passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                    builder.SetRenderFunc(
                        (WaterRenderingData data, RenderGraphContext ctx) =>
                        {
                            // Prepare the material property block for the rendering
                            data.parameters.mbp.SetTexture(HDShaderIDs._WaterDisplacementBuffer, data.displacementBuffer);
                            data.parameters.mbp.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, data.additionalData);

                            // Bind the color pyramid
                            data.parameters.mbp.SetTexture(HDShaderIDs._ColorPyramidTexture, data.colorPyramid);

                            // Bind the global water textures
                            data.parameters.mbp.SetTexture(HDShaderIDs._WaterMask, data.parameters.waterMask);
                            data.parameters.mbp.SetTexture(HDShaderIDs._FoamTexture, data.parameters.surfaceFoam);
                            data.parameters.mbp.SetTexture(HDShaderIDs._FoamNormal, data.parameters.foamNormals);
                            data.parameters.mbp.SetTexture(HDShaderIDs._FoamMask, data.parameters.foamMask);

                            // Raise the keyword if it should be raised
                            CoreUtils.SetKeyword(ctx.cmd, "HIGH_RESOLUTION_WATER", data.parameters.highBandCount);
                            CoreUtils.SetKeyword(ctx.cmd, "WATER_PROCEDURAL_GEOMETRY", data.parameters.infinite || data.parameters.targetMesh == null);

                            // Bind the water constant buffer
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWater);

                            // Bind the render targets
                            CoreUtils.SetRenderTarget(ctx.cmd, data.colorBuffer, data.depthBuffer);

                            if (data.parameters.infinite)
                            {
                                // Need to inject at water and foam mask rotations and offsets
                                data.parameters.waterRenderingCB._WaterMaskOffset.Set(data.parameters.waterMaskOffset.x, data.parameters.waterMaskOffset.y);
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
                                            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.waterMaterial, 0, MeshTopology.Triangles, 6 * pachResolution * pachResolution, 1, data.parameters.mbp);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Set the masks offsets and rotations
                                data.parameters.waterRenderingCB._WaterMaskOffset.Set(data.parameters.center.x + data.parameters.waterMaskOffset.x, data.parameters.center.z + data.parameters.waterMaskOffset.y);
                                data.parameters.waterRenderingCB._FoamMaskOffset.Set(data.parameters.center.x + data.parameters.foamMaskOffset.x, data.parameters.center.z + data.parameters.foamMaskOffset.y);
                                data.parameters.waterRenderingCB._WaterRotation.Set(Mathf.Cos(data.parameters.rotation), Mathf.Sin(data.parameters.rotation));

                                // Patch offset and grid size
                                data.parameters.waterRenderingCB._GridSize.Set(data.parameters.extent.x, data.parameters.extent.y);
                                data.parameters.waterRenderingCB._PatchOffset = data.parameters.center;
                                data.parameters.waterRenderingCB._GridRenderingResolution = (uint)data.parameters.gridResolution;

                                // Push the draw-depending constant buffer
                                ConstantBuffer.Push(ctx.cmd, data.parameters.waterRenderingCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterRendering);

                                if (data.parameters.targetMesh == null)
                                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.waterMaterial, 0, MeshTopology.Triangles, 6 * data.parameters.gridResolution * data.parameters.gridResolution, 1, data.parameters.mbp);
                                else
                                {
                                    int numSubMeshes = data.parameters.targetMesh.subMeshCount;
                                    for (int subMeshIdx = 0; subMeshIdx < numSubMeshes; ++subMeshIdx)
                                        ctx.cmd.DrawMesh(data.parameters.targetMesh, Matrix4x4.identity, data.parameters.waterMaterial, subMeshIdx, 1, data.parameters.mbp);
                                }
                            }
                        });
                }
            }
        }
    }
}

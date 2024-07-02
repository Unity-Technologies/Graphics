using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Enum that defines the possible modes for script interactions
    /// </summary>
    public enum WaterScriptInteractionsMode
    {
        /// <summary>
        /// Accurate results but significant CPU cost.
        /// </summary>
        GPUReadback,
        /// <summary>
        /// Results come with a few frames latency but at no extra cost.
        /// </summary>
        CPUSimulation,
    }

    /// <summary>
    /// Enum that defines the sets of resolution at which the water simulation can be evaluated
    /// </summary>
    public enum WaterSimulationResolution
    {
        /// <summary>
        /// The water simulation will be ran at a resolution of 64x64 samples per band.
        /// </summary>
        Low64 = 64,
        /// <summary>
        /// The water simulation will be ran at a resolution of 128x128 samples per band.
        /// </summary>
        Medium128 = 128,
        /// <summary>
        /// The water simulation will be ran at a resolution of 256x256 samples per band.
        /// </summary>
        High256 = 256,
    }

    internal class WaterSimulationResourcesGPU
    {
        // Texture that holds the Phillips spectrum
        public RTHandle phillipsSpectrumBuffer = null;

        // Texture that holds the displacement buffers
        public RTHandle displacementBuffer = null;

        // Texture that holds the additional data buffers (normal + foam)
        public RTHandle additionalDataBuffer = null;

        // Texture that holds the caustics
        public RTHandle causticsBuffer = null;
    }

    internal class WaterSimulationResourcesCPU
    {
        // Texture that holds the Phillips spectrum
        public NativeArray<float2> h0BufferCPU;

        // Texture that holds the displacement buffers
        public NativeArray<float4> displacementBufferCPU;
    }

    /// <summary>
    /// Structure that holds all the data that allows us to define the water spectrum.
    /// </summary>
    public struct WaterSpectrumParameters
    {
        // Value that defines the patch group
        internal int4 patchGroup;

        // Value that defines the patch sizes of the bands
        internal Vector4 patchSizes;

        // The patch orientation (in degrees) for each band
        internal Vector4 patchOrientation;

        // Value that defines the orientation (in degrees) for each group
        internal float2 groupOrientation;

        // The wind speed, orientation and weight used to evaluate the Phillips spectrum
        internal Vector4 patchWindSpeed;

        // Value that defines the wind directionality to each patch
        internal Vector4 patchWindDirDampener;

        /// <summary>
        /// Compare two WaterSpectrumParameters for equality.
        /// </summary>
        /// <param name="a">The first WaterSpectrumParameters to compare.</param>
        /// <param name="b">The second WaterSpectrumParameters to compare.</param>
        /// <returns>True if the WaterSpectrumParameters are both equal (or both null), false otherwise.</returns>
        public static bool operator ==(WaterSpectrumParameters a, WaterSpectrumParameters b)
        {
            return (a.patchSizes == b.patchSizes)
                && (a.patchWindSpeed == b.patchWindSpeed)
                && (a.patchWindDirDampener == b.patchWindDirDampener)
                && (a.patchOrientation == b.patchOrientation);
        }

        /// <summary>
        /// Compare two WaterSpectrumParameters for inequality.
        /// </summary>
        /// <param name="a">The first WaterSpectrumParameters to compare.</param>
        /// <param name="b">The second WaterSpectrumParameters to compare.</param>
        /// <returns>True if the WaterSpectrumParameters are not equal, false otherwise.</returns>
        public static bool operator !=(WaterSpectrumParameters a, WaterSpectrumParameters b)
        {
            return (a.patchSizes != b.patchSizes)
                || (a.patchWindSpeed != b.patchWindSpeed)
                || (a.patchWindDirDampener != b.patchWindDirDampener)
                || (a.patchOrientation != b.patchOrientation);
        }

        /// <summary>
        /// Get an appropriate hash value for this NPath.
        /// </summary>
        /// <returns>A hash value for this NPath.</returns>
        public override int GetHashCode() { return base.GetHashCode(); }


        /// <summary>Returns true if the WaterSpectrumParameters is equal to a given WaterSpectrumParameters, false otherwise.</summary>
        /// <param name="o">Right hand side argument to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        public override bool Equals(object o)
        {
            return o is WaterSpectrumParameters other && this == other;
        }
    }

    /// <summary>
    /// Structure that holds all the data that allows us to render the water surface from the spectrum.
    /// </summary>
    public struct WaterRenderingParameters
    {
        // System simulation time
        internal float simulationTime;

        // The per-band amplitude multiplier
        internal Vector4 patchAmplitudeMultiplier;

        // The current speed
        internal Vector4 patchCurrentSpeed;

        // The first fade factor for each band
        internal Vector4 patchFadeA;

        // The second fade factor for each band
        internal Vector4 patchFadeB;

        // Maximum distance at which displacement happens
        internal float maxFadeDistance;

        // Matrix to convert from the water space to world space
        internal float4x4 waterToWorldMatrix;

        // Matrix to convert from the world space to water space
        internal float4x4 worldToWaterMatrix;
        internal float4x4 worldToWaterMatrixCustom;
    }

    internal class WaterSimulationResources
    {
        // System simulation time, used for timeline.
        private bool active = true;

        // Overall time that has passed since Unity has been initialized
        float m_Time;
        // Current simulation time in seconds (used to compute the dispersion of the Phillips spectrum)
        public float simulationTime = 0;
        // Delta time of the current frame
        public float deltaTime = 0;

        // Resolution at which the water system is ran
        public int simulationResolution = 0;
        // The number bands that we will be running the simulation at
        public int numActiveBands = 0;
        // If we support simulation foam
        bool foam;

        // The type of the surface
        public WaterSurfaceType surfaceType;

        // The spectrum parameters
        public WaterSpectrumParameters spectrum = new WaterSpectrumParameters();
        public bool gpuSpectrumValid;
        public bool cpuSpectrumValid;

        // The rendering parameters
        public WaterRenderingParameters rendering = new WaterRenderingParameters();

        // The set of GPU Buffers used to run the simulation
        public WaterSimulationResourcesGPU gpuBuffers = null;

        // The set of CPU Buffers used to run the simulation
        public WaterSimulationResourcesCPU cpuBuffers = null;

        public void AllocateSimulationBuffersGPU()
        {
            gpuBuffers = new WaterSimulationResourcesGPU();
            gpuBuffers.phillipsSpectrumBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numActiveBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, name: "Phillips Spectrum");
            gpuBuffers.displacementBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numActiveBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, name: "Displacement");
            gpuBuffers.additionalDataBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numActiveBands, dimension: TextureDimension.Tex2DArray, colorFormat: foam ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false, name: "Normal");
        }

        public void ReleaseSimulationBuffersGPU()
        {
            if (gpuBuffers != null)
            {
                RTHandles.Release(gpuBuffers.additionalDataBuffer);
                RTHandles.Release(gpuBuffers.displacementBuffer);
                RTHandles.Release(gpuBuffers.phillipsSpectrumBuffer);
                RTHandles.Release(gpuBuffers.causticsBuffer);
                gpuBuffers = null;
            }
        }

        public void AllocateSimulationBuffersCPU()
        {
            cpuBuffers = new WaterSimulationResourcesCPU();
            cpuBuffers.h0BufferCPU = new NativeArray<float2>(simulationResolution * simulationResolution * numActiveBands, Allocator.Persistent);
            cpuBuffers.displacementBufferCPU = new NativeArray<float4>(simulationResolution * simulationResolution * numActiveBands, Allocator.Persistent);
        }

        public void ReleaseSimulationBuffersCPU()
        {
            if (cpuBuffers != null)
            {
                cpuBuffers.h0BufferCPU.Dispose();
                cpuBuffers.displacementBufferCPU.Dispose();
                cpuBuffers = null;
            }
        }

        // Function that allocates the resources and keep track of the resolution and number of bands
        public void InitializeSimulationResources(int simulationRes, int nbBands, bool simulationFoam)
        {
            // Keep track of the values that constraint the texture allocation.
            simulationResolution = simulationRes;
            numActiveBands = nbBands;
            foam = simulationFoam;
            EnableTimeSteps();
        }

        // Function that validates the resources (size and if allocated)
        public bool ValidResources(int simulationRes, int nbBands, bool simulationFoam)
        {
            return (simulationRes == simulationResolution)
            && (nbBands == numActiveBands)
            && (foam == simulationFoam)
            && (gpuBuffers != null);
        }

        public void CheckCausticsResources(bool used, int causticsResolution)
        {
            if (used)
            {
                bool needsAllocation = true;
                if (gpuBuffers.causticsBuffer != null)
                {
                    needsAllocation = gpuBuffers.causticsBuffer.rt.width != causticsResolution;
                    if (needsAllocation)
                        RTHandles.Release(gpuBuffers.causticsBuffer);
                }

                if (needsAllocation)
                    gpuBuffers.causticsBuffer = RTHandles.Alloc(causticsResolution, causticsResolution, 1, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false, name: "Caustics");
            }
            else
            {
                if (gpuBuffers.causticsBuffer != null)
                {
                    RTHandles.Release(gpuBuffers.causticsBuffer);
                    gpuBuffers.causticsBuffer = null;
                }
            }
        }

        public void EnableTimeSteps()
        {
            active = true;
            simulationTime = 0;
            m_Time = Time.realtimeSinceStartup;
        }

        public void DisableTimeSteps()
        {
            active = false;
            simulationTime = 0;
            m_Time = 0.0f;
        }

        public bool HasActiveTimeSteps()
        {
            return active;
        }

        // Function that computes the delta time for the frame
        public void Update(float timeMultiplier)
        {
            if (HasActiveTimeSteps())
            {
                float totalTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
                float delta = totalTime - m_Time;
                m_Time = totalTime;

                #if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused)
                    delta = 0.0f;
                else if (!Application.isPlaying)
                    delta = Mathf.Min(delta, 0.033f);
                #endif

                deltaTime = delta * timeMultiplier;
                simulationTime += deltaTime;
            }
        }

        // Function that releases the resources and resets all the internal variables
        public void ReleaseSimulationResources()
        {
            // Release the textures
            ReleaseSimulationBuffersGPU();
            ReleaseSimulationBuffersCPU();

            // Reset the spectrum data
            spectrum.patchSizes = Vector4.zero;
            spectrum.patchOrientation = Vector4.zero;
            spectrum.patchWindSpeed = Vector4.zero;
            spectrum.patchWindDirDampener = Vector4.zero;

            // Reset the rendering data
            rendering.patchAmplitudeMultiplier = Vector4.zero;
            rendering.patchCurrentSpeed = Vector4.zero;
            rendering.patchFadeA = Vector4.zero;
            rendering.patchFadeB = Vector4.one;

            // Reset the resolution data
            simulationResolution = 0;
            numActiveBands = 0;

            // Reset the simulation time
            simulationTime = 0;
            deltaTime = 0;
            active = false;
        }
    }

    partial class WaterSystem
    {
        // Simulation shader and kernels
        ComputeShader m_WaterSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsKernel;
        int m_EvaluateNormalsJacobianKernel;
        int m_PrepareCausticsGeometryKernel;
        int m_EvaluateInstanceDataKernel;
        int m_EvaluateInstanceDataInfiniteKernel;

        // FFT shader and kernels
        ComputeShader m_FourierTransformCS;
        int m_RowPassTi_Kernel;
        int m_ColPassTi_Kernel;

        // Intermediate RTHandles used to render the water
        RTHandle m_HtRs = null;
        RTHandle m_HtIs = null;
        RTHandle m_FFTRowPassRs = null;
        RTHandle m_FFTRowPassIs = null;

        bool m_GPUReadbackMode;
        WaterSimulationResolution m_WaterBandResolution;
        WaterSimulationResolution m_WaterCPUSimulationResolution;

        internal bool replicateSimulationOnCPU => !m_GPUReadbackMode;
        internal WaterSimulationResolution simationRes => m_WaterBandResolution;
        internal WaterSimulationResolution cpuSimationRes => m_WaterCPUSimulationResolution;

        void InitializeWaterSimulation()
        {
            m_GPUReadbackMode = m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.waterScriptInteractionsMode == WaterScriptInteractionsMode.GPUReadback;
            m_WaterBandResolution = m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.waterSimulationResolution;

            m_WaterCPUSimulationResolution = m_WaterBandResolution;
            if ((int)m_WaterBandResolution != 64 && !m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.waterFullCPUSimulation)
                m_WaterCPUSimulationResolution = (WaterSimulationResolution)((int)m_WaterBandResolution / 2);

            // Simulation shader and kernels
            m_WaterSimulationCS = m_RuntimeResources.waterSimulationCS;
            m_InitializePhillipsSpectrumKernel = m_WaterSimulationCS.FindKernel("InitializePhillipsSpectrum");
            m_EvaluateDispersionKernel = m_WaterSimulationCS.FindKernel("EvaluateDispersion");
            m_EvaluateNormalsKernel = m_WaterSimulationCS.FindKernel("EvaluateNormals");
            m_EvaluateNormalsJacobianKernel = m_WaterSimulationCS.FindKernel("EvaluateNormalsJacobian");
            m_PrepareCausticsGeometryKernel = m_WaterSimulationCS.FindKernel("PrepareCausticsGeometry");
            m_EvaluateInstanceDataKernel = m_WaterSimulationCS.FindKernel("EvaluateInstanceData");
            m_EvaluateInstanceDataInfiniteKernel = m_WaterSimulationCS.FindKernel("EvaluateInstanceDataInfinite");

            // FFT shader and kernels
            m_FourierTransformCS = m_RuntimeResources.fourierTransformCS;
            GetFFTKernels(m_FourierTransformCS, m_WaterBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

            // Allocate all the RTHanles required for the water simulation
            int textureRes = (int)m_WaterBandResolution;
            int maxBandCount = WaterConsts.k_WaterHighBandCount;
            m_HtRs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtIs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassRs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassIs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
        }


        void ReleaseWaterSimulation()
        {
            // Release all the RTHandles
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
        }

        void UpdateGPUWaterSimulation(CommandBuffer cmd, WaterSurface currentWater)
        {
            // Evaluate the band count
            int bandCount = currentWater.simulation.numActiveBands;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceSimulation)))
            {
                // Bind the constant buffers
                BindPerSurfaceConstantBuffer(cmd, m_WaterSimulationCS, m_ShaderVariablesWaterPerSurface[currentWater.surfaceIndex]);
                BindPerSurfaceConstantBuffer(cmd, m_FourierTransformCS, m_ShaderVariablesWaterPerSurface[currentWater.surfaceIndex]);

                // Raise the keyword if it should be raised
                SetupWaterShaderKeyword(cmd, m_EnableDecalWorkflow, bandCount, false);

                // Number of tiles we will need to dispatch
                int tileCount = (int)m_WaterBandResolution / 8;

                // Do we need to re-evaluate the Phillips spectrum?
                if (!currentWater.simulation.gpuSpectrumValid)
                {
                    // Convert the noise to the Phillips spectrum
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, HDShaderIDs._H0BufferRW, currentWater.simulation.gpuBuffers.phillipsSpectrumBuffer);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, tileCount, tileCount, bandCount);
                    currentWater.simulation.gpuSpectrumValid = true;
                }

                // Execute the dispersion
                cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._H0Buffer, currentWater.simulation.gpuBuffers.phillipsSpectrumBuffer);
                cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtRealBufferRW, m_HtRs);
                cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtImaginaryBufferRW, m_HtIs);
                cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateDispersionKernel, tileCount, tileCount, bandCount);

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
                int kernel = currentWater.HasSimulationFoam() ? m_EvaluateNormalsJacobianKernel : m_EvaluateNormalsKernel;
                cmd.SetComputeTextureParam(m_WaterSimulationCS, kernel, HDShaderIDs._WaterDisplacementBuffer, currentWater.simulation.gpuBuffers.displacementBuffer);
                cmd.SetComputeTextureParam(m_WaterSimulationCS, kernel, HDShaderIDs._WaterAdditionalDataBufferRW, currentWater.simulation.gpuBuffers.additionalDataBuffer);
                cmd.DispatchCompute(m_WaterSimulationCS, kernel, tileCount, tileCount, bandCount);

                // Make sure the mip-maps are generated
                currentWater.simulation.gpuBuffers.additionalDataBuffer.rt.Create();
                cmd.GenerateMips(currentWater.simulation.gpuBuffers.additionalDataBuffer.rt);

                // For the CPU Simulation
                if (m_GPUReadbackMode)
                    cmd.IncrementUpdateCount(currentWater.simulation.gpuBuffers.displacementBuffer.rt);
            }
        }

        void EvaluateWaterCaustics(CommandBuffer cmd, WaterSurface currentWater)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceCaustics)))
            {
                // Initialize the indices buffer
                int meshResolution = WaterConsts.k_WaterCausticsMeshResolution;
                if (!m_CausticsBufferGeometryInitialized)
                {
                    int meshTileCount = (meshResolution + 7) / 8;
                    cmd.SetComputeIntParam(m_WaterSimulationCS, HDShaderIDs._CausticGeometryResolution, meshResolution);
                    cmd.SetComputeBufferParam(m_WaterSimulationCS, m_PrepareCausticsGeometryKernel, "_CauticsGeometryRW", m_CausticsGeometry);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_PrepareCausticsGeometryKernel, meshTileCount, meshTileCount, 1);
                    m_CausticsBufferGeometryInitialized = true;
                }

                // Make sure that the caustics texture is allocated
                int causticsResolution = (int)currentWater.causticsResolution;
                currentWater.simulation.CheckCausticsResources(true, causticsResolution);

                // Setup properties
                var perSurfaceCB = m_ShaderVariablesWaterPerSurface[currentWater.surfaceIndex];
                m_WaterMaterialPropertyBlock.SetConstantBuffer(HDShaderIDs._ShaderVariablesWaterPerSurface, perSurfaceCB, 0, perSurfaceCB.stride);
                m_WaterMaterialPropertyBlock.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, currentWater.simulation.gpuBuffers.additionalDataBuffer);
                m_WaterMaterialPropertyBlock.SetFloat(HDShaderIDs._CausticsVirtualPlane, currentWater.virtualPlaneDistance);
                m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticsNormalsMipOffset, EvaluateNormalMipOffset(m_WaterBandResolution));
                m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticGeometryResolution, meshResolution);

                // Render the caustics
                CoreUtils.SetRenderTarget(cmd, currentWater.simulation.gpuBuffers.causticsBuffer, clearFlag: ClearFlag.Color, Color.black);
                cmd.DrawProcedural(m_CausticsGeometry, Matrix4x4.identity, m_CausticsMaterial, 0, MeshTopology.Triangles, WaterConsts.k_WaterCausticsMeshNumQuads * 6, 1, m_WaterMaterialPropertyBlock);

                // Make sure the mip-maps are generated
                currentWater.simulation.gpuBuffers.causticsBuffer.rt.Create();
                cmd.GenerateMips(currentWater.simulation.gpuBuffers.causticsBuffer.rt);
            }
        }
    }
}

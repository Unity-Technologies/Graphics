using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
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
        // The number of bands that are actually evaluated
        internal int numActiveBands;

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
            return (a.numActiveBands == b.numActiveBands)
                && (a.patchSizes == b.patchSizes)
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
            return (a.numActiveBands != b.numActiveBands)
                || (a.patchSizes != b.patchSizes)
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

        // The fade start for each band
        internal Vector4 patchFadeStart;

        // The fade distance for each band
        internal Vector4 patchFadeDistance;

        // The fade value for each band
        internal Vector4 patchFadeValue;

        // Matrix to convert from the water space to world space
        internal float4x4 waterToWorldMatrix;

        // Matrix to convert from the world space to water space
        internal float4x4 worldToWaterMatrix;
    }

    internal class WaterSimulationResources
    {
        // System simulation time, used for timeline.
        private bool active = true;

        // Overall time that has passed since Unity has been initialized
        private float m_Time = 0;
        // Current simulation time (used to compute the dispersion of the Phillips spectrum)
        public float simulationTime = 0;
        // Delta time of the current frame
        public float deltaTime = 0;

        // Resolution at which the water system is ran
        public int simulationResolution = 0;
        // The number bands that we will be running the simulation at
        public int maxNumBands = 0;

        // The type of the surface
        public WaterSurfaceType surfaceType;

        // The spectrum parameters
        public WaterSpectrumParameters spectrum = new WaterSpectrumParameters();

        // The rendering parameters
        public WaterRenderingParameters rendering = new WaterRenderingParameters();

        // The set of GPU Buffers used to run the simulation
        public WaterSimulationResourcesGPU gpuBuffers = null;

        // The set of CPU Buffers used to run the simulation
        public WaterSimulationResourcesCPU cpuBuffers = null;

        public void AllocateSimulationBuffersGPU()
        {
            gpuBuffers = new WaterSimulationResourcesGPU();
            gpuBuffers.phillipsSpectrumBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, maxNumBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            gpuBuffers.displacementBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, maxNumBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            gpuBuffers.additionalDataBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, maxNumBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
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
            cpuBuffers.h0BufferCPU = new NativeArray<float2>(simulationResolution * simulationResolution * maxNumBands, Allocator.Persistent);
            cpuBuffers.displacementBufferCPU = new NativeArray<float4>(simulationResolution * simulationResolution * maxNumBands, Allocator.Persistent);
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
        public void InitializeSimulationResources(int simulationRes, int nbBands)
        {
            // Keep track of the values that constraint the texture allocation.
            simulationResolution = simulationRes;
            maxNumBands = nbBands;
            EnableTimeSteps();
        }

        // Function that validates the resources (size and if allocated)
        public bool ValidResources(int simulationRes, int nbBands)
        {
            return (simulationRes == simulationResolution)
            && (nbBands == maxNumBands)
            && AllocatedTextures();
        }

        // Function that makes sure that all the textures are allocated
        public bool AllocatedTextures()
        {
            return (gpuBuffers != null);
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
                    gpuBuffers.causticsBuffer = RTHandles.Alloc(causticsResolution, causticsResolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
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
            m_Time = Time.realtimeSinceStartup;
            active = true;
            simulationTime = 0;
        }

        public void DisableTimeSteps()
        {
            active = false;
            m_Time = 0;
            simulationTime = 0;
        }

        public bool HasActiveTimeSteps()
        {
            return active;
        }

        // Function that computes the delta time for the frame
        public void Update(float totalTime, float timeMultiplier)
        {
            if (HasActiveTimeSteps())
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPaused)
#endif
                {
                    deltaTime = (totalTime - m_Time) * timeMultiplier;
                    simulationTime += deltaTime;
                }
                m_Time = totalTime;
            }
        }

        // Function that releases the resources and resets all the internal variables
        public void ReleaseSimulationResources()
        {
            // Release the textures
            ReleaseSimulationBuffersGPU();
            ReleaseSimulationBuffersCPU();

            // Reset the spectrum data
            spectrum.numActiveBands = 0;
            spectrum.patchSizes = Vector4.zero;
            spectrum.patchOrientation = Vector4.zero;
            spectrum.patchWindSpeed = Vector4.zero;
            spectrum.patchWindDirDampener = Vector4.zero;

            // Reset the rendering data
            rendering.patchAmplitudeMultiplier = Vector4.zero;
            rendering.patchCurrentSpeed = Vector4.zero;
            rendering.patchFadeStart = Vector4.zero;
            rendering.patchFadeDistance = Vector4.zero;
            rendering.patchFadeValue = Vector4.zero;

            // Reset the resolution data
            simulationResolution = 0;
            maxNumBands = 0;

            // Reset the simulation time
            m_Time = 0;
            simulationTime = 0;
            deltaTime = 0;
            active = false;
        }
    }

    public partial class HDRenderPipeline
    {
        // Simulation shader and kernels
        ComputeShader m_WaterSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsFoamKernel;
        int m_CopyAdditionalDataKernel;
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
        RTHandle m_AdditionalData = null;

        WaterSimulationResolution m_WaterBandResolution = WaterSimulationResolution.Medium128;

        void InitializeWaterSimulation()
        {
            m_WaterBandResolution = m_Asset.currentPlatformRenderPipelineSettings.waterSimulationResolution;

            // Simulation shader and kernels
            m_WaterSimulationCS = m_Asset.renderPipelineResources.shaders.waterSimulationCS;
            m_InitializePhillipsSpectrumKernel = m_WaterSimulationCS.FindKernel("InitializePhillipsSpectrum");
            m_EvaluateDispersionKernel = m_WaterSimulationCS.FindKernel("EvaluateDispersion");
            m_EvaluateNormalsFoamKernel = m_WaterSimulationCS.FindKernel("EvaluateNormalsFoam");
            m_CopyAdditionalDataKernel = m_WaterSimulationCS.FindKernel("CopyAdditionalData");
            m_PrepareCausticsGeometryKernel = m_WaterSimulationCS.FindKernel("PrepareCausticsGeometry");
            m_EvaluateInstanceDataKernel = m_WaterSimulationCS.FindKernel("EvaluateInstanceData");
            m_EvaluateInstanceDataInfiniteKernel = m_WaterSimulationCS.FindKernel("EvaluateInstanceDataInfinite");

            // FFT shader and kernels
            m_FourierTransformCS = m_Asset.renderPipelineResources.shaders.fourierTransformCS;
            GetFFTKernels(m_FourierTransformCS, m_WaterBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

            // Allocate all the RTHanles required for the water simulation
            int textureRes = (int)m_WaterBandResolution;
            int maxBandCount = WaterConsts.k_WaterHighBandCount;
            m_HtRs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtIs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassRs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassIs = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_AdditionalData = RTHandles.Alloc(textureRes, textureRes, maxBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
        }


        void ReleaseWaterSimulation()
        {
            // Release all the RTHandles
            RTHandles.Release(m_AdditionalData);
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
        }

        void UpdateGPUWaterSimulation(CommandBuffer cmd, WaterSurface currentWater, bool gpuResourcesInvalid, bool validHistory, ShaderVariablesWater shaderVariablesWater)
        {
            // Bind the constant buffer
            ConstantBuffer.Push(cmd, shaderVariablesWater, m_WaterSimulationCS, HDShaderIDs._ShaderVariablesWater);

            // Evaluate the band count
            int bandCount = currentWater.simulation.spectrum.numActiveBands;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceSimulation)))
            {
                // Raise the keyword if it should be raised
                SetupWaterShaderKeyword(cmd, bandCount, false);

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
                shaderVariablesWater._WaterInitialFrame = validHistory ? 0 : 1;

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

                // Bind the constant buffer
                ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_CausticsMaterial, HDShaderIDs._ShaderVariablesWater);

                // Render the caustics
                CoreUtils.SetRenderTarget(cmd, currentWater.simulation.gpuBuffers.causticsBuffer, clearFlag: ClearFlag.Color, Color.black);
                m_WaterMaterialPropertyBlock.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, currentWater.simulation.gpuBuffers.additionalDataBuffer);
                m_WaterMaterialPropertyBlock.SetFloat(HDShaderIDs._CausticsVirtualPlane, currentWater.virtualPlaneDistance);
                m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticsNormalsMipOffset, EvaluateNormalMipOffset(m_WaterBandResolution));
                m_WaterMaterialPropertyBlock.SetInt(HDShaderIDs._CausticGeometryResolution, meshResolution);
                cmd.DrawProcedural(m_CausticsGeometry, Matrix4x4.identity, m_CausticsMaterial, 0, MeshTopology.Triangles, WaterConsts.k_WaterCausticsMeshNumQuads * 6, 1, m_WaterMaterialPropertyBlock);

                // Make sure the mip-maps are generated
                currentWater.simulation.gpuBuffers.causticsBuffer.rt.GenerateMips();
            }
        }
    }
}

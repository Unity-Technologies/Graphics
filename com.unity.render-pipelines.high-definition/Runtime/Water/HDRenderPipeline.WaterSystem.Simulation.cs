using Unity.Collections;
using Unity.Mathematics;
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

    internal class WaterSimulationResources
    {
        // Overall time that has passed since Unity has been initialized
        private float m_Time = 0;

        // Current simulation time (used to compute the dispersion of the Phillips spectrum)
        public float simulationTime = 0;

        // Delta time of the current frame
        public float deltaTime = 0;

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

        // Wave amplitude multiplier
        public Vector4 waveAmplitude = Vector4.zero;

        // Maximum wave height of the simulation
        public float maxWaveHeight = 0.0f;

        // The set of GPU Buffers used to run the simulation
        public WaterSimulationResourcesGPU gpuBuffers = null;

        // The set of CPU Buffers used to run the simulation
        public WaterSimulationResourcesCPU cpuBuffers = null;

        public void AllocateSimulationBuffersGPU()
        {
            gpuBuffers = new WaterSimulationResourcesGPU();
            gpuBuffers.phillipsSpectrumBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            gpuBuffers.displacementBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            gpuBuffers.additionalDataBuffer = RTHandles.Alloc(simulationResolution, simulationResolution, numBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
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
            cpuBuffers.h0BufferCPU = new NativeArray<float2>(simulationResolution * simulationResolution * numBands, Allocator.Persistent);
            cpuBuffers.displacementBufferCPU = new NativeArray<float4>(simulationResolution * simulationResolution * numBands, Allocator.Persistent);
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
            numBands = nbBands;
            m_Time = Time.realtimeSinceStartup;
        }

        // Function that validates the resources (size and if allocated)
        public bool ValidResources(int simulationRes, int nbBands)
        {
            return (simulationRes == simulationResolution) && (nbBands == numBands) && AllocatedTextures();
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

        // Function that computes the delta time for the frame
        public void Update(float totalTime, float timeMultiplier)
        {
            deltaTime = (totalTime - m_Time) * timeMultiplier;
            simulationTime += deltaTime;
            m_Time = totalTime;
        }

        // Function that releases the resources and resets all the internal variables
        public void ReleaseSimulationResources()
        {
            // Release the textures
            ReleaseSimulationBuffersGPU();
            ReleaseSimulationBuffersCPU();

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
}

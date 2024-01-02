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

        // Value that defines the patch sizes of the bands (up to 4)
        internal Vector4 patchSizes;

        // The wind speed, orientation and weight used to evaluate the Phillips spectrum
        internal Vector4 patchWindSpeed;

        // Value that defines the wind directionality to each patch (up to 4)
        internal Vector4 patchWindDirDampener;

        // The wind orientation (in degrees) for each band
        internal Vector4 patchWindOrientation;

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
                && (a.patchWindOrientation == b.patchWindOrientation);
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
                || (a.patchWindOrientation != b.patchWindOrientation);
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

        // The current orientation
        internal Vector4 patchCurrentOrientation;

        // The fade start for each band
        internal Vector4 patchFadeStart;

        // The fade distance for each band
        internal Vector4 patchFadeDistance;

        // The fade value for each band
        internal Vector4 patchFadeValue;
    }

    internal class WaterSimulationResources
    {
        // Overall time that has passed since Unity has been initialized
        float m_Time;
        // Current simulation time in seconds (used to compute the dispersion of the Phillips spectrum)
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
            m_Time = Time.realtimeSinceStartup;
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
                    gpuBuffers.causticsBuffer = RTHandles.Alloc(causticsResolution, causticsResolution, 1, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
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
        public void Update(float timeMultiplier)
        {
            float totalTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float delta = totalTime - m_Time;
            m_Time = totalTime;

            #if UNITY_EDITOR
            if (EditorApplication.isPaused)
                delta = 0.0f;
            #endif

            deltaTime = delta * timeMultiplier;
            simulationTime += deltaTime;
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
            spectrum.patchWindSpeed = Vector4.zero;
            spectrum.patchWindOrientation = Vector4.zero;
            spectrum.patchWindDirDampener = Vector4.zero;

            // Reset the rendering data
            rendering.patchAmplitudeMultiplier = Vector4.zero;
            rendering.patchCurrentSpeed = Vector4.zero;
            rendering.patchCurrentOrientation = Vector4.zero;
            rendering.patchFadeStart = Vector4.zero;
            rendering.patchFadeDistance = Vector4.zero;
            rendering.patchFadeValue = Vector4.zero;

            // Reset the resolution data
            simulationResolution = 0;
            maxNumBands = 0;

            // Reset the simulation time
            simulationTime = 0;
            deltaTime = 0;
        }
    }
}

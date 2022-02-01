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
        /// <summary>
        /// The water simulation will be ran at a resolution of 512x512 samples per band.
        /// </summary>
        Ultra512 = 512
    }

    internal class WaterSimulationResources
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

        // Texture that holds the caustics
        public RTHandle causticsBuffer = null;

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
            m_Time = Time.realtimeSinceStartup;

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

        public void CheckCausticsResources(bool used, int causticsResolution)
        {
            if (used)
            {
                bool needsAllocation = true;
                if (causticsBuffer != null)
                {
                    needsAllocation = causticsBuffer.rt.width != causticsResolution;
                    if (needsAllocation)
                        RTHandles.Release(causticsBuffer);
                }

                if (needsAllocation)
                    causticsBuffer = RTHandles.Alloc(causticsResolution, causticsResolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
            }
            else
            {
                if (causticsBuffer != null)
                {
                    RTHandles.Release(causticsBuffer);
                    causticsBuffer = null;
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
            RTHandles.Release(additionalDataBuffer);
            additionalDataBuffer = null;
            RTHandles.Release(displacementBuffer);
            displacementBuffer = null;
            RTHandles.Release(phillipsSpectrumBuffer);
            phillipsSpectrumBuffer = null;

            // Release the caustics resources if allocated
            if (causticsBuffer != null)
            {
                RTHandles.Release(causticsBuffer);
                causticsBuffer = null;
            }

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

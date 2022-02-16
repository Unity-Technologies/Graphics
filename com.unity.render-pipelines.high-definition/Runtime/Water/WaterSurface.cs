using System;
using Unity.Mathematics;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Water surface component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class WaterSurface : MonoBehaviour
    {
        /// <summary>
        /// Controls the type of geometry used to render the water surface when non infinite.
        /// </summary>
        public enum WaterGeometryType
        {
            /// <summary>
            /// The water surface will be rendered as a procedural quad.
            /// </summary>
            Quad,
            /// <summary>
            /// The water will be rendered with a mesh provided by the user.
            /// </summary>
            Custom
        }

        #region Instance Management

        // Management to avoid memory allocations at fetch time
        internal static HashSet<WaterSurface> instances = new HashSet<WaterSurface>();
        internal static WaterSurface[] instancesAsArray = null;
        internal static int instanceCount = 0;

        internal static void RegisterInstance(WaterSurface surface)
        {
            instances.Add(surface);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterSurface[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }

        internal static void UnregisterInstance(WaterSurface surface)
        {
            instances.Remove(surface);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterSurface[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }
        #endregion

        #region Water General
        /// <summary>
        /// When enabled, the water surface is rendered as an infinite surface. This is designed to render seas and oceans.
        /// </summary>
        [Tooltip("When enabled, the water surface is rendered as an infinite surface. This is designed to render seas and oceans.")]
        public bool infinite = true;

        /// <summary>
        /// When enabled, the water system evaluates 4 simulations bands instead of 2. This may increase the amount of detail depending on the water max patch size, but will increase the cost of the water surface.
        /// </summary>
        [Tooltip("When enabled, the water system evaluates 4 simulations bands instead of 2. This may increase the amount of detail depending on the water max patch size, but will increase the cost of the water surface.")]
        public bool highFrequencyBands = true;

        /// <summary>
        /// Specifies the type of geometry used to render the water surface when non infinite.
        /// </summary>
        [Tooltip("Specifies the type of geometry used to render the water surface when non infinite.")]
        public WaterGeometryType geometryType = WaterGeometryType.Quad;

        /// <summary>
        /// Sets the geometry to use when rendering in finite and custom geometry type mode. The vertical position of the vertices will be overridden to keep the surface of water leveled.
        /// </summary>
        [Tooltip("Sets the geometry to use when rendering in finite and custom geometry type mode. The vertical position of the vertices will be overridden to keep the surface of water leveled.")]
        public Mesh geometry = null;
        #endregion

        #region Water CPU Simulation
        /// <summary>
        /// When enabled, HDRP will evaluate the water simulation on the CPU for C# script height requests. Enabling this will significantly increase the CPU cost of the feature.
        /// </summary>
        [Tooltip("When enabled, HDRP will evaluate the water simulation on the CPU for C# script height requests. Enabling this will significantly increase the CPU cost of the feature.")]
        public bool cpuSimulation = false;

        /// <summary>
        /// Specifies if the CPU simulation should be evaluated at full or half resolution. When in full resolution, the visual fidelity will be higher but the cost of the simulation will increase.
        /// </summary>
        [Tooltip("Specifies if the CPU simulation should be evaluated at full or half resolution. When in full resolution, the visual fidelity will be higher but the cost of the simulation will increase.")]
        public bool cpuFullResolution = false;

        /// <summary>
        /// Specifies if the CPU simulation should evaluate all four band (when active) or should limit itself to the first two bands. A higher band count will allow for a higher visual fidelity but the cost of the simulation will increase.
        /// </summary>
        [Tooltip("Specifies if the CPU simulation should evaluate all four band (when active) or should limit itself to the first two bands. A higher band count will allow for a higher visual fidelity but the cost of the simulation will increase.")]
        public bool cpuEvaluateAllBands = false;
        #endregion

        #region Water Simulation
        /// <summary>
        /// Sets the maximum patch size that is used to run the water simulation. The wind speed is adjusted to remain coherent with the patch size.
        /// </summary>
        [Tooltip("Sets the maximum patch size that is used to run the water simulation. The wind speed is adjusted to remain coherent with the patch size.")]
        public float waterMaxPatchSize = 500.0f;

        /// <summary>
        /// Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).
        /// </summary>
        [Tooltip("Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).")]
        public Vector4 amplitude = Vector4.one;

        /// <summary>
        /// Sets the choppiness factor the waves. Higher values combined with high wind speed may introduce visual artifacts.
        /// </summary>
        [Tooltip("Sets the choppiness factor the waves. Higher values combined with high wind speed may introduce visual artifacts.")]
        public float choppiness = 0.9f;

        /// <summary>
        /// Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.
        /// </summary>
        [Tooltip("Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.")]
        public float timeMultiplier = 1.0f;
        #endregion

        #region Water Material
        /// <summary>
        /// Sets a custom material that will be used to render the water surface. If set to None a default material is used.
        /// </summary>
        [Tooltip("Sets a custom material that will be used to render the water surface. If set to None a default material is used.")]
        public Material customMaterial = null;

        /// <summary>
        /// Controls the smoothness used to render the water surface.
        /// </summary>
        [Tooltip("Controls the smoothness used to render the water surface.")]
        public float waterSmoothness = 0.95f;
        #endregion

        #region Water Refraction
        /// <summary>
        /// Sets the color that is used to simulate the under-water refraction.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the under-water refraction.")]
        [ColorUsage(false)]
        public Color refractionColor = new Color(0.00f, 0.45f, 0.65f);

        /// <summary>
        /// Controls the maximum distance in meters used to clamp the under water refraction depth. Higher value increases the distortion amount.
        /// </summary>
        [Tooltip("Controls the maximum distance in meters used to clamp the under water refraction depth. Higher value increases the distortion amount.")]
        public float maxRefractionDistance = 1.0f;

        /// <summary>
        /// Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.
        /// </summary>
        [Tooltip("Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.")]
        public float absorptionDistance = 5.0f;
        #endregion

        #region Water Scattering
        /// <summary>
        /// Sets the color that is used to simulate the under-water scattering.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the under-water scattering.")]
        [ColorUsage(false)]
        public Color scatteringColor = new Color(0.0f, 0.12f, 0.25f);

        /// <summary>
        /// Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.
        /// </summary>
        [Tooltip("Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float heightScattering = 0.5f;

        /// <summary>
        /// Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.
        /// </summary>
        [Tooltip("Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float displacementScattering = 0.5f;

        /// <summary>
        /// Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.
        /// </summary>
        [Tooltip("Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.")]
        public float directLightTipScattering = 0.5f;

        /// <summary>
        /// Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.
        /// </summary>
        [Tooltip("Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.")]
        public float directLightBodyScattering = 0.5f;
        #endregion

        #region Water Caustics General
        /// <summary>
        /// When enabled, the water surface will render caustics.
        /// </summary>
        [Tooltip("When enabled, the water surface will render caustics.")]
        public bool caustics = true;

        /// <summary>
        /// Sets the intensity of the under-water caustics.
        /// </summary>
        [Tooltip("Sets the intensity of the under-water caustics.")]
        public float causticsIntensity = 0.5f;

        /// <summary>
        /// Sets the vertical blending distance for the water caustics.
        /// </summary>
        [Tooltip("Sets the vertical blending distance for the water caustics.")]
        public float causticsPlaneBlendDistance = 1.0f;

        /// <summary>
        /// Defines what algorithm is used for caustics rendering.
        /// </summary>
        public enum WaterCausticsType
        {
            /// <summary>
            /// The water simulation is used to evaluate the caustics.
            /// </summary>
            Simulation,
            /// <summary>
            /// A procedural approach is used for the caustics
            /// </summary>
            Procedural
        }

        /// <summary>
        /// Specifies the algorithm used to evaluate the caustics. Procedural is completely uncorrelated to the water simulation. Simulation relies on the normals of the selected band and has a higher cost.
        /// </summary>
        [Tooltip("Specifies the algorithm used to evaluate the caustics. Procedural is completely uncorrelated to the water simulation. Simulation relies on the normals of the selected band and has a higher cost.")]
        public WaterCausticsType causticsAlgorithm = WaterCausticsType.Simulation;
        #endregion

        #region Water Caustics Simulation
        /// <summary>
        /// Defines the resolution a which caustics are rendered (simulation only).
        /// </summary>
        public enum WaterCausticsResolution
        {
            /// <summary>
            /// The water caustics are rendered at 256x256
            /// </summary>
            Caustics256 = 256,
            /// <summary>
            /// The water caustics are rendered at 512x512
            /// </summary>
            Caustics512 = 512,
            /// <summary>
            /// The water caustics are rendered at 1024x1024
            /// </summary>
            Caustics1024 = 1024,
        }

        /// <summary>
        /// Specifies the resolution at which the water caustics are rendered (simulation only).
        /// </summary>
        [Tooltip("Specifies the resolution at which the water caustics are rendered (simulation only).")]
        public WaterCausticsResolution causticsResolution = WaterCausticsResolution.Caustics512;

        /// <summary>
        /// Controls which band is used for the caustics evaluation.
        /// </summary>
        [Tooltip("Controls which band is used for the caustics evaluation.")]
        public int causticsBand = 1;

        /// <summary>
        /// Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artefacts.
        /// </summary>
        [Tooltip("Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artefacts.")]
        public float virtualPlaneDistance = 5.0f;
        #endregion

        #region Water Caustics Procedural
        /// <summary>
        /// Sets the tiling factor of the under-water caustics.
        /// </summary>
        [Tooltip("Sets the tiling factor of the under-water caustics.")]
        public float causticsTiling = 1.5f;

        /// <summary>
        /// Sets the scrolling speed of the under-water caustics.
        /// </summary>
        [Tooltip("Sets the scrolling speed of the under-water caustics.")]
        public float causticsSpeed = 0.0f;
        #endregion

        #region Water Foam
        /// <summary>
        /// Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and chopiness values.
        /// </summary>
        [Tooltip("Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and chopiness values.")]
        public float simulationFoamAmount = 0.5f;

        /// <summary>
        /// Controls the life span of the surface foam. A higher value will cause the foam to persist longer and leave a trail.
        /// </summary>
        [Tooltip("Controls the life span of the surface foam. A higher value will cause the foam to persist longer and leave a trail.")]
        public float simulationFoamDrag = 0.5f;

        /// <summary>
        /// Controls the simulation foam smoothness.
        /// </summary>
        [Tooltip("Controls the surface foam smoothness.")]
        public float simulationFoamSmoothness = 0.3f;

        /// <summary>
        /// Set the texture used to attenuate or supress the simulation foam.
        /// </summary>
        [Tooltip("Sets the texture used to attenuate or supress the simulation foam.")]
        public Texture2D foamMask = null;

        /// <summary>
        /// Sets the extent of the foam mask in meters.
        /// </summary>
        [Tooltip("Sets the extent of the foam mask in meters.")]
        public Vector2 foamMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Sets the offset of the foam mask in meters.
        /// </summary>
        [Tooltip("Sets the offset of the foam mask in meters.")]
        public Vector2 foamMaskOffset = new Vector2(0.0f, 0.0f);
        #endregion

        #region Water Masking
        /// <summary>
        /// Sets the texture used to attenuate or supress the lower, band X and Y, (red channel) or higher, band Z and W, (green channel) water frequencies.
        /// </summary>
        [Tooltip("Sets the texture used to attenuate or supress the lower, band X and Y, (red channel) or higher, band Z and W, (green channel) water frequencies.")]
        public Texture2D waterMask = null;

        /// <summary>
        /// Sets the extent of the water mask in meters.
        /// </summary>
        [Tooltip("Sets the extent of the water mask in meters.")]
        public Vector2 waterMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Sets the offset of the water mask in meters.
        /// </summary>
        [Tooltip("Sets the offset of the water mask in meters.")]
        public Vector2 waterMaskOffset = new Vector2(0.0f, 0.0f);
        #endregion

        #region Water Wind
        /// <summary>
        /// Sets the wind orientation in degrees. Zero means north (-x). Wind speed is clamped to be coherent with the patch size.
        /// </summary>
        [Tooltip("Sets the wind orientation in degrees. Zero means north (-x). Wind speed is clamped to be coherent with the patch size.")]
        public float windOrientation = 0.0f;

        /// <summary>
        /// Controls the wind speed in kilometers per hour.
        /// </summary>
        [Tooltip("Controls the wind speed in kilometers per hour.")]
        public float windSpeed = 30.0f;

        /// <summary>
        /// Controls the proportion in which the wind affects the current of the water.
        /// </summary>
        [Tooltip("Controls the proportion in which the wind affects the current of the water.")]
        public float windAffectCurrent = 0.5f;

        /// <summary>
        /// Controls the foam amount depending on the wind speed.
        /// </summary>
        [Tooltip("Controls the foam amount depending on the wind speed.")]
        public AnimationCurve windFoamCurve = new AnimationCurve(new Keyframe(0f, 0.0f), new Keyframe(0.2f, 0.0f), new Keyframe(0.3f, 1.0f), new Keyframe(1.0f, 1.0f));
        #endregion

        #region Water Rendering
        /// <summary>
        /// Specifies the decal layers that affect the water surface.
        /// </summary>
        [Tooltip("Specifies the decal layers that affect the water surface.")]
        public DecalLayerEnum decalLayerMask = DecalLayerEnum.DecalLayerDefault;

        /// <summary>
        /// Specifies the light layers that affect the water surface.
        /// </summary>
        [Tooltip("Specifies the light layers that affect the water surface.")]
        public LightLayerEnum lightLayerMask = LightLayerEnum.LightLayerDefault;
        #endregion

        #region Water Underwater
        /// <summary>
        /// When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.
        /// </summary>
        [Tooltip("When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.")]
        public bool underWater = false;

        /// <summary>
        /// Sets a box collider that will be used to define the volume where the under water effect is applied for non infinite surfaces.
        /// </summary>
        [Tooltip("Sets a box collider that will be used to define the volume where the under water effect is applied for non infinite surfaces.")]
        public BoxCollider volumeBounds = null;

        /// <summary>
        /// Sets maximum depth at which the under water effect is evaluated for infinite surfaces.
        /// </summary>
        [Tooltip("Sets maximum depth at which the under water effect is evaluated for infinite surfaces.")]
        public float volumeDepth = 50.0f;

        /// <summary>
        /// Sets a priority value that is used to define which surface should be considered for under water rendering in the case of multiple overlapping surfaces.
        /// </summary>
        [Tooltip(" Sets a priority value that is used to define which surface should be considered for under water rendering in the case of multiple overlapping surfaces.")]
        public int volumePrority = 0;

        /// <summary>
        /// Sets a vertical distance to the water surface at which the blending between above and under water starts.
        /// </summary>
        [Tooltip("Sets a vertical distance to the water surface at which the blending between above and under water starts.")]
        public float transitionSize = 0.1f;

        /// <summary>
        /// Sets the multiplier for the Absorption Distance when the camera is under water. A value of 2.0 means you will see twice as far underwater.
        /// </summary>
        [Tooltip("Sets the multiplier for the  Absorption Distance when the camera is under water. A value of 2.0 means you will see twice as far underwater.")]
        public float absorbtionDistanceMultiplier = 1.0f;
        #endregion

        // Internal simulation data
        internal WaterSimulationResources simulation = null;

        internal void CheckResources(int bandResolution, int bandCount, bool cpuSimActive, out bool gpuBuffersValid, out bool cpuBuffersValid)
        {
            // By default we shouldn't need an update
            gpuBuffersValid = true;
            cpuBuffersValid = true;

            // If the previously existing resources are not valid, just release them
            if (simulation != null && !simulation.ValidResources(bandResolution, bandCount))
            {
                simulation.ReleaseSimulationResources();
                simulation = null;
            }

            // Will we need to enable the CPU simulation?
            bool cpuSimulationActive = cpuSimActive && cpuSimulation;

            // If the resources have not been allocated for this water surface, allocate them
            if (simulation == null)
            {
                // In this case the CPU buffers are invalid and we need to rebuild them
                gpuBuffersValid = false;
                cpuBuffersValid = false;

                // Create the simulation resources
                simulation = new WaterSimulationResources();

                // Initialize for the allocation
                simulation.InitializeSimulationResources(bandResolution, bandCount);

                // GPU buffers should always be allocated
                simulation.AllocateSimulationBuffersGPU();

                // CPU buffers should be allocated only if required
                if (cpuSimulationActive)
                    simulation.AllocateSimulationBuffersCPU();
            }

            // One more case that we need check here is that if the CPU became required
            if (!cpuSimulationActive && simulation.cpuBuffers != null)
            {
                simulation.ReleaseSimulationBuffersCPU();
                cpuBuffersValid = false;
            }

            // One more case that we need check here is that if the CPU became required
            if (cpuSimulationActive && simulation.cpuBuffers == null)
            {
                simulation.AllocateSimulationBuffersCPU();
                cpuBuffersValid = false;
            }

            // If the spectrum defining data changed, we need to invalidate the buffers
            if (simulation.windSpeed != windSpeed
                || simulation.windOrientation != windOrientation
                || simulation.windAffectCurrent != windAffectCurrent
                || simulation.patchSizes.x != waterMaxPatchSize)
            {
                gpuBuffersValid = false;
                cpuBuffersValid = false;
            }

			// TODO: Handle properly the change of resolution to be able to not do this every frame.
            cpuBuffersValid = false;

            // Re-evaluate the simulation data
            UpdateSimulationData();
        }

        /// <summary>
        /// Function that returns the water surface CPU simulation resolution.
        /// </summary>
        /// <returns>A value of time WaterSimulationResolution that defines the current water surface CPU simulation resolution.</returns>
        public WaterSimulationResolution GetSimulationResolutionCPU()
        {
            int resolution;
            if (simulation.simulationResolution != 64)
                resolution = cpuFullResolution ? simulation.simulationResolution : simulation.simulationResolution / 2;
            else
                resolution = simulation.simulationResolution;
            return (WaterSimulationResolution)resolution;
        }

        /// <summary>
        /// Function that returns the number of frequency bands that the water surface CPU simulation evaluates.
        /// </summary>
        /// <returns>The number of bands that the water surface CPU simulation evaluates.</returns>
        public int GetSimulationBandCountCPU()
        {
            if (highFrequencyBands)
                return cpuEvaluateAllBands ? 4 : 2;
            else
                return 2;
        }

        /// <summary>
        /// Function that fills a WaterSimSearchData with the data of the current water surface.
        /// </summary>
        /// <param name="wsd">The water simulation search data to fill.</param>
        /// <returns>A boolean that defines if the function was able to fill the search data.</returns>
        public bool FillWaterSearchData(ref WaterSimSearchData wsd)
        {
            // If a displacement buffer is available return it,
            if (simulation != null && simulation.cpuBuffers != null)
            {
                wsd.displacementData = simulation.cpuBuffers.displacementBufferCPU;
                wsd.waterSurfaceElevation = transform.position.y;
                wsd.simulationRes = (int)GetSimulationResolutionCPU();
                wsd.choppiness = choppiness;
                wsd.amplitude = simulation.waveAmplitude;
                wsd.patchSizes = simulation.patchSizes;
                wsd.bandCount = GetSimulationBandCountCPU();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Function that attempts to evaluate the water surface height at a given location.
        /// </summary>
        /// <param name="wsp">The water simulation search parameters that defines the location we are targeting and additional parameters.</param>
        /// <param name="wsr">The water simulation search result that contains the result height, error, etc.</param>
        /// <returns>A boolean that defines if the function was able to execute the evaluation.</returns>
        public bool FindWaterSurfaceHeight(WaterSearchParameters wsp, out WaterSearchResult wsr)
        {
            // Invalidate the search result in case the simulation data is not available
            wsr.error = float.MaxValue;
            wsr.height = 0;
            wsr.candidateLocation = float3.zero;
            wsr.numIterations = wsp.maxIterations;

            // Try to to fill the search data and run the evaluation.
            WaterSimSearchData wsd = new WaterSimSearchData();
            if (FillWaterSearchData(ref wsd))
            {
                HDRenderPipeline.FindWaterSurfaceHeight(wsd, wsp, out wsr);
                return true;
            }
            return false;
        }

        private void Start()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void Awake()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnEnable()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnDisable()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);
        }

        void UpdateSimulationData()
        {
            simulation.windSpeed = windSpeed;
            simulation.windOrientation = windOrientation;
            simulation.windAffectCurrent = windAffectCurrent;
            simulation.patchSizes = HDRenderPipeline.ComputeBandPatchSizes(waterMaxPatchSize);
            simulation.patchWindSpeed = HDRenderPipeline.ComputeWindSpeeds(simulation.windSpeed, simulation.patchSizes);
            HDRenderPipeline.ComputeMaximumWaveHeight(amplitude, simulation.patchWindSpeed.x, highFrequencyBands, out simulation.waveAmplitude, out simulation.maxWaveHeight);
        }

        void OnDestroy()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);

            // Make sure to release the resources if they have been created (before HDRP destroys them)
            if (simulation != null && simulation.AllocatedTextures())
                simulation.ReleaseSimulationResources();
        }
    }
}

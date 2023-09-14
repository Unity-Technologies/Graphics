using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Enum that defines the type of a given water surface.
    /// </summary>
    public enum WaterSurfaceType
    {
        /// <summary>
        /// The water surface is either a Ocean, Sea, Lake or a large water body.
        /// </summary>
        [InspectorName("Ocean, Sea or Lake")]
        OceanSeaLake,

        /// <summary>
        /// The water surface is a river a stream.
        /// </summary>
        River,

        /// <summary>
        /// The water surface is a pool or a small water body.
        /// </summary>
        Pool,
    }

    /// <summary>
    /// Controls the type of geometry used to render the water surface.
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
        CustomMesh,

        /// <summary>
        /// The water surface will be rendered as a infinite plane.
        /// </summary>
        Infinite
    }

    /// <summary>
    /// Controls how a property is defined for a water surface.
    /// </summary>
    public enum WaterPropertyOverrideMode
    {
        /// <summary>
        /// The property in inherited from an other similar property.
        /// </summary>
        Inherit,

        /// <summary>
        /// The property needs to be specified individually.
        /// </summary>
        Custom,
    }


    /// <summary>
    /// Water surface component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class WaterSurface : MonoBehaviour
    {
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
        /// Specifies the nature of the water body that the water system needs to simulate.
        /// </summary>
        public WaterSurfaceType surfaceType = WaterSurfaceType.OceanSeaLake;

        /// <summary>
        /// Specifies the type of geometry used to render the water surface.
        /// </summary>
        public WaterGeometryType geometryType = WaterGeometryType.Infinite;

        /// <summary>
        /// Sets the geometry to use when rendering in quad and custom geometry type mode. The vertical position of the vertices will be overridden to keep the surface of water leveled.
        /// </summary>
        public Mesh mesh = null;
        #endregion

        #region Water CPU Simulation
        /// <summary>
        /// When enabled, HDRP will evaluate the water simulation on the CPU for C# script height requests. Enabling this will significantly increase the CPU cost of the feature.
        /// </summary>
        public bool cpuSimulation = false;

        /// <summary>
        /// Specifies if the CPU simulation should be evaluated at full or half resolution. When in full resolution, the visual fidelity will be higher but the cost of the simulation will increase.
        /// </summary>
        public bool cpuFullResolution = false;

        /// <summary>
        /// Specifies if the CPU simulation should evaluate the ripples as part of the simulation. Including ripples will allow a higher visual fidelity but the cost of the simulation will increase.
        /// </summary>
        public bool cpuEvaluateRipples = false;
        #endregion

        #region Water Simulation
        /// <summary>
        /// Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.
        /// </summary>
        public float timeMultiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float repetitionSize = 500.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeWindSpeed = 30.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeWindOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeCurrentSpeedValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeCurrentOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeChaos = 0.8f;

        /// <summary>
        ///
        /// </summary>
        public float largeBand0Multiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        public bool largeBand0FadeToggle = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand0FadeStart = 1500.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand0FadeDistance = 3000.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand1Multiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public bool largeBand1FadeToggle = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand1FadeStart = 300.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand1FadeDistance = 800.0f;

        /// <summary>
        /// When enabled, the water system allows you to simulate and render a ripples simulation for finer details.
        /// </summary>
        public bool ripples = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesWindSpeed = 8.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public WaterPropertyOverrideMode ripplesWindOrientationMode = WaterPropertyOverrideMode.Inherit;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesWindOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public WaterPropertyOverrideMode ripplesCurrentMode = WaterPropertyOverrideMode.Inherit;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesCurrentSpeedValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesCurrentOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesChaos = 0.8f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public bool ripplesFadeToggle = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesFadeStart = 50.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesFadeDistance = 200.0f;
        #endregion

        #region Water Material
        /// <summary>
        /// Sets a custom material that will be used to render the water surface. If set to None a default material is used.
        /// </summary>
        public Material customMaterial = null;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float startSmoothness = 0.95f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float endSmoothness = 0.85f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float smoothnessFadeStart = 100.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float smoothnessFadeDistance = 500.0f;

#if UNITY_EDITOR
        static internal bool IsWaterMaterial(Material material)
        {
            return material.shader.FindSubshaderTagValue(0, (ShaderTagId)"ShaderGraphTargetId").name == "WaterSubTarget";
        }
#endif
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
        public Color scatteringColor = new Color(0.0f, 0.27f, 0.23f);

        /// <summary>
        /// Controls the intensity of the ambient scattering term. This can be adjusted for artistic purposes.
        /// </summary>
        [Tooltip("Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float ambientScattering = 0.1f;

        /// <summary>
        /// Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.
        /// </summary>
        [Tooltip("Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float heightScattering = 0.1f;

        /// <summary>
        /// Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.
        /// </summary>
        [Tooltip("Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float displacementScattering = 0.3f;

        /// <summary>
        /// Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.
        /// </summary>
        [Tooltip("Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.")]
        public float directLightTipScattering = 0.6f;

        /// <summary>
        /// Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.
        /// </summary>
        [Tooltip("Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.")]
        public float directLightBodyScattering = 0.4f;
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
        public WaterCausticsResolution causticsResolution = WaterCausticsResolution.Caustics256;

        /// <summary>
        /// Controls which band is used for the caustics evaluation.
        /// </summary>
        [Tooltip("Controls which band is used for the caustics evaluation.")]
        public int causticsBand = 1;

        /// <summary>
        /// Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artefacts.
        /// </summary>
        public float virtualPlaneDistance = 5.0f;
        #endregion

        #region Water Foam
        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public bool foam = true;

        /// <summary>
        /// Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and chopiness values.
        /// </summary>
        [Tooltip("Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and chopiness values.")]
        public float simulationFoamAmount = 0.3f;

        /// <summary>
        /// Controls the life span of the surface foam. A higher value will cause the foam to persist longer and leave a trail.
        /// </summary>
        [Tooltip("Controls the life span of the surface foam. A higher value will cause the foam to persist longer and leave a trail.")]
        public float simulationFoamDrag = 0.0f;

        /// <summary>
        /// Controls the simulation foam smoothness.
        /// </summary>
        [Tooltip("Controls the surface foam smoothness.")]
        public float simulationFoamSmoothness = 1.0f;

        /// <summary>
        /// Set the texture used to attenuate or suppress the simulation foam.
        /// </summary>
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

        /// <summary>
        /// Controls the foam amount depending on the wind speed.
        /// </summary>
        public AnimationCurve windFoamCurve = new AnimationCurve(new Keyframe(0f, 0.0f), new Keyframe(0.2f, 0.0f), new Keyframe(0.3f, 1.0f), new Keyframe(1.0f, 1.0f));

        /// <summary>
        /// Set the texture used to attenuate or suppress the simulation foam.
        /// </summary>
        [Tooltip("Set the texture used to attenuate or suppress the simulation foam.")]
        public Texture2D foamTexture = null;

        /// <summary>
        /// Set the per meter tiling for the foam texture.
        /// </summary>
        [Tooltip("Set the per meter tiling for the foam texture.")]
        public float foamTextureTiling = 0.2f;
        #endregion

        #region Water Masking
        /// <summary>
        /// Sets the texture used to attenuate or supress the swell, agiation and ripples water frequencies.
        /// </summary>
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
        [Tooltip("Sets a priority value that is used to define which surface should be considered for under water rendering in the case of multiple overlapping surfaces.")]
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

        internal void CheckResources(int bandResolution, int bandCount, bool cpuSimActive, out bool gpuSpectrumValid, out bool cpuSpectrumValid, out bool historyValid)
        {
            // By default we shouldn't need an update
            gpuSpectrumValid = true;
            cpuSpectrumValid = true;
            historyValid = true;

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
                gpuSpectrumValid = false;
                cpuSpectrumValid = false;
                historyValid = false;

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
                cpuSpectrumValid = false;
            }

            // One more case that we need check here is that if the CPU became required
            if (cpuSimulationActive && simulation.cpuBuffers == null)
            {
                simulation.AllocateSimulationBuffersCPU();
                cpuSpectrumValid = false;
            }

            // Evaluate the spectrum parameters
            WaterSpectrumParameters spectrum = EvaluateSpectrumParams(surfaceType);

            if (simulation.spectrum.numActiveBands != spectrum.numActiveBands)
            {
                historyValid = false;
            }

            // If the spectrum defining data changed, we need to invalidate the buffers
            if (simulation.spectrum != spectrum)
            {
                // Mark the spectrums as invalid and assign the new one
                gpuSpectrumValid = false;
                cpuSpectrumValid = false;
                simulation.spectrum = spectrum;
            }

            // TODO: Handle properly the change of resolution to be able to not do this every frame.
            cpuSpectrumValid = false;

            // Re-evaluate the simulation data
            simulation.rendering = EvaluateRenderingParams(surfaceType);
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
                wsd.spectrum = simulation.spectrum;
                wsd.rendering = simulation.rendering;
                wsd.activeBandCount = HDRenderPipeline.EvaluateCPUBandCount(surfaceType, ripples, cpuEvaluateRipples);
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

            #if UNITY_EDITOR
            // Handle scene visibility
            PrefabStage.prefabStageOpened += RegisterWaterSurfaceVisibilityUpdatePrefabStage;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) // In case the prefab stage is already opened when enabling the decal
                RegisterWaterSurfaceVisibilityUpdatePrefabStage();
            #endif
        }

        private void OnDisable()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);

        #if UNITY_EDITOR
            UnregisterWaterSurfaceVisibilityUpdatePrefabStage();
            PrefabStage.prefabStageOpened -= RegisterWaterSurfaceVisibilityUpdatePrefabStage;
        #endif
        }

        #if UNITY_EDITOR
        void RegisterWaterSurfaceVisibilityUpdatePrefabStage(PrefabStage stage = null)
        {
            SceneView.duringSceneGui -= UpdateWaterSurfaceVisibilityPrefabStage;
            SceneView.duringSceneGui += UpdateWaterSurfaceVisibilityPrefabStage;
        }

        void UnregisterWaterSurfaceVisibilityUpdatePrefabStage()
            => SceneView.duringSceneGui -= UpdateWaterSurfaceVisibilityPrefabStage;

        bool m_LastPrefabStageVisibility = true;
        void UpdateWaterSurfaceVisibilityPrefabStage(SceneView sv)
        {
            bool showWaterSurface = true;

            // If prefab context is not hidden, then we should render the WaterSurface
            if (!CoreUtils.IsSceneViewPrefabStageContextHidden())
                showWaterSurface = true;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                bool isWaterSurfaceInPrefabStage = gameObject.scene == stage.scene;

                if (!isWaterSurfaceInPrefabStage && stage.mode == PrefabStage.Mode.InIsolation)
                    showWaterSurface = false;
                if (!isWaterSurfaceInPrefabStage && CoreUtils.IsSceneViewPrefabStageContextHidden())
                    showWaterSurface = false;
            }

            // Update decal visibility based on showDecal
            if (!m_LastPrefabStageVisibility && showWaterSurface)
            {
                RegisterInstance(this);
            }
            else if (m_LastPrefabStageVisibility && !showWaterSurface)
            {
                UnregisterInstance(this);
            }
            m_LastPrefabStageVisibility = showWaterSurface;
        }
        #endif

        bool SpectrumParametersAreValid(WaterSpectrumParameters spectrum)
        {
            return (simulation.spectrum == spectrum);
        }

        // Function that evaluates the spectrum data for the ocean/sea/lake case
        WaterSpectrumParameters EvaluateSpectrumParams(WaterSurfaceType type)
        {
            WaterSpectrumParameters spectrum = new WaterSpectrumParameters();
            switch (type)
            {
                case WaterSurfaceType.OceanSeaLake:
                {
                    // Compute the patch size of the biggest swell band
                    float swellPatchSize = repetitionSize;

                    // We need to evaluate the radio between the first and second band
                    float swellSecondBandRatio = HDRenderPipeline.EvaluateSwellSecondPatchSize(swellPatchSize);

                    // Propagate the high frequency bands flag
                    spectrum.numActiveBands = ripples ? 3 : 2;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = swellPatchSize;
                    spectrum.patchSizes.y = swellPatchSize / swellSecondBandRatio;
                    spectrum.patchSizes.z = WaterConsts.k_RipplesBandSize;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = largeWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    spectrum.patchWindSpeed.y = largeWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    spectrum.patchWindSpeed.z = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    spectrum.patchWindOrientation.x = largeWindOrientationValue;
                    spectrum.patchWindOrientation.y = largeWindOrientationValue;
                    spectrum.patchWindOrientation.z = ripplesWindOrientationMode == WaterPropertyOverrideMode.Inherit ? largeWindOrientationValue : ripplesWindOrientationValue;

                    // Direction dampener
                    spectrum.patchWindDirDampener.x = largeChaos;
                    spectrum.patchWindDirDampener.y = largeChaos;
                    spectrum.patchWindDirDampener.z = ripplesChaos;
                }
                break;
                case WaterSurfaceType.River:
                {
                    // Propagate the high frequency bands flag
                    spectrum.numActiveBands = ripples ? 2 : 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = repetitionSize;
                    spectrum.patchSizes.y = WaterConsts.k_RipplesBandSize;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = largeWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    spectrum.patchWindSpeed.y = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    spectrum.patchWindOrientation.x = largeWindOrientationValue;
                    spectrum.patchWindOrientation.y = ripplesWindOrientationMode == WaterPropertyOverrideMode.Inherit ? largeWindOrientationValue : ripplesWindOrientationValue;

                    // Direction dampener
                    spectrum.patchWindDirDampener.x = largeChaos;
                    spectrum.patchWindDirDampener.y = ripplesChaos;
                }
                break;
                case WaterSurfaceType.Pool:
                {
                    // Propagate the high frequency bands flag
                    spectrum.numActiveBands = 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = WaterConsts.k_RipplesBandSize;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    spectrum.patchWindOrientation.x = ripplesWindOrientationValue;

                    // Direction dampener
                    spectrum.patchWindDirDampener.x = ripplesChaos;
                }
                break;
            }

            return spectrum;
        }

        WaterRenderingParameters EvaluateRenderingParams(WaterSurfaceType type)
        {
            WaterRenderingParameters rendering = new WaterRenderingParameters();

            // Propagate the simulation time to the rendering structure
            rendering.simulationTime = simulation.simulationTime;

            switch (type)
            {
                case WaterSurfaceType.OceanSeaLake:
                {
                    // Deduce the patch sizes from the max patch size for the swell
                    rendering.patchAmplitudeMultiplier.x = largeBand0Multiplier;
                    rendering.patchAmplitudeMultiplier.y = largeBand1Multiplier;
                    rendering.patchAmplitudeMultiplier.z = 1.0f;

                    // Keep track of the directionality is used
                    float swellCurrentSpeed = largeCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    rendering.patchCurrentSpeed.x = swellCurrentSpeed;
                    rendering.patchCurrentSpeed.y = swellCurrentSpeed;
                    rendering.patchCurrentSpeed.z = ripplesCurrentMode == WaterPropertyOverrideMode.Inherit ? swellCurrentSpeed : ripplesCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    rendering.patchCurrentOrientation.x = largeCurrentOrientationValue;
                    rendering.patchCurrentOrientation.y = largeCurrentOrientationValue;
                    rendering.patchCurrentOrientation.z = ripplesCurrentMode == WaterPropertyOverrideMode.Inherit ? largeCurrentOrientationValue : ripplesCurrentOrientationValue;

                    // Fade parameters
                    rendering.patchFadeStart.x = largeBand0FadeStart;
                    rendering.patchFadeStart.y = largeBand1FadeStart;
                    rendering.patchFadeStart.z = ripplesFadeStart;
                    rendering.patchFadeDistance.x = largeBand0FadeDistance;
                    rendering.patchFadeDistance.y = largeBand1FadeDistance;
                    rendering.patchFadeDistance.z = ripplesFadeDistance;
                    rendering.patchFadeValue.x = largeBand0FadeToggle ? 0.0f : 1.0f;
                    rendering.patchFadeValue.y = largeBand1FadeToggle ? 0.0f : 1.0f;
                    rendering.patchFadeValue.z = ripplesFadeToggle ? 0.0f : 1.0f;
                }
                break;
                case WaterSurfaceType.River:
                {
                    // Deduce the patch sizes from the max patch size for the swell
                    rendering.patchAmplitudeMultiplier.x = largeBand0Multiplier;
                    rendering.patchAmplitudeMultiplier.y = ripples ? 1.0f : 0.0f;

                    // Keep track of the directionality is used
                    rendering.patchCurrentSpeed.x = largeCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    rendering.patchCurrentSpeed.y = ripplesCurrentMode == WaterPropertyOverrideMode.Inherit ? rendering.patchCurrentSpeed.x : ripplesCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    rendering.patchCurrentOrientation.x = largeCurrentOrientationValue;
                    rendering.patchCurrentOrientation.y = ripplesCurrentMode == WaterPropertyOverrideMode.Inherit ? rendering.patchCurrentOrientation.x : ripplesCurrentOrientationValue;

                    // Fade parameters
                    rendering.patchFadeStart.x = largeBand0FadeStart;
                    rendering.patchFadeStart.y = ripplesFadeStart;
                    rendering.patchFadeDistance.x = largeBand0FadeDistance;
                    rendering.patchFadeDistance.y = ripplesFadeDistance;
                    rendering.patchFadeValue.x = largeBand0FadeToggle ? 0.0f : 1.0f;
                    rendering.patchFadeValue.y = ripplesFadeToggle ? 0.0f : 1.0f;
                }
                break;
                case WaterSurfaceType.Pool:
                {
                    // Deduce the patch sizes from the max patch size for the swell
                    rendering.patchAmplitudeMultiplier.x = 1.0f;
                    rendering.patchAmplitudeMultiplier.y = 0.0f;

                    // Keep track of the directionality is used
                    rendering.patchCurrentSpeed.x = ripplesCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    rendering.patchCurrentOrientation.x = ripplesCurrentOrientationValue;

                    // Fade parameters
                    rendering.patchFadeStart.x = ripplesFadeStart;
                    rendering.patchFadeDistance.x = ripplesFadeDistance;
                    rendering.patchFadeValue.x = ripplesFadeToggle ? 0.0f : 1.0f;
                }
                break;
            }
            return rendering;
        }

        internal bool IsInfinite()
        {
            return (surfaceType == WaterSurfaceType.OceanSeaLake) ? geometryType == WaterGeometryType.Infinite : false;
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

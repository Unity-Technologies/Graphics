using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;
using Unity.Collections;
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
        /// The water will be rendered with one or multiple Mesh Renderers provided by the user.
        /// </summary>
        Custom,

        /// <summary>
        /// The water surface will be rendered as a set of quads that cover the area defined by the water surface.
        /// </summary>
        InstancedQuads,

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
    public partial class WaterSurface : MonoBehaviour
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
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        /// <summary>
        /// Sets the speed of the water simulation. This allows to slow down the waves' speed or to accelerate it.
        /// </summary>
        public float timeMultiplier = 1.0f;
        #endregion

        #region Water CPU Simulation
        /// <summary>
        /// When enabled, HDRP will evaluate the water simulation on the CPU for C# script requests. Enabling this will significantly increase the CPU cost of the feature.
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

        #region Water Material
        /// <summary>
        /// Sets a custom material that will be used to render the water surface. If set to None a default material is used.
        /// </summary>
        public Material customMaterial = null;

        /// <summary>
        ///
        /// </summary>
        public float startSmoothness = 0.95f;

        /// <summary>
        ///
        /// </summary>
        public float endSmoothness = 0.85f;

        /// <summary>
        ///
        /// </summary>
        public float smoothnessFadeStart = 100.0f;

        /// <summary>
        ///
        /// </summary>
        public float smoothnessFadeDistance = 500.0f;
        #endregion

        #region Water Refraction
        /// <summary>
        /// Sets the color that is used to simulate the under-water refraction.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the under-water refraction.")]
        [ColorUsage(false)]
        public Color refractionColor = new Color(0.00f, 0.45f, 0.65f);

        /// <summary>
        /// Controls the maximum distance in meters used to clamp the underwater refraction depth. Higher value increases the distortion amount.
        /// </summary>
        [Tooltip("Controls the maximum distance in meters used to clamp the underwater refraction depth. Higher value increases the distortion amount.")]
        public float maxRefractionDistance = 1.0f;

        /// <summary>
        /// Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.
        /// </summary>
        [Tooltip("Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.")]
        public float absorptionDistance = 5.0f;
        #endregion

        #region Water Scattering
        /// <summary>
        /// Sets the color that is used to simulate the water light scattering.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the water light scattering.")]
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
        /// Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artifacts.
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
        /// Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and choppiness values.
        /// </summary>
        [Tooltip("Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and choppiness values.")]
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

        #region Water Miscellaneous
        /// <summary>
        /// Specifies the rendering layers that affect the water surface.
        /// </summary>
        [Tooltip("Specifies the rendering layers that affect the water surface.")]
        public RenderingLayerMask renderingLayerMask = RenderingLayerMask.Default;

        /// <summary>
        /// Sets the debug mode for a given water surface.
        /// </summary>
        public WaterDebugMode debugMode = WaterDebugMode.None;

        /// <summary>
        /// Sets the water mask debug mode for a given water surface.
        /// </summary>
        public WaterMaskDebugMode waterMaskDebugMode = WaterMaskDebugMode.RedChannel;

        /// <summary>
        /// Sets the water current debug mode for a given water surface.
        /// </summary>
        public WaterCurrentDebugMode waterCurrentDebugMode = WaterCurrentDebugMode.Large;

        /// <summary>
        /// Sets a multiplier for the arrow density in the current debug mode.
        /// </summary>
        public float currentDebugMultiplier = 1.0f;
        #endregion

        #region Water Underwater
        /// <summary>
        /// When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.
        /// </summary>
        [Tooltip("When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.")]
        public bool underWater = false;

        /// <summary>
        /// Sets a box collider that will be used to define the volume where the underwater effect is applied for non infinite surfaces.
        /// </summary>
        [Tooltip("Sets a box collider that will be used to define the volume where the underwater effect is applied for non infinite surfaces.")]
        public BoxCollider volumeBounds = null;

        /// <summary>
        /// Sets maximum depth at which the underwater effect is evaluated for infinite surfaces.
        /// </summary>
        [Tooltip("Sets maximum depth at which the underwater effect is evaluated for infinite surfaces.")]
        public float volumeDepth = 50.0f;

        /// <summary>
        /// Sets the maximum height at which the underwater effect is evaluated for infinite surfaces. This allows to cover the underwater scenario when deformers are higher than waves or ripples.
        /// </summary>
        [Tooltip("Sets the maximum height at which the underwater effect is evaluated for infinite surfaces. This allows to cover the underwater scenario when deformers are higher than waves or ripples.")]
        public float volumeHeight = 0.0f;

        /// <summary>
        /// Sets a priority value that is used to define which surface should be considered for underwater rendering in the case of multiple overlapping surfaces.
        /// </summary>
        [Tooltip("Sets a priority value that is used to define which surface should be considered for underwater rendering in the case of multiple overlapping surfaces.")]
        public int volumePrority = 0;

        /// <summary>
        /// Sets the multiplier for the Absorption Distance when the camera is underwater. A value of 2.0 means you will see twice as far underwater.
        /// </summary>
        [Tooltip("Sets the multiplier for the  Absorption Distance when the camera is underwater. A value of 2.0 means you will see twice as far underwater.")]
        public float absorptionDistanceMultiplier = 1.0f;

        /// <summary>
        /// Sets the offset used to evaluated the underwater refraction. Higher values produce blurrier results but introduce aliasing artifacts.
        /// </summary>
        [Tooltip("Sets the offset used to evaluated the underwater refraction. Higher values produce blurrier results but introduce aliasing artifacts.")]
        public int colorPyramidOffset = 1;

        /// <summary>
        /// Sets the contribution of the ambient probe to the underwater scattering color.
        /// </summary>
        public float underWaterAmbientProbeContribution = 1.0f;

        /// <summary>
        /// Controls how the scattering color is evaluated for the underwater scenario.
        /// Controls how the scattering color is evaluated for the underwater scenario.
        /// </summary>
        public enum UnderWaterScatteringColorMode
        {
            /// <summary>
            /// The scattering color is used as the underwater scattering color.
            /// </summary>
            ScatteringColor,

            /// <summary>
            /// The property needs to be specified manually.
            /// </summary>
            Custom,
        }

        /// <summary>
        /// Sets how the underwater scattering color is specified.
        /// </summary>
        [Tooltip("Sets how the underwater scattering color is specified.")]
        public UnderWaterScatteringColorMode underWaterScatteringColorMode = UnderWaterScatteringColorMode.ScatteringColor;

        /// <summary>
        /// Sets the color that is used to simulate the scattering when the camera is under-water.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the scattering when the camera is under-water.")]
        [ColorUsage(false)]
        public Color underWaterScatteringColor = new Color(0.0f, 0.27f, 0.23f);
        #endregion

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
            if (simulation != null
                && simulation.cpuBuffers != null
                && HDRenderPipeline.currentPipeline != null
                && HDRenderPipeline.currentPipeline.m_ActiveWaterSimulationCPU)
            {
                // General
                wsd.simulationTime = simulation.simulationTime;

                // Simulation
                wsd.simulationRes = (int)GetSimulationResolutionCPU();
                wsd.displacementData = simulation.cpuBuffers.displacementBufferCPU;
                wsd.spectrum = simulation.spectrum;
                wsd.rendering = simulation.rendering;
                wsd.activeBandCount = HDRenderPipeline.EvaluateCPUBandCount(surfaceType, ripples, cpuEvaluateRipples);

                // Mask data
                FillWaterMaskData(ref wsd);

                // Deformation data
                FillWaterDeformationData(ref wsd);

                // Current map data
                FillCurrentMapData(ref wsd);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Function that attempts to evaluate the projection of a given world space position onto the water surface. This requires the HDRP asset and the water surface to have script interactions enabled.
        /// </summary>
        /// <param name="wsp">The water simulation search parameters that defines the location we are targeting and additional parameters.</param>
        /// <param name="wsr">The water simulation search result that contains the result position, error, etc.</param>
        /// <returns>A boolean that defines if the function was able to execute the evaluation.</returns>
        public bool ProjectPointOnWaterSurface(WaterSearchParameters wsp, out WaterSearchResult wsr)
        {
            // Invalidate the search result in case the simulation data is not available
            wsr.error = float.MaxValue;
            wsr.projectedPositionWS = float3(0, 0, 0);
            wsr.candidateLocationWS = float3(0, 0, 0);
            wsr.currentDirectionWS = float3(1, 0, 0);
            wsr.numIterations = wsp.maxIterations;

            // Try to to fill the search data and run the evaluation.
            WaterSimSearchData wsd = new WaterSimSearchData();
            if (FillWaterSearchData(ref wsd))
            {
                HDRenderPipeline.ProjectPointOnWaterSurface(wsd, wsp, out wsr);
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
            k_Migration.Migrate(this);

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

        internal bool IsInstancedQuads()
        {
            return IsInfinite() || geometryType == WaterGeometryType.InstancedQuads;
        }

        internal bool IsInfinite()
        {
            return (surfaceType == WaterSurfaceType.OceanSeaLake) ? geometryType == WaterGeometryType.Infinite : false;
        }

        internal float3 UpVector()
        {
            float3 upDir = transform.up;
            return IsInstancedQuads() ? float3(0, 1, 0) : upDir;
        }

        internal void ReleaseResources()
        {
            // Make sure to release the resources if they have been created (before HDRP destroys them)
            if (simulation != null && simulation.AllocatedTextures())
                simulation.ReleaseSimulationResources();
            simulation = null;

            // CPU Simulation textures
            ReleaseCurrentMapResources();
            ReleaseDeformationResources();
            ReleaseWaterMaskResources();
        }

        void OnDestroy()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);

            // Release all CPU and GPU buffers
            ReleaseResources();
        }

        /// <summary>
        /// Function that returns the caustics buffer for the water surface. If the feature is disabled or the resource is not available the function returns null.
        /// </summary>
        /// <param name="regionSize">Output parameter that returns the size of the caustics region.</param>
        /// <returns>A monochrome texture that holds the caustics simulation of the water surface.</returns>
        public Texture GetCausticsBuffer(out float regionSize)
        {
            if (caustics && simulation?.gpuBuffers?.causticsBuffer != null)
            {
                regionSize = simulation.spectrum.patchSizes[causticsBand];
                return simulation.gpuBuffers.causticsBuffer;
            }
            regionSize = 0.0f;
            return null;
        }
    }
}

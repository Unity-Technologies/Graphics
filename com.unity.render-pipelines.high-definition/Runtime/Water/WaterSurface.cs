using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Water surface component.
    /// </summary>
    public class WaterSurface : MonoBehaviour
    {
        /// <summary>
        /// Controls the type of geometry used to render the water surface when non infinite.
        /// </summary>
        public enum WaterGeometryType
        {
            // The water surface will be rendered as a procedural quad.
            Quad,
            // The water will be rendered with a mesh provided by the user.
            Custom
        }

        #region Water General
        /// <summary>
        /// When enabled, the water surface is rendered as an infinite surface. This is designed to render seas and oceans.
        /// </summary>
        [Tooltip("When enabled, the water surface is rendered as an infinite surface. This is designed to render seas and oceans.")]
        public bool infinite = true;

        /// <summary>
        /// Sets the earth radius that is used to curve the water surface when in infite mode to simulate the earth's curvature.
        /// </summary>
        [Tooltip("Sets the earth radius that is used to curve the water surface when in infite mode to simulate the earth's curvature.")]
        public float earthRadius = 6371000.0f;

        /// <summary>
        /// Specifies the type of geometry used to render the water surface when non infinite.
        /// </summary>
        [Tooltip("Specifies the type of geometry used to render the water surface when non infinite.")]
        public WaterGeometryType geometryType = WaterGeometryType.Quad;

        /// <summary>
        /// Sets the geometry to use when rendering in finite and custom geometry type mode.
        /// </summary>
        [Tooltip("Sets the geometry to use when rendering in finite and custom geometry type mode.")]
        public Mesh geometry = null;
        #endregion

        #region Water Simulation
        /// <summary>
        /// Sets the maximum patch size that is used to run the water simulation.
        /// </summary>
        [Tooltip("Sets the maximum patch size that is used to run the water simulation.")]
        public float waterMaxPatchSize = 500.0f;

        /// <summary>
        /// When enabled, the water system and rendering evaluates 4 simulations bands instead of 2. This may increase the amount of detail depending on the water max patch size, but will increase the cost of the water surface.
        /// </summary>
        [Tooltip("When enabled, the water system and rendering evaluates 4 simulations bands instead of 2. This may increase the amount of detail depending on the water max patch size, but will increase the cost of the water surface.")]
        public bool highBandCount = true;

        /// <summary>
        /// Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).
        /// </summary>
        [Tooltip("Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).")]
        public Vector4 amplitude = Vector4.one;

        /// <summary>
        /// Sets the choppiness factor the waves. Higher values combined with high wind speed may introduce visual artifacts.
        /// </summary>
        [Tooltip("Sets the choppiness factor the waves. Higher values combined with high wind speed may introduce visual artifacts.")]
        public float choppiness = 1.0f;

        /// <summary>
        /// Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.
        /// </summary>
        [Tooltip("Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.")]
        public float timeMultiplier = 1.0f;
        #endregion

        #region Water Material
        /// <summary>
        /// Sets the material that is used to render the water surface. If set to None a default material is used.
        /// </summary>
        [Tooltip("Sets the material that is used to render the water surface. If set to None a default material is used.")]
        public Material material = null;
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
        public Color refractionColor = new Color(0.00f, 0.45f, 0.65f);

        /// <summary>
        /// Sets the maximum distance used to clamp the under water refraction depth.
        /// </summary>
        [Tooltip("Sets the maximum distance used to clamp the under water refraction depth.")]
        public float maxRefractionDistance = 1.0f;

        /// <summary>
        /// Sets the maximum distance that the camera can perceive under the water surface.
        /// </summary>
        [Tooltip("Sets the maximum distance that the camera can perceive under the water surface.")]
        public float maxAbsorptionDistance = 5.0f;
        #endregion

        #region Water Scattering
        /// <summary>
        /// Sets the color that is used to simulate the under-water scattering.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the under-water scattering.")]
        public Color scatteringColor = new Color(0.0f, 0.12f, 0.25f);

        /// <summary>
        /// Sets the multiplier applied to the scattering factor to attenuate the scattering term.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the under-water scattering.")]
        public float scatteringFactor = 0.5f;

        /// <summary>
        /// Sets the intensity of the height based scattering.
        /// </summary>
        [Tooltip("Sets the intensity of the height based scattering.")]
        public float heightScattering = 0.5f;

        /// <summary>
        /// Sets the intensity of the displacement based scattering.
        /// </summary>
        [Tooltip("Sets the intensity of the displacement based scattering.")]
        public float displacementScattering = 0.5f;

        /// <summary>
        /// Sets the intensity of the direct light scattering on the tip of the waves.
        /// </summary>
        [Tooltip("Sets the intensity of the direct light scattering on the tip of the waves.")]
        public float directLightTipScattering = 0.5f;

        /// <summary>
        /// Sets the intensity of the direct light scattering on the tip of the waves.
        /// </summary>
        [Tooltip("Sets the intensity of the direct light scattering on the body of the waves.")]
        public float directLightBodyScattering = 0.5f;
        #endregion

        #region Water Caustics
        /// <summary>
        /// Set the intensity of the under-water caustics.
        /// </summary>
        [Tooltip("Set the intensity of the under-water caustics.")]
        public float causticsIntensity = 0.5f;

        /// <summary>
        /// Controls the amount of dispersion of the caustics.
        /// </summary>
        [Tooltip("Controls the amount of dispersion of the caustics.")]
        public float causticsDispersionAmount = 0.5f;

        /// <summary>
        /// Set the tiling factor of the under-water caustics.
        /// </summary>
        [Tooltip("Set the tiling factor of the under-water caustics.")]
        public float causticsTiling = 1.5f;

        /// <summary>
        /// Set the scrolling speed of the under-water caustics.
        /// </summary>
        [Tooltip("Set the scrolling speed of the under-water caustics.")]
        public float causticsSpeed = 0.0f;

        /// <summary>
        /// Set the vertical plane offset at which the caustics start.
        /// </summary>
        [Tooltip("Set the vertical plane offset at which the caustics start.")]
        public float causticsPlaneOffset = 0.0f;
        #endregion

        #region Water Foam
        /// <summary>
        /// Controls the foam smoothness.
        /// </summary>
        [Tooltip("Controls the foam smoothness.")]
        public float surfaceFoamSmoothness = 0.3f;

        /// <summary>
        /// Controls the foam intensity.
        /// </summary>
        [Tooltip("Controls the foam intensity.")]
        public float surfaceFoamIntensity = 0.5f;

        /// <summary>
        /// Controls the foam amount.
        /// </summary>
        [Tooltip("Controls the foam amount.")]
        public float surfaceFoamAmount = 0.5f;

        /// <summary>
        /// Controls the foam tiling.
        /// </summary>
        [Tooltip("Controls the foam tiling.")]
        public float surfaceFoamTiling = 1.0f;

        /// <summary>
        /// Sets the deep foam amount.
        /// </summary>
        [Tooltip("Sets the deep foam amount.")]
        public float deepFoam = 0.3f;

        /// <summary>
        /// Sets the deep foam color.
        /// </summary>
        [Tooltip("Sets the deep foam color.")]
        public Color deepFoamColor = new Color(1.0f, 1.0f, 1.0f);

        /// <summary>
        /// Set the texture used to attenuate or supress the deep (red channel) or surface (green channel) foam.
        /// </summary>
        [Tooltip("Set the texture used to attenuate or supress the deep (red channel) or surface (green channel) foam.")]
        public Texture2D foamMask = null;

        /// <summary>
        /// Set the extent of the foam mask in meters.
        /// </summary>
        [Tooltip("Set the extent of the foam mask in meters.")]
        public Vector2 foamMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Set the offset of the foam mask in meters.
        /// </summary>
        [Tooltip("Set the offset of the foam mask in meters.")]
        public Vector2 foamMaskOffset = new Vector2(0.0f, 0.0f);
        #endregion

        #region Water Masking
        /// <summary>
        /// Set the texture used to attenuate or supress the lower (red channel) or higher (green channel) water frequencies.
        /// </summary>
        [Tooltip("Set the texture used to attenuate or supress the lower (red channel) or higher (green channel) water frequencies.")]
        public Texture2D waterMask = null;

        /// <summary>
        /// Set the extent of the water mask in meters.
        /// </summary>
        [Tooltip("Set the extent of the water mask in meters.")]
        public Vector2 waterMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Set the offset of the water mask in meters.
        /// </summary>
        [Tooltip("Set the offset of the water mask in meters.")]
        public Vector2 waterMaskOffset = new Vector2(0.0f, 0.0f);
        #endregion

        #region Water Masking
        /// <summary>
        /// Set the wind orientation in degrees.
        /// </summary>
        [Tooltip("Set the wind orientation in degrees.")]
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

        // Internal simulation data
        internal WaterSimulationResources simulation = null;

        internal bool CheckResources(CommandBuffer cmd, int bandResolution, int bandCount, ref bool initialAllocation)
        {
            bool needUpdate = false;
            // If the resources have not been allocated for this water surface, allocate them
            if (simulation == null)
            {
                simulation = new WaterSimulationResources();
                simulation.AllocateSmmulationResources(bandResolution, bandCount);
                needUpdate = true;
                initialAllocation = true;
            }
            else if (!simulation.ValidResources(bandResolution, bandCount))
            {
                simulation.ReleaseSmmulationResources();
                simulation.AllocateSmmulationResources(bandResolution, bandCount);
                needUpdate = true;
                initialAllocation = true;
            }

            if (simulation.windSpeed != windSpeed
                || simulation.windOrientation != windOrientation
                || simulation.windAffectCurrent != windAffectCurrent
                || simulation.patchSizes.x != waterMaxPatchSize)
            {
                needUpdate = true;
                initialAllocation = false;
            }

            // The simulation data are not valid, we need to re-evaluate the spectrum
            if (needUpdate)
            {
                UpdateSimulationData();
            }

            return !needUpdate;
        }

        void UpdateSimulationData()
        {
            simulation.windSpeed = windSpeed;
            simulation.windOrientation = windOrientation;
            simulation.windAffectCurrent = windAffectCurrent;
            simulation.patchSizes = HDRenderPipeline.ComputeBandPatchSizes(waterMaxPatchSize);
            simulation.patchWindSpeed = HDRenderPipeline.ComputeWindSpeeds(simulation.windSpeed, simulation.patchSizes);
        }

        void OnDestroy()
        {
            // Make sure to release the resources if they have been created (before HDRP destroys them)
            if (simulation.AllocatedTextures())
                simulation.ReleaseSmmulationResources();
        }
    }
}

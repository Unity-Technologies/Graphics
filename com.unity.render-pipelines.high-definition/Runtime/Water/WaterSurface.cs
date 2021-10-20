using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public class WaterSurface : MonoBehaviour
    {
        public enum WaterGeometryType
        {
            Quad,
            Custom
        }

        // Geometry parameters
        public bool infinite = true;
        public WaterGeometryType geometryType = WaterGeometryType.Quad;
        public Mesh geometry = null;

        // Simulation parameters
        public bool highBandCound = true;
        public float waterMaxPatchSize = 500.0f;
        public Vector4 waveAmplitude = Vector4.one;
        public float choppiness = 1.0f;
        public float timeMultiplier = 1.0f;

        // Rendering parameters
        public Material material = null;
        public float waterSmoothness = 0.8f;

        // Refraction parameters
        public Color transparentColor = new Color(0.00f, 0.45f, 0.65f);
        public float maxAbsorptionDistance = 5.0f;
        public float maxRefractionDistance = 1.0f;

        // Scattering parameters
        public Color scatteringColor = new Color(0.0f, 0.12f, 0.25f);
        public float scatteringFactor = 0.5f;
        public float heightScattering = 0.5f;
        public float displacementScattering = 0.5f;
        public float directLightTipScattering = 0.5f;
        public float directLightBodyScattering = 0.5f;

        // Caustic parameters
        public float causticsIntensity = 0.5f;
        public float causticsTiling = 1.5f;
        public float causticsSpeed = 0.0f;
        public float causticsPlaneOffset = 0.0f;

        // Foam parameters
        public float surfaceFoamSmoothness = 0.2f;
        public float surfaceFoamIntensity = 0.5f;
        public float surfaceFoamAmount = 0.5f;
        public float surfaceFoamTiling = 1.0f;
        public float deepFoam = 0.3f;
        public Color deepFoamColor = new Color(1.0f, 1.0f, 1.0f);
        public Texture2D foamMask = null;
        public Vector2 foamMaskExtent = new Vector2(100.0f, 100.0f);
        public Vector2 foamMaskOffset = new Vector2(0.0f, 0.0f);

        // Water Masking
        public Texture2D waterMask = null;
        public Vector2 waterMaskExtent = new Vector2(100.0f, 100.0f);
        public Vector2 waterMaskOffset = new Vector2(0.0f, 0.0f);

        // Wind
        public float windOrientation = 0.0f;
        public float windSpeed = 30.0f;
        public float windAffectCurrent = 0.5f;
        public AnimationCurve windFoamCurve = new AnimationCurve(new Keyframe(0f, 0.0f), new Keyframe(0.2f, 0.0f), new Keyframe(0.3f, 1.0f), new Keyframe(1.0f, 1.0f));

        // Internal simulation data
        internal WaterSiumulationResources simulation = null;

        internal bool CheckResources(CommandBuffer cmd, int bandResolution, int bandCount, ref bool initialAllocation)
        {
            // If the resources have not been allocated for this water surface, allocate them
            if (simulation == null)
            {
                simulation = new WaterSiumulationResources();
                simulation.AllocateSmmulationResources(bandResolution, bandCount);
                simulation.m_WindSpeed = windSpeed;
                simulation.m_WindOrientation = windOrientation;
                simulation.m_WindAffectCurrent = windAffectCurrent;
                simulation.m_WaterMaxPatchSize = waterMaxPatchSize;
                initialAllocation = true;
                return false;
            }
            else if (!simulation.ValidResources(bandResolution, bandCount))
            {
                simulation.ReleaseSmmulationResources();
                simulation.AllocateSmmulationResources(bandResolution, bandCount);
                simulation.m_WindSpeed = windSpeed;
                simulation.m_WindOrientation = windOrientation;
                simulation.m_WindAffectCurrent = windAffectCurrent;
                simulation.m_WaterMaxPatchSize = waterMaxPatchSize;
                initialAllocation = true;
                return false;
            }

            if (simulation.m_WindSpeed != windSpeed || simulation.m_WindOrientation != windOrientation || simulation.m_WindAffectCurrent != windAffectCurrent || simulation.m_WaterMaxPatchSize != waterMaxPatchSize)
            {
                simulation.m_WindSpeed = windSpeed;
                simulation.m_WindOrientation = windOrientation;
                simulation.m_WindAffectCurrent = windAffectCurrent;
                simulation.m_WaterMaxPatchSize = waterMaxPatchSize;
                initialAllocation = false;
                return false;
            }
            return true;
        }

        void OnDestroy()
        {
            // Make sure to release the resources if they have been created (before HDRP destroys them)
            if (simulation.AllocatedTextures())
                simulation.ReleaseSmmulationResources();
        }
    }
}

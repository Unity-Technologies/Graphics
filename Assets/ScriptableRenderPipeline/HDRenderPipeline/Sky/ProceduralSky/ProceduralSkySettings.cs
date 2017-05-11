namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class ProceduralSkySettings : SkySettings
    {
        public enum OcclusionDownscale { x1 = 1, x2 = 2, x4 = 4 }
        public enum OcclusionSamples   { x64 = 0, x164 = 1, x244 = 2 }
        public enum ScatterDebugMode   { None, Scattering, Occlusion, OccludedScattering, Rayleigh, Mie, Height }

        [Header("Global Settings")]
        public float    worldMieColorIntensity        = 1f;
        public Gradient worldMieColorRamp             = null;
        public float    worldMieDensity               = 15f;
        public float    worldMieExtinctionFactor      = 0f;
        public float    worldMieNearScatterPush       = 0f;
        public float    worldMiePhaseAnisotropy       = 0.9f;
        public float    worldNormalDistance           = 1000f;
        public float    worldRayleighColorIntensity   = 1f;
        public Gradient worldRayleighColorRamp        = null;
        public float    worldRayleighDensity          = 10f;
        public float    worldRayleighExtinctionFactor = 1.1f;
        public float    worldRayleighIndirectScatter  = 0.33f;
        public float    worldRayleighNearScatterPush  = 0f;

        [Header("Height Settings")]
        public float    heightDistance                = 50f;
        public float    heightExtinctionFactor        = 1.1f;
        public float    heightMieDensity              = 0f;
        public float    heightMieNearScatterPush      = 0f;
        public float    heightNormalDistance          = 1000f;
        public Vector3  heightPlaneShift              = Vector3.zero;
        public Color    heightRayleighColor           = Color.white;
        public float    heightRayleighDensity         = 10f;
        public float    heightRayleighIntensity       = 1f;
        public float    heightRayleighNearScatterPush = 0f;
        public float    heightSeaLevel                = 0f;

        /*
        [Header("Scatter Occlusion")]
        public bool               useOcclusion             = false;
        public bool               occlusionFullSky         = false;
        public bool               occlusionDepthFixup      = true;
        public float              occlusionBias            = 0f;
        public float              occlusionBiasClouds      = 0.3f;
        public float              occlusionBiasIndirect    = 0.6f;
        public float              occlusionBiasSkyMie      = 0.4f;
        public float              occlusionBiasSkyRayleigh = 0.2f;
        public float              occlusionDepthThreshold  = 25f;
        public OcclusionDownscale occlusionDownscale       = OcclusionDownscale.x2;
        public OcclusionSamples   occlusionSamples         = OcclusionSamples.x64;
        */

        [Header("Other")]
        public Cubemap skyHDRI             = null;
        // public Shader atmosphericShader = null;
        // public Shader occlusionShader   = null;
        public float worldScaleExponent    = 1.0f;
        public float maxSkyDistance        = 4000.0f;
        public ScatterDebugMode debugMode  = ScatterDebugMode.None;

        // Camera   m_currentCamera;

        // UnityEngine.Rendering.CommandBuffer m_occlusionCmdAfterShadows, m_occlusionCmdBeforeScreen;

        void Awake()
        {
            if (worldRayleighColorRamp == null)
            {
                worldRayleighColorRamp = new Gradient();
                worldRayleighColorRamp.SetKeys(
                    new[] { new GradientColorKey(new Color(0.3f, 0.4f, 0.6f), 0f),
                            new GradientColorKey(new Color(0.5f, 0.6f, 0.8f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f),
                            new GradientAlphaKey(1f, 1f) }
                    );
            }

            if (worldMieColorRamp == null)
            {
                worldMieColorRamp = new Gradient();
                worldMieColorRamp.SetKeys(
                    new[] { new GradientColorKey(new Color(0.95f, 0.75f, 0.5f), 0f),
                            new GradientColorKey(new Color(1f, 0.9f, 8.0f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f),
                            new GradientAlphaKey(1f, 1f) }
                    );
            }
        }

        public void OnValidate()
        {
            worldMieDensity               = Mathf.Clamp(worldMieDensity, 0f, 1000f);
            worldMiePhaseAnisotropy       = Mathf.Clamp01(worldMiePhaseAnisotropy);
            worldMieNearScatterPush       = Mathf.Clamp(worldMieNearScatterPush, -200f, 300f);
            worldNormalDistance           = Mathf.Clamp(worldNormalDistance, 1f, 10000f);
            worldRayleighDensity          = Mathf.Clamp(worldRayleighDensity, 0, 1000f);
            worldRayleighIndirectScatter  = Mathf.Clamp(worldRayleighIndirectScatter, 0f, 1f);
            worldRayleighNearScatterPush  = Mathf.Clamp(worldRayleighNearScatterPush, -200f, 300f);

            heightMieDensity              = Mathf.Clamp(heightMieDensity, 0, 1000f);
            heightMieNearScatterPush      = Mathf.Clamp(heightMieNearScatterPush, -200f, 300f);
            heightNormalDistance          = Mathf.Clamp(heightNormalDistance, 1f, 10000f);
            heightRayleighDensity         = Mathf.Clamp(heightRayleighDensity, 0, 1000f);
            heightRayleighNearScatterPush = Mathf.Clamp(heightRayleighNearScatterPush, -200f, 300f);

            worldScaleExponent            = Mathf.Clamp(worldScaleExponent, 1f, 2f);
            maxSkyDistance                = Mathf.Clamp(maxSkyDistance, 1.0f, 1000000.0f);

            /*
            occlusionBias                = Mathf.Clamp01(occlusionBias);
            occlusionBiasClouds          = Mathf.Clamp01(occlusionBiasClouds);
            occlusionBiasIndirect        = Mathf.Clamp01(occlusionBiasIndirect);
            occlusionBiasSkyMie          = Mathf.Clamp01(occlusionBiasSkyMie);
            occlusionBiasSkyRayleigh     = Mathf.Clamp01(occlusionBiasSkyRayleigh);
            */
        }

        public override SkyRenderer GetRenderer()
        {
            return new ProceduralSkyRenderer(this);
        }
    }
}

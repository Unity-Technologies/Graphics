using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSurfacePresets
    {
        static internal void ApplyCommonPreset(WaterSurface waterSurface)
        {
            waterSurface.timeMultiplier = 1.0f;
            waterSurface.cpuSimulation = false;
            waterSurface.cpuFullResolution = false;
            waterSurface.cpuEvaluateRipples = false;

            // Simulation
            waterSurface.waterMask = null;
            waterSurface.waterMaskExtent.Set(100.0f, 100.0f);
            waterSurface.waterMaskOffset = Vector2.zero;
            waterSurface.largeBand0Multiplier = 1.0f;
            waterSurface.largeBand1Multiplier = 1.0f;

            // Fade
            waterSurface.largeBand0FadeToggle = true;
            waterSurface.largeBand1FadeToggle = true;
            waterSurface.ripplesFadeToggle = true;

            waterSurface.ripplesFadeStart = 50.0f;
            waterSurface.ripplesFadeDistance = 200.0f;

            // Ripples
            waterSurface.ripples = true;
            waterSurface.ripplesChaos = 0.8f;
            waterSurface.ripplesWindSpeed = 8.0f;

            // Refraction
            waterSurface.refractionColor = new Color(0.1f, 0.5f, 0.5f);
            waterSurface.maxRefractionDistance = 0.5f;
            waterSurface.absorptionDistance = 1.5f;

            // Caustics
            waterSurface.caustics = true;
            waterSurface.causticsBand = 2;
            waterSurface.causticsIntensity = 0.5f;
            waterSurface.causticsResolution = WaterSurface.WaterCausticsResolution.Caustics256;
            waterSurface.virtualPlaneDistance = 4.0f;

            // Foam
            waterSurface.foam = false;
        }

        static internal void ApplyWaterOceanPreset(WaterSurface waterSurface)
        {
            ApplyCommonPreset(waterSurface);

            // Set the various parameters
            waterSurface.surfaceType = WaterSurfaceType.OceanSeaLake;
            waterSurface.geometryType = WaterGeometryType.Infinite;
            waterSurface.geometryType = WaterGeometryType.Infinite;
            waterSurface.cpuSimulation = true;

            // Swell
            waterSurface.repetitionSize = 500.0f;
            waterSurface.largeWindSpeed = 30.0f;
            waterSurface.largeChaos = 0.85f;
            waterSurface.largeCurrentSpeedValue = 0.0f;

            // Fade
            waterSurface.largeBand0FadeStart = 1500.0f;
            waterSurface.largeBand0FadeDistance = 3000.0f;
            waterSurface.largeBand1FadeStart = 300.0f;
            waterSurface.largeBand1FadeDistance = 800.0f;

            // Scattering
            waterSurface.scatteringColor = new Color(0.0f, 0.4f, 0.4f);
            waterSurface.ambientScattering = 0.2f;
            waterSurface.heightScattering = 0.2f;
            waterSurface.displacementScattering = 0.1f;
            waterSurface.directLightTipScattering = 0.6f;
            waterSurface.directLightBodyScattering = 0.5f;

            // Foam
            waterSurface.foam = true;
            waterSurface.foamTextureTiling = 0.15f;
            waterSurface.simulationFoamAmount = 0.2f;
            waterSurface.simulationFoamDrag = 0.0f;
            waterSurface.simulationFoamSmoothness = 1.0f;
            waterSurface.foamTexture = null;
            waterSurface.foamMask = null;

            // Caustics
            waterSurface.caustics = false;
        }

        static internal void ApplyWaterRiverPreset(WaterSurface waterSurface)
        {
            ApplyCommonPreset(waterSurface);

            // Set the various parameters
            waterSurface.surfaceType = WaterSurfaceType.River;
            waterSurface.geometryType = WaterGeometryType.Quad;

            // Agitation
            waterSurface.repetitionSize = 75.0f;
            waterSurface.largeWindSpeed = 17.5f;
            waterSurface.largeChaos = 0.9f;
            waterSurface.largeCurrentSpeedValue = 1.5f;

            // Fade
            waterSurface.largeBand0FadeStart = 150.0f;
            waterSurface.largeBand0FadeDistance = 300.0f;

            // Ripples
            waterSurface.ripplesChaos = 0.2f;

            // Scattering
            waterSurface.scatteringColor = new Color(0.0f, 0.4f, 0.45f);
            waterSurface.ambientScattering = 0.35f;
            waterSurface.heightScattering = 0.2f;
            waterSurface.displacementScattering = 0.1f;
            waterSurface.directLightTipScattering = 0.6f;
            waterSurface.directLightBodyScattering = 0.5f;

            // Caustics
            waterSurface.causticsPlaneBlendDistance = 1.0f;
        }

        static internal void ApplyWaterPoolPreset(WaterSurface waterSurface)
        {
            ApplyCommonPreset(waterSurface);

            // Set the various parameters
            waterSurface.surfaceType = WaterSurfaceType.Pool;
            waterSurface.geometryType = WaterGeometryType.Quad;

            // Make the time multiplier a bit slower
            waterSurface.timeMultiplier = 0.8f;

            // Ripples
            waterSurface.ripplesWindSpeed = 5.0f;
            waterSurface.ripplesChaos = 1.0f;

            // Refraction
            waterSurface.refractionColor = new Color(0.2f, 0.55f, 0.55f);
            waterSurface.maxRefractionDistance = 0.35f;
            waterSurface.absorptionDistance = 5.0f;

            // Scattering
            waterSurface.scatteringColor = new Color(0.0f, 0.5f, 0.6f);
            waterSurface.ambientScattering = 0.6f;
            waterSurface.heightScattering = 0.0f;
            waterSurface.displacementScattering = 0.0f;
            waterSurface.directLightBodyScattering = 0.2f;
            waterSurface.directLightTipScattering = 0.2f;

            // Caustics
            waterSurface.causticsPlaneBlendDistance = 2.0f;

            // Under Water
            waterSurface.underWater = true;
            waterSurface.volumeBounds = waterSurface.GetComponent<BoxCollider>();
            waterSurface.volumePrority = 0;
            waterSurface.transitionSize = 0.2f;
            waterSurface.absorbtionDistanceMultiplier = 1.0f;
        }
    }
}

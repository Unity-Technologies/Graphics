using static UnityEngine.Rendering.HighDefinition.WaterSurface;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSurfacePresets
    {
        static internal void ApplyWaterOceanPreset(WaterSurface waterSurface)
        {
            // Set the various parameters
            waterSurface.surfaceType = WaterSurfaceType.OceanSeaLake;
            waterSurface.geometryType = WaterGeometryType.Infinite;
            waterSurface.geometryType = WaterGeometryType.Infinite;
            waterSurface.cpuSimulation = true;
            waterSurface.waterMask = null;

            // Swell
            waterSurface.repetitionSize = 500.0f;
            waterSurface.largeOrientationValue = 0.0f;
            waterSurface.largeWindSpeed = 30.0f;
            waterSurface.largeChaos = 0.85f;
            waterSurface.largeCurrentSpeedValue = 0.0f;
            waterSurface.largeCurrentMap = null;
            waterSurface.largeCurrentRegionExtent = new Vector2(100.0f, 100.0f);
            waterSurface.largeCurrentRegionOffset = Vector2.zero;
            waterSurface.largeCurrentMapInfluence = 1.0f;
            waterSurface.largeBand0Multiplier = 1.0f;
            waterSurface.largeBand1Multiplier = 1.0f;

            // Fade
            waterSurface.largeBand0FadeMode = FadeMode.Automatic;
            waterSurface.largeBand1FadeMode = FadeMode.Automatic;
            waterSurface.ripplesFadeMode = FadeMode.Automatic;

            waterSurface.largeBand0FadeStart = 1500.0f;
            waterSurface.largeBand0FadeDistance = 3000.0f;
            waterSurface.largeBand1FadeStart = 300.0f;
            waterSurface.largeBand1FadeDistance = 800.0f;
            waterSurface.ripplesFadeStart = 50.0f;
            waterSurface.ripplesFadeDistance = 200.0f;

            // Ripples
            waterSurface.ripples = true;
            waterSurface.ripplesWindSpeed = 8.0f;
            waterSurface.ripplesChaos = 0.8f;
            waterSurface.ripplesCurrentMap = null;
            waterSurface.ripplesCurrentRegionExtent = new Vector2(100.0f, 100.0f);
            waterSurface.ripplesCurrentRegionOffset = Vector2.zero;
            waterSurface.ripplesCurrentMapInfluence = 1.0f;

            // Refraction
            waterSurface.refractionColor = new Color(0.1f, 0.5f, 0.5f).linear;
            waterSurface.maxRefractionDistance = 0.5f;
            waterSurface.absorptionDistance = 1.5f;

            // Scattering
            waterSurface.scatteringColor = new Color(0.0f, 0.4f, 0.4f).linear;
            waterSurface.ambientScattering = 0.2f;
            waterSurface.heightScattering = 0.2f;
            waterSurface.displacementScattering = 0.1f;
            waterSurface.directLightTipScattering = 0.6f;
            waterSurface.directLightBodyScattering = 0.5f;

            waterSurface.foam = true;
            waterSurface.foamResolution = WaterSurface.WaterFoamResolution.Resolution512;
            waterSurface.foamAreaSize.Set(200f, 200f);
            waterSurface.foamAreaOffset.Set(0, 0);
            waterSurface.foamTextureTiling = 0.15f;
            waterSurface.foamSmoothness = 1.0f;
            waterSurface.simulationFoam = true;
            waterSurface.simulationFoamAmount = 0.2f;
            waterSurface.simulationFoamMask = null;
            waterSurface.simulationFoamWindCurve = new AnimationCurve(new Keyframe(0f, 0.0f), new Keyframe(0.2f, 0.0f), new Keyframe(0.3f, 1.0f), new Keyframe(1.0f, 1.0f));

            // Caustics
            waterSurface.caustics = false;
            waterSurface.causticsBand = 2;
        }

        static internal void ApplyWaterRiverPreset(WaterSurface waterSurface)
        {
            // Set the various parameters
            waterSurface.surfaceType = WaterSurfaceType.River;
            waterSurface.geometryType = WaterGeometryType.InstancedQuads;
            waterSurface.cpuSimulation = false;
            waterSurface.timeMultiplier = 1.0f;
            waterSurface.waterMask = null;

            // Agitation
            waterSurface.repetitionSize = 75.0f;
            waterSurface.largeOrientationValue = 0.0f;
            waterSurface.largeWindSpeed = 17.5f;
            waterSurface.largeChaos = 0.9f;
            waterSurface.largeCurrentSpeedValue = 1.5f;
            waterSurface.largeCurrentMap = null;
            waterSurface.largeCurrentRegionExtent = new Vector2(100.0f, 100.0f);
            waterSurface.largeCurrentRegionOffset = Vector2.zero;
            waterSurface.largeCurrentMapInfluence = 1.0f;
            waterSurface.largeBand0Multiplier = 1.0f;
            waterSurface.largeBand1Multiplier = 1.0f;

            // Fade
            waterSurface.largeBand0FadeMode = FadeMode.Automatic;
            waterSurface.ripplesFadeMode = FadeMode.Automatic;

            waterSurface.largeBand0FadeStart = 150.0f;
            waterSurface.largeBand0FadeDistance = 300.0f;
            waterSurface.ripplesFadeStart = 50.0f;
            waterSurface.ripplesFadeDistance = 200.0f;

            // Ripples
            waterSurface.ripples = true;
            waterSurface.ripplesWindSpeed = 8.0f;
            waterSurface.ripplesChaos = 0.2f;
            waterSurface.ripplesCurrentMap = null;
            waterSurface.ripplesCurrentRegionExtent = new Vector2(100.0f, 100.0f);
            waterSurface.ripplesCurrentRegionOffset = Vector2.zero;
            waterSurface.ripplesCurrentMapInfluence = 1.0f;

            // Scattering
            waterSurface.scatteringColor = new Color(0.0f, 0.4f, 0.45f).linear;
            waterSurface.ambientScattering = 0.35f;
            waterSurface.heightScattering = 0.2f;
            waterSurface.displacementScattering = 0.1f;
            waterSurface.directLightTipScattering = 0.6f;
            waterSurface.directLightBodyScattering = 0.5f;

            // Foam
            waterSurface.foam = false;

            // Caustics
            waterSurface.caustics = true;
            waterSurface.causticsIntensity = 0.5f;
            waterSurface.causticsPlaneBlendDistance = 1.0f;
            waterSurface.causticsResolution = WaterSurface.WaterCausticsResolution.Caustics256;
            waterSurface.causticsBand = 1;
            waterSurface.virtualPlaneDistance = 4.0f;
        }

        static internal void ApplyWaterPoolPreset(WaterSurface waterSurface)
        {
            // Set the various parameters
            waterSurface.surfaceType = WaterSurfaceType.Pool;
            waterSurface.geometryType = WaterGeometryType.Quad;
            waterSurface.cpuSimulation = false;

            // Make the time multiplier a bit slower
            waterSurface.timeMultiplier = 0.8f;
            waterSurface.waterMask = null;

            // Fade
            waterSurface.ripplesFadeMode = FadeMode.Automatic;
            waterSurface.ripplesFadeStart = 50.0f;
            waterSurface.ripplesFadeDistance = 200.0f;

            // Ripples
            waterSurface.ripplesWindSpeed = 5.0f;
            waterSurface.ripplesChaos = 1.0f;
            waterSurface.ripplesCurrentMap = null;
            waterSurface.ripplesCurrentRegionExtent = new Vector2(100.0f, 100.0f);
            waterSurface.ripplesCurrentRegionOffset = Vector2.zero;
            waterSurface.ripplesCurrentMapInfluence = 1.0f;

            // Refraction
            waterSurface.refractionColor = new Color(0.2f, 0.55f, 0.55f).linear;
            waterSurface.maxRefractionDistance = 0.35f;
            waterSurface.absorptionDistance = 5.0f;

            // Scattering
            waterSurface.scatteringColor = new Color(0.0f, 0.5f, 0.6f).linear;
            waterSurface.ambientScattering = 0.6f;
            waterSurface.heightScattering = 0.0f;
            waterSurface.displacementScattering = 0.0f;
            waterSurface.directLightBodyScattering = 0.2f;
            waterSurface.directLightTipScattering = 0.2f;

            // Caustics
            waterSurface.caustics = true;
            waterSurface.causticsIntensity = 0.5f;
            waterSurface.causticsPlaneBlendDistance = 2.0f;
            waterSurface.causticsResolution = WaterSurface.WaterCausticsResolution.Caustics256;
            waterSurface.causticsBand = 0;
            waterSurface.virtualPlaneDistance = 4.0f;

            // Foam
            waterSurface.foam = false;

            // Under Water
            waterSurface.underWater = true;
            waterSurface.TryGetComponent<BoxCollider>(out BoxCollider boxCollider);
            waterSurface.volumeBounds = boxCollider;
            waterSurface.volumePrority = 0;
            waterSurface.absorptionDistanceMultiplier = 1.0f;
        }
    }
}

using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedLightingQualitySettings
    {
        public SerializedProperty root;

        // AO
        public SerializedProperty AOStepCount;
        public SerializedProperty AOFullRes;
        public SerializedProperty AOMaximumRadiusPixels;
        public SerializedProperty AODirectionCount;
        public SerializedProperty AOBilateralUpsample;

        // Ray Traced Ambient Occlusion
        public SerializedProperty RTAORayLength;
        public SerializedProperty RTAOSampleCount;
        public SerializedProperty RTAODenoise;
        public SerializedProperty RTAODenoiserRadius;

        // Contact Shadows
        public SerializedProperty ContactShadowSampleCount;

        // SSR
        public SerializedProperty SSRMaxRaySteps;

        // Ray Traced reflections
        public SerializedProperty RTRMinSmoothness;
        public SerializedProperty RTRSmoothnessFadeStart;
        public SerializedProperty RTRRayLength;
        public SerializedProperty RTRFullResolution;
        public SerializedProperty RTRRayMaxIterations;
        public SerializedProperty RTRDenoise;
        public SerializedProperty RTRDenoiserRadiusDimmer;
        public SerializedProperty RTRDenoiserAntiFlicker;

        // Ray Traced Global Illumination
        public SerializedProperty RTGIRayLength;
        public SerializedProperty RTGIFullResolution;
        public SerializedProperty RTGIRaySteps;
        public SerializedProperty RTGIDenoise;
        public SerializedProperty RTGIHalfResDenoise;
        public SerializedProperty RTGIDenoiserRadius;
        public SerializedProperty RTGISecondDenoise;

        // Screen Space Global Illumination
        public SerializedProperty SSGIRaySteps;
        public SerializedProperty SSGIDenoise;
        public SerializedProperty SSGIHalfResDenoise;
        public SerializedProperty SSGIDenoiserRadius;
        public SerializedProperty SSGISecondDenoise;

        // Fog
        public SerializedProperty VolumetricFogBudget;
        public SerializedProperty VolumetricFogRatio;

        public SerializedLightingQualitySettings(SerializedProperty root)
        {
            this.root = root;

            // AO
            AOStepCount = root.Find((GlobalLightingQualitySettings s) => s.AOStepCount);
            AOFullRes = root.Find((GlobalLightingQualitySettings s) => s.AOFullRes);
            AOMaximumRadiusPixels = root.Find((GlobalLightingQualitySettings s) => s.AOMaximumRadiusPixels);
            AODirectionCount = root.Find((GlobalLightingQualitySettings s) => s.AODirectionCount);
            AOBilateralUpsample = root.Find((GlobalLightingQualitySettings s) => s.AOBilateralUpsample);

            // RTAO
            RTAORayLength = root.Find((GlobalLightingQualitySettings s) => s.RTAORayLength);
            RTAOSampleCount = root.Find((GlobalLightingQualitySettings s) => s.RTAOSampleCount);
            RTAODenoise = root.Find((GlobalLightingQualitySettings s) => s.RTAODenoise);
            RTAODenoiserRadius = root.Find((GlobalLightingQualitySettings s) => s.RTAODenoiserRadius);

            // Contact Shadows
            ContactShadowSampleCount = root.Find((GlobalLightingQualitySettings s) => s.ContactShadowSampleCount);

            // SSR
            SSRMaxRaySteps = root.Find((GlobalLightingQualitySettings s) => s.SSRMaxRaySteps);

            // Ray Traced reflections
            RTRMinSmoothness = root.Find((GlobalLightingQualitySettings s) => s.RTRMinSmoothness);
            RTRSmoothnessFadeStart = root.Find((GlobalLightingQualitySettings s) => s.RTRSmoothnessFadeStart);
            RTRRayLength = root.Find((GlobalLightingQualitySettings s) => s.RTRRayLength);
            RTRFullResolution = root.Find((GlobalLightingQualitySettings s) => s.RTRFullResolution);
            RTRRayMaxIterations = root.Find((GlobalLightingQualitySettings s) => s.RTRRayMaxIterations);
            RTRDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTRDenoise);
            RTRDenoiserRadiusDimmer = root.Find((GlobalLightingQualitySettings s) => s.RTRDenoiserRadiusDimmer);
            RTRDenoiserAntiFlicker = root.Find((GlobalLightingQualitySettings s) => s.RTRDenoiserAntiFlicker);

            // Ray Traced Global Illumination
            RTGIRayLength = root.Find((GlobalLightingQualitySettings s) => s.RTGIRayLength);
            RTGIFullResolution = root.Find((GlobalLightingQualitySettings s) => s.RTGIFullResolution);
            RTGIRaySteps = root.Find((GlobalLightingQualitySettings s) => s.RTGIRaySteps);
            RTGIDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTGIDenoise);
            RTGIHalfResDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTGIHalfResDenoise);
            RTGIDenoiserRadius = root.Find((GlobalLightingQualitySettings s) => s.RTGIDenoiserRadius);
            RTGISecondDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTGISecondDenoise);

            // Screen Space Global Illumination
            SSGIRaySteps = root.Find((GlobalLightingQualitySettings s) => s.SSGIRaySteps);
            SSGIDenoise = root.Find((GlobalLightingQualitySettings s) => s.SSGIDenoise);
            SSGIHalfResDenoise = root.Find((GlobalLightingQualitySettings s) => s.SSGIHalfResDenoise);
            SSGIDenoiserRadius = root.Find((GlobalLightingQualitySettings s) => s.SSGIDenoiserRadius);
            SSGISecondDenoise = root.Find((GlobalLightingQualitySettings s) => s.SSGISecondDenoise);

            // Fog
            VolumetricFogBudget = root.Find((GlobalLightingQualitySettings s) => s.Fog_Budget);
            VolumetricFogRatio = root.Find((GlobalLightingQualitySettings s) => s.Fog_DepthRatio);
        }
    }
}

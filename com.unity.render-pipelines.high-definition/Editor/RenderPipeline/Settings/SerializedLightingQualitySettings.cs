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
        public SerializedProperty RTRClampValue;
        public SerializedProperty RTRFullResolution;
        public SerializedProperty RTRDenoise;
        public SerializedProperty RTRDenoiserRadius;
        public SerializedProperty RTRSmoothDenoising;

        // Ray Traced Global Illumination
        public SerializedProperty RTGIRayLength;
        public SerializedProperty RTGIFullResolution;
        public SerializedProperty RTGIClampValue;
        public SerializedProperty RTGIUpScaleRadius;
        public SerializedProperty RTGIDenoise;
        public SerializedProperty RTGIHalfResDenoise;
        public SerializedProperty RTGIDenoiserRadius;
        public SerializedProperty RTGISecondDenoise;

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
            RTRClampValue = root.Find((GlobalLightingQualitySettings s) => s.RTRClampValue);
            RTRFullResolution = root.Find((GlobalLightingQualitySettings s) => s.RTRFullResolution);
            RTRDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTRDenoise);
            RTRDenoiserRadius = root.Find((GlobalLightingQualitySettings s) => s.RTRDenoiserRadius);
            RTRSmoothDenoising = root.Find((GlobalLightingQualitySettings s) => s.RTRSmoothDenoising);

            // Ray Traced Global Illumination
            RTGIRayLength = root.Find((GlobalLightingQualitySettings s) => s.RTGIRayLength);
            RTGIFullResolution = root.Find((GlobalLightingQualitySettings s) => s.RTGIFullResolution);
            RTGIClampValue = root.Find((GlobalLightingQualitySettings s) => s.RTGIClampValue);
            RTGIUpScaleRadius = root.Find((GlobalLightingQualitySettings s) => s.RTGIUpScaleRadius);
            RTGIDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTGIDenoise);
            RTGIHalfResDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTGIHalfResDenoise);
            RTGIDenoiserRadius = root.Find((GlobalLightingQualitySettings s) => s.RTGIDenoiserRadius);
            RTGISecondDenoise = root.Find((GlobalLightingQualitySettings s) => s.RTGISecondDenoise);

            // Fog
            VolumetricFogBudget = root.Find((GlobalLightingQualitySettings s) => s.Fog_Budget);
            VolumetricFogRatio = root.Find((GlobalLightingQualitySettings s) => s.Fog_DepthRatio);
        }
    }
}

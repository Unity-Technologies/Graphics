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

        // Contact Shadows
        public SerializedProperty ContactShadowSampleCount;

        // SSR
        public SerializedProperty SSRMaxRaySteps;

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

            // Contact Shadows
            ContactShadowSampleCount = root.Find((GlobalLightingQualitySettings s) => s.ContactShadowSampleCount);

            // SSR
            SSRMaxRaySteps = root.Find((GlobalLightingQualitySettings s) => s.SSRMaxRaySteps);

            // Fog
            VolumetricFogBudget = root.Find((GlobalLightingQualitySettings s) => s.Fog_Budget);
            VolumetricFogRatio = root.Find((GlobalLightingQualitySettings s) => s.Fog_DepthRatio);
        }
    }
}

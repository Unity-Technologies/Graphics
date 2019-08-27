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

        // Contact Shadows
        public SerializedProperty ContactShadowSampleCount;

        public SerializedLightingQualitySettings(SerializedProperty root)
        {
            this.root = root;

            // AO
            AOStepCount = root.Find((GlobalLightingQualitySettings s) => s.AOStepCount);
            AOFullRes = root.Find((GlobalLightingQualitySettings s) => s.AOFullRes);
            AOMaximumRadiusPixels = root.Find((GlobalLightingQualitySettings s) => s.AOMaximumRadiusPixels);

            // Contact Shadows
            ContactShadowSampleCount = root.Find((GlobalLightingQualitySettings s) => s.ContactShadowSampleCount);
        }
    }
}

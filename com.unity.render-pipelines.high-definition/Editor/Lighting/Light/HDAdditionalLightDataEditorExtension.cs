using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    // Editor only functions for HDAdditoonalLightData User API
    public static class HDAdditionalLightDataEditorExtension
    {
        /// <summary>
        /// Toggle the usage of color temperature.
        /// </summary>
        /// <param name="hdLight"></param>
        /// <param name="enable"></param>
        public static void EnableColorTemperature(this HDAdditionalLightData hdLight, bool enable)
        {
            hdLight.useColorTemperature = enable;
        }

        /// <summary>
        /// Set Lightmap Bake Type.
        /// </summary>
        /// <param name="hdLight"></param>
        /// <param name="lightmapBakeType"></param>
        /// <returns></returns>
        public static UnityEngine.LightmapBakeType SetLightmapBakeType(this HDAdditionalLightData hdLight, UnityEngine.LightmapBakeType lightmapBakeType) => hdLight.legacyLight.lightmapBakeType = lightmapBakeType;
    }
}

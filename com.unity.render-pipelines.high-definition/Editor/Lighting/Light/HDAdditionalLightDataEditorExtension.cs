using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Extension class that contains all the Editor Only functions available for the HDAdditionalLightData component
    /// </summary>
    public static class HDAdditionalLightDataEditorExtension
    {
        /// <summary>
        /// Set Lightmap Bake Type.
        /// </summary>
        /// <param name="hdLight"></param>
        /// <param name="lightmapBakeType"></param>
        /// <returns></returns>
        public static LightmapBakeType SetLightmapBakeType(this HDAdditionalLightData hdLight, LightmapBakeType lightmapBakeType) => hdLight.legacyLight.lightmapBakeType = lightmapBakeType;

        /// <summary>
        /// Get the display emissive mesh value
        /// </summary>
        public static bool GetDisplayAreaLightEmissiveMesh(this HDAdditionalLightData hdLight) => hdLight.displayAreaLightEmissiveMesh;

        /// <summary>
        /// Displays or hide an emissive mesh for the area light
        /// </summary>
        /// <param name="hdLight"></param>
        /// <param name="display"></param>
        public static void SetDisplayAreaLightEmissiveMesh(this HDAdditionalLightData hdLight, bool display)
        {
            if (hdLight.displayAreaLightEmissiveMesh == display)
                return;

            if (display)
            {
                // fix the local scale to match the emissive quad size
                hdLight.transform.localScale = new Vector3(hdLight.shapeWidth, hdLight.shapeHeight, HDAdditionalLightData.k_MinAreaWidth);
            }

            hdLight.displayAreaLightEmissiveMesh = display;
        }
    }
}

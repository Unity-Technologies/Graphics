using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Class to describe the HDRP specific function
    /// </summary>
    [InitializeOnLoad]
    public static class HDIESImporter
    {
        /// <summary>
        /// Constructor of HD IES Importer
        /// </summary>
        /// <returns>The title of the Preview</returns>
        static HDIESImporter()
        {
            UnityEditor.Rendering.IESImporter.createRenderPipelinePrefabLight += CreateRenderPipelinePrefabLight;
        }

        /// <summary>
        /// Describe how to create an Prefab for the current SRP, have to be reimplemented for each SRP.
        /// </summary>
        /// <param name="ctx">Context used from the asset importer</param>
        /// <param name="iesFileName">Filename of the current IES file</param>
        /// <param name="useIESMaximumIntensity">True if uses the internal Intensity from the file</param>
        /// <param name="iesMaximumIntensityUnit">The string of the units described by the intensity</param>
        /// <param name="iesMaximumIntensity">Intensity</param>
        /// <param name="light">Light used for the prefab</param>
        /// <param name="ies">Texture used for the prefab</param>
        static public void CreateRenderPipelinePrefabLight(AssetImportContext ctx, string iesFileName, bool useIESMaximumIntensity, string iesMaximumIntensityUnit, float iesMaximumIntensity, Light light, Texture ies)
        {
            LightType lightType = (light.type == LightType.Point) ? LightType.Point : LightType.Spot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, lightType);
            Light coreLight = hdLight.GetComponent<Light>();

            if (useIESMaximumIntensity)
            {
                LightUnit lightUnit = (iesMaximumIntensityUnit == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                coreLight.intensity = LightUnitUtils.ConvertIntensity(coreLight, iesMaximumIntensity, lightUnit, LightUnit.Candela);
                if (light.type == LightType.Point)
                    hdLight.IESPoint = ies;
                else
                    hdLight.IESSpot = ies;
            }

            // The light object will be automatically converted into a prefab.
            ctx.AddObjectToAsset(iesFileName + "-HDRP", light.gameObject);
        }
    }
}

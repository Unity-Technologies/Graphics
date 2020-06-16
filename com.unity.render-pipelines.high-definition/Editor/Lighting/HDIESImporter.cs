using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Class to describe an IES file
    /// </summary>
    [InitializeOnLoad]
    public static class HDIESImporter
    {
        static HDIESImporter()
        {
            UnityEditor.Rendering.IESImporter.setupRenderPipelinePrefabLight += SetupRenderPipelinePrefabLight;
        }

        static public void SetupRenderPipelinePrefabLight(bool useIESMaximumIntensity, string iesMaximumIntensityUnit, float iesMaximumIntensity, Light light, Texture ies)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            if (useIESMaximumIntensity)
            {
                LightUnit lightUnit = (iesMaximumIntensityUnit == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                hdLight.SetIntensity(iesMaximumIntensity, lightUnit);
                if (light.type == LightType.Point)
                    hdLight.IESPoint = ies;
                else
                    hdLight.IESSpot = ies;
            }
        }
    }
}

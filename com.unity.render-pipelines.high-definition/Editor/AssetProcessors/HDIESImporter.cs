using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Rendering.HighDefinition
{
    [ScriptedImporter(1, "ies")]
    public class HDIESImporter : UnityEditor.Rendering.IESImporter
    {
        public override void SetupIesEngineForRenderPipeline(IESEngine engine)
        {
            engine.TextureGenerationType = TextureImporterType.Default;
        }

        public override void SetupRenderPipelinePrefabLight(IESEngine engine, Light light)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            if (UseIesMaximumIntensity)
            {
                LightUnit lightUnit = (IesMaximumIntensityUnit == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                hdLight.SetIntensity(IesMaximumIntensity, lightUnit);
            }
        }
    }
}

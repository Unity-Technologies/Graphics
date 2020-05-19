using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Rendering.HighDefinition
{
    [ScriptedImporter(1, "ies")]
    public partial class HDIESImporter : ScriptedImporter
    {
        public UnityEditor.Rendering.IESImporter commonIESImporter = new UnityEditor.Rendering.IESImporter();

        internal void SetupRenderPipelinePrefabLight(IESEngine engine, Light light, Texture ies)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            if (commonIESImporter.iesMetaData.UseIESMaximumIntensity)
            {
                LightUnit lightUnit = (commonIESImporter.iesMetaData.IESMaximumIntensityUnit == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                hdLight.SetIntensity(commonIESImporter.iesMetaData.IESMaximumIntensity, lightUnit);
                if (light.type == LightType.Point)
                    hdLight.IESPoint = ies;
                else
                    hdLight.IESSpot = ies;
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            commonIESImporter.engine.TextureGenerationType = TextureImporterType.Default;

            commonIESImporter.CommonOnImportAsset(ctx,
                delegate (IESEngine engine, Light light, Texture ies)
                {
                    SetupRenderPipelinePrefabLight(engine, light, ies);
                } );
        }
    }
}

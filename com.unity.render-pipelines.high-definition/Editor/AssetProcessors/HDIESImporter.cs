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

        internal void SetupRenderPipelinePrefabLight(IESEngine engine, Light light)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            if (commonIESImporter.iesMetaData.UseIESMaximumIntensity)
            {
                LightUnit lightUnit = (commonIESImporter.iesMetaData.IESMaximumIntensityUnit == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                hdLight.SetIntensity(commonIESImporter.iesMetaData.IESMaximumIntensity, lightUnit);
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            commonIESImporter.engine.TextureGenerationType = TextureImporterType.Default;

            commonIESImporter.CommonOnImportAsset(ctx,
                delegate (IESEngine engine, Light light)
                {
                    SetupRenderPipelinePrefabLight(engine, light);
                } );
        }
    }
}

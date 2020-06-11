using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Class to describe an IES file
    /// </summary>
    [ScriptedImporter(1, "ies")]
    public partial class HDIESImporter : ScriptedImporter
    {
        /// <summary>
        /// Data of the IES importer which is common between Core and HDRP
        /// </summary>
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

        /// <summary>
        /// Callback when the Importer is done
        /// </summary>
        /// <param name="ctx">Asset Importer context.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            commonIESImporter.engine.TextureGenerationType = TextureImporterType.Default;

            commonIESImporter.CommonOnImportAsset(ctx,
                delegate (IESEngine engine, Light light, Texture ies)
                {
                    SetupRenderPipelinePrefabLight(engine, light, ies);
                });
        }
    }
}

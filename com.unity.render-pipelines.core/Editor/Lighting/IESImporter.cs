using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Common class use to share code between implementation of IES Importeres
    /// </summary>
    [System.Serializable]
    public class IESImporter
    {
        /// <summary>
        /// IES Engine
        /// </summary>
        public IESEngine engine = new IESEngine();

        /// <summary>
        /// IES Meta data stored in the ies file
        /// </summary>
        public IESMetaData iesMetaData = new IESMetaData();

        /// <summary>
        /// Delegate prototype which will be sent by the pipeline implementation of the IES Importer
        /// </summary>
        public delegate void SetupRenderPipelinePrefabLight(IESEngine engine, Light light, Texture ies);

        /// <summary>
        /// Common method performing the import of the asset
        /// </summary>
        /// <param name="ctx">Asset importer context.</param>
        /// <param name="setupRenderPipelinePrefabLight">Delegate needed to perform operation which are "Render Pipeline specific" here setuping the prefab of light</param>
        public void CommonOnImportAsset(AssetImportContext ctx, SetupRenderPipelinePrefabLight setupRenderPipelinePrefabLight)
        {
            Texture cookieTextureCube = null;
            Texture cookieTexture2D = null;

            string iesFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ctx.assetPath);
            string errorMessage = engine.ReadFile(iesFilePath);

            if (string.IsNullOrEmpty(errorMessage))
            {
                iesMetaData.FileFormatVersion = engine.FileFormatVersion;
                iesMetaData.IESPhotometricType = engine.GetPhotometricType();
                iesMetaData.Manufacturer = engine.GetKeywordValue("MANUFAC");
                iesMetaData.LuminaireCatalogNumber = engine.GetKeywordValue("LUMCAT");
                iesMetaData.LuminaireDescription = engine.GetKeywordValue("LUMINAIRE");
                iesMetaData.LampCatalogNumber = engine.GetKeywordValue("LAMPCAT");
                iesMetaData.LampDescription = engine.GetKeywordValue("LAMP");

                (iesMetaData.IESMaximumIntensity, iesMetaData.IESMaximumIntensityUnit) = engine.GetMaximumIntensity();

                string warningMessage;

                (warningMessage, cookieTextureCube) = engine.GenerateCubeCookie(iesMetaData.CookieCompression, (int)iesMetaData.iesSize);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES Cube texture: {warningMessage}");
                }
                cookieTextureCube.IncrementUpdateCount();

                (warningMessage, cookieTexture2D) = engine.Generate2DCookie(iesMetaData.CookieCompression, iesMetaData.SpotAngle, (int)iesMetaData.iesSize, iesMetaData.ApplyLightAttenuation);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES 2D texture: {warningMessage}");
                }
                cookieTexture2D.IncrementUpdateCount();
            }
            else
            {
                ctx.LogImportError($"Cannot read IES file '{iesFilePath}': {errorMessage}");
            }

            string iesFileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var iesObject = ScriptableObject.CreateInstance<IESObject>();
            iesObject.iesMetaData = iesMetaData;
            var lightObject = new GameObject(iesFileName);

            lightObject.transform.localEulerAngles = new Vector3(90f, 0f, iesMetaData.LightAimAxisRotation);

            Light light = lightObject.AddComponent<Light>();
            light.type = (iesMetaData.PrefabLightType == IESLightType.Point) ? LightType.Point : LightType.Spot;
            light.intensity = 1f;  // would need a better intensity value formula
            light.range = 10f; // would need a better range value formula
            light.spotAngle = iesMetaData.SpotAngle;

            setupRenderPipelinePrefabLight(engine, light, (iesMetaData.PrefabLightType == IESLightType.Point) ? cookieTextureCube : cookieTexture2D);

            ctx.AddObjectToAsset("IES", iesObject);
            ctx.SetMainObject(iesObject);

            // The light object will be automatically converted into a prefab.
            ctx.AddObjectToAsset(iesFileName, lightObject);

            if (cookieTextureCube != null)
            {
                cookieTextureCube.name = iesFileName + "-Cube-IES";
                ctx.AddObjectToAsset(cookieTextureCube.name, cookieTextureCube);
            }
            if (cookieTexture2D != null)
            {
                cookieTexture2D.name = iesFileName + "-2D-IES";
                ctx.AddObjectToAsset(cookieTexture2D.name, cookieTexture2D);
            }
        }
    }
}

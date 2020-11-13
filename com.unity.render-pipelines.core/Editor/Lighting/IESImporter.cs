using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Common class use to share code between implementation of IES Importeres
    /// </summary>
    [System.Serializable]
    [ScriptedImporter(1, "ies")]
    public partial class IESImporter : ScriptedImporter
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
        /// Must be initialized during the creation of the SRP
        /// </summary>
        public static event System.Action<AssetImportContext, string, bool, string, float, Light, Texture> createRenderPipelinePrefabLight;

        /// <summary>
        /// Common method performing the import of the asset
        /// </summary>
        /// <param name="ctx">Asset importer context.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            engine.TextureGenerationType = TextureImporterType.Default;

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
            GameObject lightObject = new GameObject(iesFileName);

            lightObject.transform.localEulerAngles = new Vector3(90f, 0f, iesMetaData.LightAimAxisRotation);

            Light light = lightObject.AddComponent<Light>();
            light.type = (iesMetaData.PrefabLightType == IESLightType.Point) ? LightType.Point : LightType.Spot;
            light.intensity = 1f;  // would need a better intensity value formula
            light.range = 10f; // would need a better range value formula
            light.spotAngle = iesMetaData.SpotAngle;

            ctx.AddObjectToAsset("IES", iesObject);
            ctx.SetMainObject(iesObject);

            IESImporter.createRenderPipelinePrefabLight?.Invoke(ctx, iesFileName, iesMetaData.UseIESMaximumIntensity, iesMetaData.IESMaximumIntensityUnit, iesMetaData.IESMaximumIntensity, light, (iesMetaData.PrefabLightType == IESLightType.Point) ? cookieTextureCube : cookieTexture2D);

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

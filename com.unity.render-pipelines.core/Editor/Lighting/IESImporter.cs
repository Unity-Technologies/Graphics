using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public enum IESLightType
    {
        Point,
        Spot,
    }

    [System.Serializable]
    public class IESMetaData
    {
        public string FileFormatVersion;
        public string IESPhotometricType;
        public float  IESMaximumIntensity;
        public string IESMaximumIntensityUnit;

        // IES luminaire product information.
        public string Manufacturer;           // IES keyword MANUFAC
        public string LuminaireCatalogNumber; // IES keyword LUMCAT
        public string LuminaireDescription;   // IES keyword LUMINAIRE
        public string LampCatalogNumber;      // IES keyword LAMPCAT
        public string LampDescription;        // IES keyword LAMP

        public IESLightType PrefabLightType = IESLightType.Point;

        [Range(1f, 179f)]
        public float SpotAngle = 120f;
        [Range(32, 2048)]
        public int   SpotCookieSize = 128;
        public bool  ApplyLightAttenuation  = true;
        public bool  UseIESMaximumIntensity = true;

        public TextureImporterCompression CookieCompression = TextureImporterCompression.Uncompressed;

        [Range(-180f, 180f)]
        public float LightAimAxisRotation = -90f;
    }

    [System.Serializable]
    public class IESImporter
    {
        public IESEngine    engine      = new IESEngine();
        public IESMetaData  iesMetaData = new IESMetaData();

        public delegate void SetupRenderPipelinePrefabLight(IESEngine engine, Light light, Texture ies);

        public void CommonOnImportAsset(AssetImportContext ctx, SetupRenderPipelinePrefabLight setupRenderPipelinePrefabLight)
        {
            Texture cookieTextureCube   = null;
            Texture cookieTexture2D     = null;
            Texture cylindricalTexture  = null;

            string iesFilePath  = Path.Combine(Path.GetDirectoryName(Application.dataPath), ctx.assetPath);
            string errorMessage = engine.ReadFile(iesFilePath);

            if (string.IsNullOrEmpty(errorMessage))
            {
                iesMetaData.FileFormatVersion      = engine.FileFormatVersion;
                iesMetaData.IESPhotometricType     = engine.GetPhotometricType();
                iesMetaData.Manufacturer           = engine.GetKeywordValue("MANUFAC");
                iesMetaData.LuminaireCatalogNumber = engine.GetKeywordValue("LUMCAT");
                iesMetaData.LuminaireDescription   = engine.GetKeywordValue("LUMINAIRE");
                iesMetaData.LampCatalogNumber      = engine.GetKeywordValue("LAMPCAT");
                iesMetaData.LampDescription        = engine.GetKeywordValue("LAMP");

                (iesMetaData.IESMaximumIntensity, iesMetaData.IESMaximumIntensityUnit) = engine.GetMaximumIntensity();

                string warningMessage;

                (warningMessage, cookieTextureCube) = engine.GenerateCubeCookie(iesMetaData.CookieCompression);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES Cube texture: {warningMessage}");
                }

                (warningMessage, cookieTexture2D) = engine.Generate2DCookie(iesMetaData.CookieCompression, iesMetaData.SpotAngle, iesMetaData.SpotCookieSize, iesMetaData.ApplyLightAttenuation);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES 2D texture: {warningMessage}");
                }

                (warningMessage, cylindricalTexture) = engine.GenerateCylindricalTexture(iesMetaData.CookieCompression);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES latitude-longitude texture: {warningMessage}");
                }
            }
            else
            {
                ctx.LogImportError($"Cannot read IES file '{iesFilePath}': {errorMessage}");
            }

            string iesFileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var lightObject = new GameObject(iesFileName);

            lightObject.transform.localEulerAngles = new Vector3(90f, 0f, iesMetaData.LightAimAxisRotation);

            Light light = lightObject.AddComponent<Light>();
            light.type      = (iesMetaData.PrefabLightType == IESLightType.Point) ? LightType.Point : LightType.Spot;
            light.intensity = 1f;  // would need a better intensity value formula
            light.range     = 10f; // would need a better range value formula
            light.spotAngle = iesMetaData.SpotAngle;
            //light.cookie    = (iesMetaData.PrefabLightType == IESLightType.Point) ? cookieTextureCube : cookieTexture2D;

            setupRenderPipelinePrefabLight(engine, light, (iesMetaData.PrefabLightType == IESLightType.Point) ? cookieTextureCube : cookieTexture2D);

            // The light object will be automatically converted into a prefab.
            ctx.AddObjectToAsset(iesFileName, lightObject);
            ctx.SetMainObject(lightObject);

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
            if (cylindricalTexture != null)
            {
                cylindricalTexture.name = iesFileName + "-Cylindrical-IES";
                ctx.AddObjectToAsset(cylindricalTexture.name, cylindricalTexture);
            }
        }
    }
}

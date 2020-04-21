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

    public abstract class IESImporter : ScriptedImporter
    {
        public string FileFormatVersion;
        public string IesPhotometricType;
        public float  IesMaximumIntensity;
        public string IesMaximumIntensityUnit;

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
        public int   SpotCookieSize = 512;
        public bool  ApplyLightAttenuation  = true;
        public bool  UseIesMaximumIntensity = true;

        public TextureImporterCompression CookieCompression = TextureImporterCompression.Uncompressed;

        [Range(-180f, 180f)]
        public float LightAimAxisRotation = -90f;

        public abstract void SetupIesEngineForRenderPipeline(IESEngine engine);
        public abstract void SetupRenderPipelinePrefabLight(IESEngine engine, Light light);

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var engine = new IESEngine();

            SetupIesEngineForRenderPipeline(engine);

            Texture cookieTextureCube   = null;
            Texture cookieTexture2D     = null;
            Texture cylindricalTexture  = null;

            string iesFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ctx.assetPath);

            string errorMessage = engine.ReadFile(iesFilePath);

            if (string.IsNullOrEmpty(errorMessage))
            {
                FileFormatVersion      = engine.FileFormatVersion;
                IesPhotometricType     = engine.GetPhotometricType();
                Manufacturer           = engine.GetKeywordValue("MANUFAC");
                LuminaireCatalogNumber = engine.GetKeywordValue("LUMCAT");
                LuminaireDescription   = engine.GetKeywordValue("LUMINAIRE");
                LampCatalogNumber      = engine.GetKeywordValue("LAMPCAT");
                LampDescription        = engine.GetKeywordValue("LAMP");

                (IesMaximumIntensity, IesMaximumIntensityUnit) = engine.GetMaximumIntensity();

                string warningMessage;


                (warningMessage, cookieTextureCube) = engine.GenerateCubeCookie(CookieCompression);

                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES Cube texture: {warningMessage}");
                }

                (warningMessage, cookieTexture2D) = engine.Generate2DCookie(CookieCompression, SpotAngle, SpotCookieSize, ApplyLightAttenuation);

                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES 2D texture: {warningMessage}");
                }

                (warningMessage, cylindricalTexture) = engine.GenerateCylindricalTexture(CookieCompression);

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

            lightObject.transform.localEulerAngles = new Vector3(90f, 0f, LightAimAxisRotation);

            Light light = lightObject.AddComponent<Light>();
            light.type      = (PrefabLightType == IESLightType.Point) ? LightType.Point : LightType.Spot;
            light.intensity = 1f;  // would need a better intensity value formula
            light.range     = 10f; // would need a better range value formula
            light.spotAngle = SpotAngle;
            light.cookie    = (PrefabLightType == IESLightType.Point) ? cookieTextureCube : cookieTexture2D;

            SetupRenderPipelinePrefabLight(engine, light);

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

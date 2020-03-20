using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

//#if HDRP_7_1_6_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
//#endif

namespace UnityEditor.Rendering.HighDefinition
{
    [ScriptedImporter(1, "ies")]
    public class IesImporter : ScriptedImporter
    {
        // IES luminaire product information.
        public string Manufacturer;           // IES keyword MANUFAC
        public string LuminaireCatalogNumber; // IES keyword LUMCAT
        public string LuminaireDescription;   // IES keyword LUMINAIRE
        public string LampCatalogNumber;      // IES keyword LAMPCAT
        public string LampDescription;        // IES keyword LAMP

        public Texture CookieTexture = null;
        public Texture CylindricalTexture = null;
        public float ProfileRotationInY;

        readonly bool k_UsingHdrp = RenderPipelineManager.currentPipeline?.ToString() == "UnityEngine.Rendering.HighDefinition.HDRenderPipeline";

        const int k_CylindricalTextureHeight = 256;                            // for 180 latitudinal degrees
        const int k_CylindricalTextureWidth = 2 * k_CylindricalTextureHeight; // for 360 longitudinal degrees

        public override void OnImportAsset(AssetImportContext ctx)
        {
            //string iesFilePath = Application.dataPath;
            //foreach (var selectionObject in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            //{
            //    iesFilePath = Path.Combine(Path.GetDirectoryName(iesFilePath), AssetDatabase.GetAssetPath(selectionObject));
            //    if (File.Exists(iesFilePath))
            //    {
            //        iesFilePath = Path.GetDirectoryName(iesFilePath);
            //    }
            //    break;
            //}
            //iesFilePath = Path.Combine(iesFilePath, Path.GetFileName(ctx.assetPath));

            string iesFilePath = ctx.assetPath;

            var engine = new IesEngine();

            string errorMessage = engine.ReadFile(iesFilePath);

            if (string.IsNullOrEmpty(errorMessage))
            {
                Manufacturer = engine.GetKeywordValue("MANUFAC");
                LuminaireCatalogNumber = engine.GetKeywordValue("LUMCAT");
                LuminaireDescription = engine.GetKeywordValue("LUMINAIRE");
                LampCatalogNumber = engine.GetKeywordValue("LAMPCAT");
                LampDescription = engine.GetKeywordValue("LAMP");

                CookieTexture = GenerateTexture(ctx, engine, true, engine.GetTextureSize());
                CylindricalTexture = GenerateTexture(ctx, engine, false, (k_CylindricalTextureHeight, k_CylindricalTextureWidth));
            }
            else
            {
                ctx.LogImportError($"Cannot read IES file: {iesFilePath}\n{errorMessage}");
            }

            string iesFileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var lightObject = new GameObject(iesFileName);

            //if (k_UsingHdrp)
            {
//#if HDRP_7_1_6_OR_NEWER
                HDAdditionalLightData hdLight = lightObject.AddHDLight(HDLightTypeAndShape.Point);
                if (engine.TotalLumens == -1f) // absolute photometry
                {
                    hdLight.SetIntensity(engine.MaxCandelas, LightUnit.Candela);
                }
                else
                {
                    hdLight.SetIntensity(engine.TotalLumens, LightUnit.Lumen);
                }
                hdLight.SetRange(100f); // would need a better range value formula
                hdLight.SetCookie(CookieTexture);
//#else
//                ctx.LogImportError("IES Importer needs HDRP 7.1.6 or newer.");
//#endif
            }
            //else
            //{
            //    Light light = lightObject.AddComponent<Light>();
            //    light.type = LightType.Point;
            //    light.intensity = 1f;   // would need a better intensity value formula
            //    light.range = 100f; // would need a  better range value formula
            //    light.cookie = CookieTexture;
            //}

            // The light object will be automatically converted into a prefab.
            ctx.AddObjectToAsset(iesFileName, lightObject);
            ctx.SetMainObject(lightObject);

            if (CookieTexture != null)
            {
                CookieTexture.name = iesFileName + "-Cookie";
                ctx.AddObjectToAsset(CookieTexture.name, CookieTexture);
            }

            if (CylindricalTexture != null)
            {
                CylindricalTexture.name = iesFileName + "-Cylindrical";
                ctx.AddObjectToAsset(CylindricalTexture.name, CylindricalTexture);

                // string filePath = Path.Combine(Path.GetDirectoryName(iesFilePath), CylindricalTexture.name + ".png");
                // byte[] bytes    = ((Texture2D)CylindricalTexture).EncodeToPNG();
                // File.WriteAllBytes(filePath, bytes);
            }
        }

        Texture GenerateTexture(AssetImportContext ctx, IesEngine engine, bool generateCookie, (int height, int width) size)
        {
            // Default values set by the TextureGenerationSettings constructor can be found in this file on GitHub:
            // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/AssetPipeline/TextureGenerator.bindings.cs

            var settings = new TextureGenerationSettings((generateCookie && !k_UsingHdrp) ? TextureImporterType.Cookie : TextureImporterType.Default);

            SourceTextureInformation textureInfo = settings.sourceTextureInformation;
            textureInfo.containsAlpha = true;
            textureInfo.height = size.height;
            textureInfo.width = size.width;

            TextureImporterSettings textureImporterSettings = settings.textureImporterSettings;
            textureImporterSettings.alphaSource = k_UsingHdrp ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            textureImporterSettings.aniso = 0;
            textureImporterSettings.borderMipmap = textureImporterSettings.textureType == TextureImporterType.Cookie;
            textureImporterSettings.filterMode = FilterMode.Bilinear;
            textureImporterSettings.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            textureImporterSettings.mipmapEnabled = false;
            textureImporterSettings.readable = true;
            textureImporterSettings.sRGBTexture = false;
            textureImporterSettings.textureShape = generateCookie ? TextureImporterShape.TextureCube : TextureImporterShape.Texture2D;
            textureImporterSettings.wrapMode = textureImporterSettings.wrapModeU = textureImporterSettings.wrapModeV = textureImporterSettings.wrapModeW = TextureWrapMode.Clamp;

            TextureImporterPlatformSettings platformSettings = settings.platformSettings;
            platformSettings.maxTextureSize = 2048;
            platformSettings.resizeAlgorithm = TextureResizeAlgorithm.Bilinear;
            platformSettings.textureCompression = generateCookie ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;

            NativeArray<Color32> colorBuffer = engine.BuildTextureBuffer(size);

            TextureGenerationOutput output = TextureGenerator.GenerateTexture(settings, colorBuffer);

            if (output.importWarnings.Length > 0)
            {
                ctx.LogImportError("IES Texture Generation:\n" + string.Join("\n", output.importWarnings));
            }

            if (!string.IsNullOrEmpty(output.importInspectorWarnings))
            {
                ctx.LogImportError("IES Texture Generation:\n" + output.importInspectorWarnings);
            }

            return output.texture;
        }
    }
}

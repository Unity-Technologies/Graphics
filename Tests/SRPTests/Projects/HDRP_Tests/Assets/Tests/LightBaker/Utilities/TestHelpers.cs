using NUnit.Framework;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.LightBaking.Tests.Helpers
{
    internal static class Lightmaps
    {
        public static Texture2D LoadImageAndConvertToRGBAHalf(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            Texture2D reference = new(1, 1, TextureFormat.RGBAHalf, false);

            {
                Texture2D referenceFloat32 = new(1, 1);
                byte[] bytes = File.ReadAllBytes(path);
                if (!ImageConversion.LoadImage(referenceFloat32, bytes))
                    return null;
                if (!reference.Reinitialize(referenceFloat32.width, referenceFloat32.height))
                    return null;
                reference.SetPixels(referenceFloat32.GetPixels());
            }

            return reference;
        }

        public static void WriteActualImageToFile()
        {
            // Look for newly dropped actual images and attempt to reimport them with the correct settings
            string actualImagePath = $"{FileUtil.ActualImagePath}.exr";

            if (File.Exists(actualImagePath))
            {
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(actualImagePath);

                // If it was never imported before do an initial import to register it with the database
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(actualImagePath);
                }

                // Import the asset with suitable settings
                ImageHandler.TextureImporterSettings importSettings = new()
                {
                    IsReadable = true,
                    UseMipMaps = false,
                    NPOTScale = TextureImporterNPOTScale.None,
                    TextureCompressionType = TextureImporterCompression.Uncompressed,
                    TextureFilterMode = FilterMode.Point
                };

                ImageHandler.ReImportTextureWithSettings(actualImagePath, importSettings);
            }
        }
    }
}

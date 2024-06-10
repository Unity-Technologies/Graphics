using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using static UnityEditor.LightBaking.InputExtraction;
using static UnityEditor.LightBaking.Tests.Helpers.Lightmaps;

namespace UnityEditor.LightBaking.Tests
{
    public enum MetaPassType
    {
        Albedo = 0,
        Emission = 1
    };

    public class MetaPassSceneTest
    {
        public string SceneName { get; set; }
        public string ObjectName { get; set; } // Object for which to test the metaPass data
        public MetaPassType MetaPassType { get; set; } = MetaPassType.Albedo;
        public int TextureCount { get; set; }
        public override string ToString() { return $"{SceneName}-{ObjectName}-{MetaPassType}"; }
    };

    internal class InputExtractionMaterials
    {
        private static readonly float metaPassRMSETolerance = 0.0001f;

        Texture2D ConvertVector4ImageToRGBAHalfTexture(uint width, uint height, Vector4[] textureData)
        {
            Texture2D texture = new((int)width, (int)height, TextureFormat.RGBAHalf, false);
            var colors = new Color[width * height];
            for (uint i = 0; i < width * height; ++i)
            {
                colors[i] = textureData[i];
            }
            texture.SetPixels(colors);
            return texture;
        }

        private static readonly MetaPassSceneTest[] metaPassTests = {
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "default", MetaPassType = MetaPassType.Albedo, TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "default", MetaPassType = MetaPassType.Emission, TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "emissive", MetaPassType = MetaPassType.Emission, TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "albedo", MetaPassType = MetaPassType.Albedo, TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "emissiveTex", MetaPassType = MetaPassType.Emission, TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "albedoTex", MetaPassType = MetaPassType.Albedo , TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "emissiveTexTiling23", MetaPassType = MetaPassType.Emission , TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "albedoTexTiling23", MetaPassType = MetaPassType.Albedo , TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "emissiveTexOffset_p5p3", MetaPassType = MetaPassType.Emission , TextureCount = 9 },
            new() { SceneName = "Metapass_BasicMaterials", ObjectName = "albedoTexOffset_p5p3", MetaPassType = MetaPassType.Albedo , TextureCount = 9 },
            new() { SceneName = "BakedLightingTerrainAlbedo-editor", ObjectName = "Terrain", MetaPassType = MetaPassType.Albedo , TextureCount = 3 },
            new() { SceneName = "BakedLightingTerrainAlbedo-editor", ObjectName = "TerrainOffset", MetaPassType = MetaPassType.Albedo , TextureCount = 3 }
        };

        [Test, Category("Graphics")]
        public void MetaPassTemplateTests([ValueSource(nameof(metaPassTests))]MetaPassSceneTest metaPassTest)
        {
            string sceneName = $"{metaPassTest.SceneName}.unity";
            EditorSceneManager.OpenScene($"Assets/Test/LightBaker/Scenes/{sceneName}");

            // Extract the scene
            using LightBaker.BakeInput bakeInput = new();
            using LightBaker.LightmapRequests lightmapRequest = new();
            using LightBaker.LightProbeRequests probeRequest = new();
            using SourceMap map = new();
            var result = ExtractFromScene(FileUtil.LightBakerTempOutputPath, bakeInput, lightmapRequest, probeRequest,  map);
            Assert.IsTrue(result, $"Scene {metaPassTest.SceneName} failed to extract.");

            // Lookup the texture data for the requested object
            LightBaker.TextureData actualTextureData = new LightBaker.TextureData();
            {
                uint? instanceIndex = SourceMapUtil.LookupInstanceIndex(map, metaPassTest.ObjectName);
                Assert.IsNotNull(instanceIndex, $"Could not find object '{metaPassTest.ObjectName}' in scene '{sceneName}'.");

                switch (metaPassTest.MetaPassType)
                {
                    case MetaPassType.Albedo:
                        {
                            int texIndex = bakeInput.instanceToAlbedoIndex(instanceIndex.Value);
                            Assert.AreNotEqual(-1, texIndex, $"Could not find albedo index for '{metaPassTest.ObjectName}' in scene '{sceneName}'.");
                            actualTextureData = bakeInput.GetAlbedoTextureData((uint)texIndex);
                            Assert.AreEqual(metaPassTest.TextureCount, bakeInput.albedoTextureCount);
                            break;
                        };
                    case MetaPassType.Emission:
                        {
                            int texIndex = bakeInput.instanceToEmissiveIndex(instanceIndex.Value);
                            Assert.AreNotEqual(-1, texIndex, $"Could not find emission index for '{metaPassTest.ObjectName}' in scene '{sceneName}'.");
                            actualTextureData = bakeInput.GetEmissiveTextureData((uint)texIndex);
                            Assert.AreEqual(metaPassTest.TextureCount, bakeInput.emissiveTextureCount);
                            break;
                        }
                }
            }
            Texture2D actualTexture2D = ConvertVector4ImageToRGBAHalfTexture(actualTextureData.resolution.width, actualTextureData.resolution.height, actualTextureData.data);

            // Compare to reference
            string referencePath = $"{FileUtil.PlatformReferenceImagePath}.exr";
            Texture2D reference = LoadImageAndConvertToRGBAHalf(referencePath);

            ImageComparisonSettings imageComparisonSettings = new()
            {
                TargetWidth = actualTexture2D.width,
                TargetHeight = actualTexture2D.height,
                ActivePixelTests = ImageComparisonSettings.PixelTests.None,
                ActiveImageTests = ImageComparisonSettings.ImageTests.RMSE,
                IncorrectPixelsThreshold = 1.0f / (actualTexture2D.width * actualTexture2D.height),
                RMSEThreshold = metaPassRMSETolerance
            };

            try
            {
                ImageAssert.AreEqualLinearHDR(reference, actualTexture2D, imageComparisonSettings);
            }
            catch (AssertionException)
            {
                WriteActualImageToFile();
                throw;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
using static UnityEditor.LightBaking.InputExtraction;

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

        // Block-mean compare noise floor: cross-backend noise peaks at ~0.016 RMSE, the smallest measured real regression at ~0.031.
        public const float DefaultRmseThreshold = 0.025f;
        public float RMSEThreshold { get; set; } = DefaultRmseThreshold;

        public override string ToString()
        {
            return $"{SceneName}-{ObjectName}-{MetaPassType}";
        }
    };

    internal class InputExtractionMaterials
    {
        Texture2D ConvertVector4ImageToRGBAHalfTexture(
            uint width,
            uint height,
            Vector4[] textureData
        )
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

        const int k_DownsampleFactor = 4;

        // Catches uniform drift the RMSE floor allows (10% darkening of a dark albedo ~0.021); cross-backend mean noise < 0.004.
        const float k_MeanColorTolerance = 0.01f;

        // Box-average downsample; partial edge blocks average only their covered texels.
        static Texture2D BoxDownsample(Texture2D src, int factor)
        {
            int w = (src.width + factor - 1) / factor;
            int h = (src.height + factor - 1) / factor;
            Color[] srcPixels = src.GetPixels();
            var dstPixels = new Color[w * h];
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                int blockW = Math.Min(factor, src.width - x * factor);
                int blockH = Math.Min(factor, src.height - y * factor);
                Color sum = Color.clear;
                for (int dy = 0; dy < blockH; ++dy)
                for (int dx = 0; dx < blockW; ++dx)
                    sum += srcPixels[(y * factor + dy) * src.width + (x * factor + dx)];
                dstPixels[y * w + x] = sum / (blockW * blockH);
            }
            Texture2D dst = new(w, h, TextureFormat.RGBAFloat, false, true);
            dst.SetPixels(dstPixels);
            dst.Apply();
            return dst;
        }

        static Color AverageColor(Color[] pixels)
        {
            Color sum = Color.clear;
            foreach (Color pixel in pixels)
                sum += pixel;
            return sum / pixels.Length;
        }

        // Writes to the same destination as the framework's failure exporter (LegacyComparisonImageExporter).
        static void SaveFullResActual(Texture2D actual)
        {
            GraphicsTestBuildSettings buildSettings = GraphicsTestBuildSettings.LoadOrDefault();
            PlatformSchema schema = buildSettings.BuildPlatformSchemata is { Length: > 0 }
                ? buildSettings.BuildPlatformSchemata[0]
                : null; // GetCurrent falls back to the all-platform schema
            ImageHandler.instance.SaveImage(new ImageMessage
            {
                PathName = string.Join('/', buildSettings.ActualImagesPath, GraphicsTestPlatform.GetCurrent(schema).ResultsPath),
                ImageName = TestContext.CurrentContext.Test.Name.ToValidPath(),
                ActualImage = actual.EncodeToEXR(),
            }, "exr");
        }

        private static readonly MetaPassSceneTest[] metaPassTests =
        {
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "default",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "default",
                MetaPassType = MetaPassType.Emission,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "emissive",
                MetaPassType = MetaPassType.Emission,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedo",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "emissiveTex",
                MetaPassType = MetaPassType.Emission,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTex",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTexTriplanarWorldSpace",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTexPlanarWorldSpace",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTexTriplanarObjectSpace",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTexPlanarObjectSpace",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "emissiveTexTiling23",
                MetaPassType = MetaPassType.Emission,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTexTiling23",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "emissiveTexOffset_p5p3",
                MetaPassType = MetaPassType.Emission,
                TextureCount = 13
            },
            new()
            {
                SceneName = "Metapass_BasicMaterials",
                ObjectName = "albedoTexOffset_p5p3",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 13
            },
            new()
            {
                SceneName = "BakedLightingTerrainAlbedo-editor",
                ObjectName = "Terrain",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 3,
                RMSEThreshold = 0.03f
            },
            new()
            {
                SceneName = "BakedLightingTerrainAlbedo-editor",
                ObjectName = "TerrainOffset",
                MetaPassType = MetaPassType.Albedo,
                TextureCount = 3,
                RMSEThreshold = 0.03f
            }
        };

        [Test, Category("Graphics"), GraphicsTest(TextureFormat = TextureFormat.RGBAHalf, ImageExtension = ImageExtension.EXR)]
        public void MetaPassTemplateTests(
            GraphicsTestCase testCase,
            [ValueSource(nameof(metaPassTests))] MetaPassSceneTest metaPassTest
        )
        {
            string sceneName = $"{metaPassTest.SceneName}.unity";
            EditorSceneManager.OpenScene($"Assets/Tests/LightBaker/Scenes/{sceneName}");

            // Extract the scene
            using BakeInput bakeInput = new();
            using LightmapRequests lightmapRequest = new();
            using LightProbeRequests probeRequest = new();
            using SourceMap map = new();
            var result = ExtractFromScene(
                FileUtil.LightBakerTempOutputPath,
                bakeInput,
                lightmapRequest,
                probeRequest,
                map
            );
            Assert.IsTrue(result, $"Scene {metaPassTest.SceneName} failed to extract.");

            // Lookup the texture data for the requested object
            TextureData actualTextureData = new TextureData();
            {
                uint? instanceIndex = SourceMapUtil.LookupInstanceIndex(
                    map,
                    metaPassTest.ObjectName
                );
                Assert.IsNotNull(
                    instanceIndex,
                    $"Could not find object '{metaPassTest.ObjectName}' in scene '{sceneName}'."
                );

                switch (metaPassTest.MetaPassType)
                {
                    case MetaPassType.Albedo:

                        {
                            int texIndex = bakeInput.instanceToAlbedoIndex(instanceIndex.Value);
                            Assert.AreNotEqual(
                                -1,
                                texIndex,
                                $"Could not find albedo index for '{metaPassTest.ObjectName}' in scene '{sceneName}'."
                            );
                            actualTextureData = bakeInput.GetAlbedoTextureData((uint)texIndex);
                            Assert.AreEqual(
                                metaPassTest.TextureCount,
                                bakeInput.albedoTextureCount
                            );
                            break;
                        }
                        ;
                    case MetaPassType.Emission:
                    {
                        int texIndex = bakeInput.instanceToEmissiveIndex(instanceIndex.Value);
                        Assert.AreNotEqual(
                            -1,
                            texIndex,
                            $"Could not find emission index for '{metaPassTest.ObjectName}' in scene '{sceneName}'."
                        );
                        actualTextureData = bakeInput.GetEmissiveTextureData((uint)texIndex);
                        Assert.AreEqual(metaPassTest.TextureCount, bakeInput.emissiveTextureCount);
                        break;
                    }
                }
            }
            Texture2D actualTexture2D = ConvertVector4ImageToRGBAHalfTexture(actualTextureData.resolution.width, actualTextureData.resolution.height, actualTextureData.data);

            // The meta pass samples in lightmap-UV space, so at texture-feature boundaries the extracted
            // albedo/emissive differs slightly per graphics backend. Compare block means (what baking
            // consumes) so one shared reference passes on every backend and vendor.
            // On a missing or size-changed reference, ImageAssert itself reports the problem at full
            // resolution and saves the full-res actual, from which a new reference can be bootstrapped.
            Texture2D expected = testCase.ReferenceImage.Image;
            Texture2D actual = actualTexture2D;
            bool sizesMatch = expected != null
                && expected.width == actual.width && expected.height == actual.height;
            if (sizesMatch)
            {
                Color expectedMean = AverageColor(expected.GetPixels());
                Color actualMean = AverageColor(actual.GetPixels());
                float meanDelta = 0.0f;
                for (int channel = 0; channel < 4; ++channel)
                    meanDelta = Mathf.Max(meanDelta, Mathf.Abs(actualMean[channel] - expectedMean[channel]));
                if (meanDelta > k_MeanColorTolerance)
                {
                    SaveFullResActual(actualTexture2D);
                    Assert.Fail($"Mean color delta {meanDelta} exceeds {k_MeanColorTolerance} (expected mean {expectedMean}, actual mean {actualMean}).");
                }

                expected = BoxDownsample(expected, k_DownsampleFactor);
                actual = BoxDownsample(actual, k_DownsampleFactor);
            }

            ImageComparisonSettings imageComparisonSettings =
                new()
                {
                    ActiveImageTests = ImageComparisonSettings.ImageTests.RMSE,
                    RMSEThreshold = metaPassTest.RMSEThreshold,
                };

            try
            {
                ImageAssert.AreEqualLinearHDR(
                    expected,
                    actual,
                    imageComparisonSettings
                );
            }
            catch when (sizesMatch)
            {
                // Replace the downsampled artifact with the full-res actual so it can be promoted to a reference directly.
                SaveFullResActual(actualTexture2D);
                throw;
            }

            // -save-actual-images rebaseline runs need the promotable full-res image too.
            if (sizesMatch && GraphicsTestBuildSettings.LoadOrDefault().SaveActualImages)
                SaveFullResActual(actualTexture2D);
        }
    }
}

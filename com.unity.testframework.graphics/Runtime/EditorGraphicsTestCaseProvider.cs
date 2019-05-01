#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools.Graphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.TestTools.Graphics
{
    internal class EditorGraphicsTestCaseProvider : IGraphicsTestCaseProvider
    {
        string m_ExpectedImagePath = string.Empty;

        public EditorGraphicsTestCaseProvider()
        {
        }

        public EditorGraphicsTestCaseProvider(string expectedImagePath)
        {
            m_ExpectedImagePath = expectedImagePath;
        }

        public static IEnumerable<string> GetTestScenePaths()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Where(s =>
                {
                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(s);
                    var labels = AssetDatabase.GetLabels(asset);
                    return !labels.Contains("ExcludeGfxTests");
                });
        }

        public IEnumerable<GraphicsTestCase> GetTestCases()
        {
            var allImages = CollectExpectedImagePathsFor(string.IsNullOrEmpty(m_ExpectedImagePath) ? ExpectedImagesRoot : m_ExpectedImagePath, QualitySettings.activeColorSpace, Application.platform,
                SystemInfo.graphicsDeviceType);

            var scenes = GetTestScenePaths();
            foreach (var scenePath in scenes)
            {
                Texture2D expectedImage = null;

                string imagePath;
                if (allImages.TryGetValue(Path.GetFileNameWithoutExtension(scenePath), out imagePath))
                {
                    expectedImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                }

                yield return new GraphicsTestCase(scenePath, expectedImage);
            }
        }

        public GraphicsTestCase GetTestCaseFromPath(string scenePath)
        {
            GraphicsTestCase output = null;

            var allImages = CollectExpectedImagePathsFor(string.IsNullOrEmpty(m_ExpectedImagePath) ? ExpectedImagesRoot : m_ExpectedImagePath, QualitySettings.activeColorSpace, Application.platform,
                SystemInfo.graphicsDeviceType);

            Texture2D expectedImage = null;

            string imagePath;
            if (allImages.TryGetValue(Path.GetFileNameWithoutExtension(scenePath), out imagePath))
                expectedImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

            output = new GraphicsTestCase(scenePath, expectedImage);

            return output;
        }

        public const string ExpectedImagesRoot = "Assets/ExpectedImages";

        public static Dictionary<string, string> CollectExpectedImagePathsFor(string expectedImageRoot, ColorSpace colorSpace, RuntimePlatform runtimePlatform,
            GraphicsDeviceType graphicsApi)
        {
            var result = new Dictionary<string, string>();

            if (!Directory.Exists(expectedImageRoot))
                return result;

            var fullPathPrefix = string.Format("{0}/{1}/{2}/{3}/", expectedImageRoot, colorSpace, runtimePlatform, graphicsApi);

            foreach (var assetPath in AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith(fullPathPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Count(ch => ch == '/')))
            {
                // Skip directories
                if (!File.Exists(assetPath))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                if (fileName == null)
                    continue;

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (!texture)
                    continue;

                result[fileName] = assetPath;
            }

            return result;
        }
    }
}
#endif

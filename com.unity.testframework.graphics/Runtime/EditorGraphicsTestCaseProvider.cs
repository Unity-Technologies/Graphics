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
        string m_ReferenceImagePath = string.Empty;

        public const string ReferenceImagesRoot = "Assets/ReferenceImages";
        public const string ReferenceImagesBaseRoot = "Assets/ReferenceImagesBase";

        public EditorGraphicsTestCaseProvider()
        {
        }

        public EditorGraphicsTestCaseProvider(string referenceImagePath)
        {
            m_ReferenceImagePath = referenceImagePath;
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
            var allImagesSpecific = CollectReferenceImagePathsFor(string.IsNullOrEmpty(m_ReferenceImagePath) ? ReferenceImagesRoot : m_ReferenceImagePath,
                UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice, UseGraphicsTestCasesAttribute.LoadedXRDevice);
            var allImagesBase = CollectReferenceImageBasePaths(ReferenceImagesBaseRoot);

            EditorUtils.SetupReferenceImageImportSettings(allImagesSpecific.Values);
            EditorUtils.SetupReferenceImageImportSettings(allImagesBase.Values);

            var scenes = GetTestScenePaths();
            foreach (var scenePath in scenes)
            {
                Texture2D referenceImage = null;

                string imagePath;
                if (allImagesSpecific.TryGetValue(Path.GetFileNameWithoutExtension(scenePath), out imagePath))
                {
                    referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                }
                else
                {
                    imagePath = $"{ReferenceImagesBaseRoot}/{Path.GetFileNameWithoutExtension(scenePath)}.png";
                    referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                }

                yield return new GraphicsTestCase(scenePath, referenceImage);
            }
        }

        public GraphicsTestCase GetTestCaseFromPath(string scenePath)
        {
            GraphicsTestCase output = null;

            var allImagesSpecific = CollectReferenceImagePathsFor(string.IsNullOrEmpty(m_ReferenceImagePath) ? ReferenceImagesRoot : m_ReferenceImagePath,
                UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice);

            Texture2D referenceImage = null;

            string imagePath;
            if (allImagesSpecific.TryGetValue(Path.GetFileNameWithoutExtension(scenePath), out imagePath))
                referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            else
            {
                imagePath = $"{ReferenceImagesBaseRoot}/{Path.GetFileNameWithoutExtension(scenePath)}.png";
                referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            }

            output = new GraphicsTestCase(scenePath, referenceImage);

            return output;
        }

        public static Dictionary<string, string> CollectReferenceImagePathsFor(string referenceImageRoot, ColorSpace colorSpace, RuntimePlatform runtimePlatform,
            GraphicsDeviceType graphicsApi, string xrsdk = "None")
        {
            var result = new Dictionary<string, string>();

            if (!Directory.Exists(referenceImageRoot))
                return result;

            var fullPathPrefix = $"{referenceImageRoot}/{TestUtils.GetTestResultsFolderPath(colorSpace, runtimePlatform, graphicsApi, xrsdk)}/";

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

        public static Dictionary<string, string> CollectReferenceImageBasePaths(string referenceImageBaseRoot)
        {
            var result = new Dictionary<string, string>();

            if (!Directory.Exists(referenceImageBaseRoot))
                return result;

            foreach (var assetPath in AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith(referenceImageBaseRoot, StringComparison.OrdinalIgnoreCase)))
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

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools.Graphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.TestTools.Graphics
{
    internal class EditorGraphicsTestCaseProvider : IGraphicsTestCaseProvider
    {
        public ColorSpace ColorSpace
        {
            get
            {
                return QualitySettings.activeColorSpace;
            }
        }

        public RuntimePlatform Platform
        {
            get
            {
                return Application.platform;
            }
        }

        public GraphicsDeviceType GraphicsDevice
        {
            get
            {
                return SystemInfo.graphicsDeviceType;
            }
        }


        public IEnumerable<GraphicsTestCase> GetTestCases()
        {
            var allImages = CollectReferenceImagePathsFor(QualitySettings.activeColorSpace, Application.platform,
                SystemInfo.graphicsDeviceType);

            foreach (var scenePath in EditorBuildSettings.scenes.Select(s => s.path))
            {
                Texture2D referenceImage = null;

                string imagePath;
                if (allImages.TryGetValue(Path.GetFileNameWithoutExtension(scenePath), out imagePath))
                {
                    referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                }

                yield return new GraphicsTestCase(scenePath, referenceImage);
            }
        }

        public const string ReferenceImagesRoot = "Assets/ReferenceImages";

        public static Dictionary<string, string> CollectReferenceImagePathsFor(ColorSpace colorSpace, RuntimePlatform runtimePlatform,
            GraphicsDeviceType graphicsApi)
        {
            var result = new Dictionary<string, string>();

            if (!Directory.Exists(ReferenceImagesRoot))
                return result;

            var fullPathPrefix = string.Format("{0}/{1}/{2}/{3}/", ReferenceImagesRoot, colorSpace, runtimePlatform, graphicsApi);

            foreach (var assetPath in AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith(ReferenceImagesRoot, StringComparison.OrdinalIgnoreCase))
                .Where(p => fullPathPrefix.StartsWith(Path.GetDirectoryName(p)))
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

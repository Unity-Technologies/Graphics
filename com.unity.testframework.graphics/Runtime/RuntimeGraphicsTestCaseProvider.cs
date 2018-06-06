using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    internal class RuntimeGraphicsTestCaseProvider : IGraphicsTestCaseProvider
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
            AssetBundle referenceImagesBundle = null;

            var referenceImagesBundlePath = string.Format("{0}/referenceimages-{1}-{2}-{3}", Application.streamingAssetsPath, ColorSpace, Platform, GraphicsDevice);
            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

            foreach (var scenePath in File.ReadAllLines(Application.streamingAssetsPath + "/SceneList.txt"))
            {
                var imagePath = Path.GetFileNameWithoutExtension(scenePath);

                Texture2D referenceImage = null;

                // The bundle might not exist if there are no reference images for this configuration yet
                if (referenceImagesBundle != null)
                    referenceImage = referenceImagesBundle.LoadAsset<Texture2D>(imagePath);

                yield return new GraphicsTestCase(scenePath, referenceImage);
            }
        }


    }
}

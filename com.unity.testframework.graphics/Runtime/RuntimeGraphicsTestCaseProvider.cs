using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Networking;

namespace UnityEngine.TestTools.Graphics
{
    internal class RuntimeGraphicsTestCaseProvider : IGraphicsTestCaseProvider
    {
        public IEnumerable<GraphicsTestCase> GetTestCases()
        {
            AssetBundle referenceImagesBundle = null;
            AssetBundle referenceImagesBaseBundle = null;
            string[] scenePaths;

            // apparently unity automatically saves the asset bundle as all lower case
            var referenceImagesBundlePath = string.Format("referenceimages-{0}-{1}-{2}-{3}",
                UseGraphicsTestCasesAttribute.ColorSpace,
                UseGraphicsTestCasesAttribute.Platform.ToUniqueString(),
                UseGraphicsTestCasesAttribute.GraphicsDevice,
                UseGraphicsTestCasesAttribute.LoadedXRDevice).ToLower();
            referenceImagesBundlePath = Path.Combine(Application.streamingAssetsPath, referenceImagesBundlePath);

            var referenceImagesBaseBundlePath = "referenceimagesbase";
            referenceImagesBaseBundlePath = Path.Combine(Application.streamingAssetsPath, referenceImagesBaseBundlePath);

#if UNITY_ANDROID
            // Unlike standalone where you can use File.Read methods and pass the path to the file,
            // Android requires UnityWebRequest to read files from local storage
            referenceImagesBundle = GetRefImagesBundleViaWebRequest(referenceImagesBundlePath);
            referenceImagesBaseBundle = GetRefImagesBundleViaWebRequest(referenceImagesBaseBundlePath);

            // Same applies to the scene list
            scenePaths = GetScenePathsViaWebRequest(Application.streamingAssetsPath + "/SceneList.txt");
#else
            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

            if (File.Exists(referenceImagesBaseBundlePath))
                referenceImagesBaseBundle = AssetBundle.LoadFromFile(referenceImagesBaseBundlePath);

            scenePaths = File.ReadAllLines(Application.streamingAssetsPath + "/SceneList.txt");
#endif

            foreach (var scenePath in scenePaths)
            {
                var imagePath = Path.GetFileNameWithoutExtension(scenePath);

                Texture2D referenceImage = null;

                // The bundle might not exist if there are no reference images for this configuration yet
                if (referenceImagesBaseBundle != null && referenceImagesBaseBundle.Contains(imagePath))
                    referenceImage = referenceImagesBaseBundle.LoadAsset<Texture2D>(imagePath);

                else if (referenceImagesBundle != null && referenceImagesBundle.Contains(imagePath))
                    referenceImage = referenceImagesBundle.LoadAsset<Texture2D>(imagePath);

                yield return new GraphicsTestCase(scenePath, referenceImage);
            }
        }

        public GraphicsTestCase GetTestCaseFromPath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return null;

            GraphicsTestCase output = null;

            AssetBundle referenceImagesBundle = null;
            AssetBundle referenceImagesBaseBundle = null;

            var referenceImagesBundlePath = string.Format("referenceimages-{0}-{1}-{2}-{3}",
                UseGraphicsTestCasesAttribute.ColorSpace,
                UseGraphicsTestCasesAttribute.Platform.ToUniqueString(),
                UseGraphicsTestCasesAttribute.GraphicsDevice,
                UseGraphicsTestCasesAttribute.LoadedXRDevice).ToLower();

            referenceImagesBundlePath = Path.Combine(Application.streamingAssetsPath, referenceImagesBundlePath);

            var referenceImagesBaseBundlePath = "referenceimagesbase";
            referenceImagesBaseBundlePath = Path.Combine(Application.streamingAssetsPath, referenceImagesBaseBundlePath);

            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

            if (File.Exists(referenceImagesBaseBundlePath))
                referenceImagesBaseBundle = AssetBundle.LoadFromFile(referenceImagesBaseBundlePath);

            var imagePath = Path.GetFileNameWithoutExtension(scenePath);

            Texture2D referenceImage = null;

            // The bundle might not exist if there are no reference images for this configuration yet
            if (referenceImagesBaseBundle != null)
                referenceImage = referenceImagesBaseBundle.LoadAsset<Texture2D>(imagePath);

            else if (referenceImagesBundle != null)
                referenceImage = referenceImagesBundle.LoadAsset<Texture2D>(imagePath);

            output = new GraphicsTestCase(scenePath, referenceImage);

            return output;
        }

        private AssetBundle GetRefImagesBundleViaWebRequest(string referenceImagesBundlePath)
        {
            AssetBundle referenceImagesBundle = null;
            using (var webRequest = new UnityWebRequest(referenceImagesBundlePath))
            {
                var handler = new DownloadHandlerAssetBundle(referenceImagesBundlePath, 0);
                webRequest.downloadHandler = handler;

                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    // wait for response
                }

                if (string.IsNullOrEmpty(webRequest.error))
                {
                    referenceImagesBundle = handler.assetBundle;
                }
                else
                {
                    Debug.Log("Error loading reference image bundle, " + webRequest.error);
                }
            }
            return referenceImagesBundle;
        }

        private string[] GetScenePathsViaWebRequest(string sceneListTextFilePath)
        {
            string[] scenePaths;
            using (var webRequest = UnityWebRequest.Get(sceneListTextFilePath))
            {
                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    // wait for download
                }

                if (string.IsNullOrEmpty(webRequest.error) || webRequest.downloadHandler.text != null)
                {
                    scenePaths = webRequest.downloadHandler.text.Split(
                        new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    scenePaths = new string[] { string.Empty };
                    Debug.Log("Scene list was not found.");
                }
            }
            return scenePaths;
        }
    }
}

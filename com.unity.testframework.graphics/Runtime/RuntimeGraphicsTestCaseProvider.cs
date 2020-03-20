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
            string[] scenePaths;
            var referenceImagesBundlePath = string.Format("{0}/referenceimages-{1}-{2}-{3}",
                Application.streamingAssetsPath,
                UseGraphicsTestCasesAttribute.ColorSpace.ToString().ToLower(),
                UseGraphicsTestCasesAttribute.Platform.ToString().ToLower(),
                UseGraphicsTestCasesAttribute.GraphicsDevice.ToString().ToLower());

#if UNITY_ANDROID
            // Unlike standalone where you can use File.Read methods and pass the path to the file,
            // Android requires UnityWebRequest to read files from local storage
            referenceImagesBundle = GetRefImagesBundleViaWebRequest(referenceImagesBundlePath);

            // Same applies to the scene list
            scenePaths = GetScenePathsViaWebRequest(Application.streamingAssetsPath + "/SceneList.txt");
#else
            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

            scenePaths = File.ReadAllLines(Application.streamingAssetsPath + "/SceneList.txt");
#endif

            foreach (var scenePath in scenePaths)
            {
                var imagePath = Path.GetFileNameWithoutExtension(scenePath);

                Texture2D referenceImage = null;

                // The bundle might not exist if there are no reference images for this configuration yet
                if (referenceImagesBundle != null)
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

            var referenceImagesBundlePath = string.Format("{0}/referenceimages-{1}-{2}-{3}", Application.streamingAssetsPath, UseGraphicsTestCasesAttribute.ColorSpace.ToString().ToLower(), UseGraphicsTestCasesAttribute.Platform.ToString().ToLower(), UseGraphicsTestCasesAttribute.GraphicsDevice.ToString().ToLower());
            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

            var imagePath = Path.GetFileNameWithoutExtension(scenePath);

            Texture2D referenceImage = null;

            // The bundle might not exist if there are no reference images for this configuration yet
            if (referenceImagesBundle != null)
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

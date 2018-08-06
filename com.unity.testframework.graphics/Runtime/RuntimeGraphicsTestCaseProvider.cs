using System.Collections.Generic;
using System.IO;

namespace UnityEngine.TestTools.Graphics
{
    internal class RuntimeGraphicsTestCaseProvider : IGraphicsTestCaseProvider
    {
       public IEnumerable<GraphicsTestCase> GetTestCases()
        {
            AssetBundle referenceImagesBundle = null;

            var referenceImagesBundlePath = string.Format("{0}/referenceimages-{1}-{2}-{3}", Application.streamingAssetsPath, UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice);
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

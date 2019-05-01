using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEngine.TestTools.Graphics
{
    internal class RuntimeGraphicsTestCaseProvider : IGraphicsTestCaseProvider
    {
       public IEnumerable<GraphicsTestCase> GetTestCases()
        {
            AssetBundle expectedImagesBundle = null;

            var expectedImagesBundlePath = string.Format("{0}/expectedimages-{1}-{2}-{3}", Application.streamingAssetsPath, UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice);
            if (File.Exists(expectedImagesBundlePath))
                expectedImagesBundle = AssetBundle.LoadFromFile(expectedImagesBundlePath);

            foreach (var scenePath in File.ReadAllLines(Application.streamingAssetsPath + "/SceneList.txt"))
            {
                var imagePath = Path.GetFileNameWithoutExtension(scenePath);

                Texture2D expectedImage = null;

                // The bundle might not exist if there are no reference images for this configuration yet
                if (expectedImagesBundle != null)
                    expectedImage = expectedImagesBundle.LoadAsset<Texture2D>(imagePath);

                yield return new GraphicsTestCase(scenePath, expectedImage);
            }
        }

        public GraphicsTestCase GetTestCaseFromPath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return null;

            GraphicsTestCase output = null;

            AssetBundle expectedImagesBundle = null;

            var expectedImagesBundlePath = string.Format("{0}/expectedimages-{1}-{2}-{3}", Application.streamingAssetsPath, UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice);
            if (File.Exists(expectedImagesBundlePath))
                expectedImagesBundle = AssetBundle.LoadFromFile(expectedImagesBundlePath);

            var imagePath = Path.GetFileNameWithoutExtension(scenePath);

            Texture2D expectedImage = null;

            // The bundle might not exist if there are no reference images for this configuration yet
            if (expectedImagesBundle != null)
                expectedImage = expectedImagesBundle.LoadAsset<Texture2D>(imagePath);

            output = new GraphicsTestCase( scenePath, expectedImage );

            return output;
        }
    }
}

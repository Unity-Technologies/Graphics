using System.IO;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class PreviewTestUtils
    {
        static Texture2D DrawRTToTexture(RenderTexture renderTexture)
        {
            var prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D output = new(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            Rect readPixels = new(0, 0, output.width, output.height);
            output.ReadPixels(readPixels, 0, 0);
            output.Apply();
            RenderTexture.active = prevActive;
            return output;
        }

        public static void SaveRTToDisk(RenderTexture renderTexture, string assetPath = "")
        {
            if (assetPath == "")
                assetPath = Application.dataPath + "/Texture.png";
            var debugTexture = DrawRTToTexture(renderTexture);
            byte[] bytes = debugTexture.EncodeToPNG();
            File.WriteAllBytes(assetPath, bytes);
        }
    }
}

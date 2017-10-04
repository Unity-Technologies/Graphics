using System.IO;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDEditorUtils
    {
        public static string GetHDRenderPipelinePath()
        {
            // User can create their own directory for SRP, so we need to find the current path that they use.
            // We know that DefaultHDMaterial exist and we know where it is, let's use that to find the current directory.
            var guid = AssetDatabase.FindAssets("DefaultHDMaterial t:material");
            string path = AssetDatabase.GUIDToAssetPath(guid[0]);
            path = Path.GetDirectoryName(path); // Asset is in HDRenderPipeline/RenderPipelineResources/DefaultHDMaterial.mat
            path = path.Replace("RenderPipelineResources", ""); // Keep only path with HDRenderPipeline

            return path;
        }
    }
}

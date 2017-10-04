#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDUtils
    {
#if UNITY_EDITOR
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
#endif

        public const RendererConfiguration k_RendererConfigurationBakedLighting = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;

        public static Matrix4x4 GetViewProjectionMatrix(Matrix4x4 worldToViewMatrix, Matrix4x4 projectionMatrix)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var gpuVP = gpuProj *  worldToViewMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API.

            return gpuVP;
        }

        // Helper to help to display debug info on screen
        static float s_OverlayLineHeight = -1.0f;
        public static void NextOverlayCoord(ref float x, ref float y, float overlayWidth, float overlayHeight, float width)
        {
            x += overlayWidth;
            s_OverlayLineHeight = Mathf.Max(overlayHeight, s_OverlayLineHeight);
            // Go to next line if it goes outside the screen.
            if (x + overlayWidth > width)
            {
                x = 0;
                y -= s_OverlayLineHeight;
                s_OverlayLineHeight = -1.0f;
            }
        }
    }
}

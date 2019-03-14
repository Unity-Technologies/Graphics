using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class HDRPPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Detect if the users forget to assign an HDRP Asset
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                if (!Application.isBatchMode)
                {
                    if (!EditorUtility.DisplayDialog("Build Player",
                                                    "There is no HDRP Asset provided in GraphicsSettings.\nAre you sure you want to continue ?\n Build time can be extremely long without it.", "Ok", "Cancel"))
                    {
                        throw new BuildFailedException("Stop build on request.");
                    }
                }
                else
                {
                    Debug.LogWarning("There is no HDRP Asset provided in GraphicsSettings. Build time can be extremely long without it.");
                }

                return;
            }

            // Don't execute the preprocess if we are not HDRenderPipeline
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdPipelineAsset == null)
                return;

            // If platform is supported all good
            GraphicsDeviceType  unsupportedGraphicDevice = GraphicsDeviceType.Null;
            if (HDUtils.IsSupportedBuildTarget(report.summary.platform)
                && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem)
                && HDUtils.AreGraphicsAPIsSupported(report.summary.platform, out unsupportedGraphicDevice))
                return;
            
            unsupportedGraphicDevice = (unsupportedGraphicDevice == GraphicsDeviceType.Null) ? SystemInfo.graphicsDeviceType : unsupportedGraphicDevice;
            string msg = "The platform " + report.summary.platform.ToString() + " with the graphic API " +  unsupportedGraphicDevice + " is not supported with High Definition Render Pipeline";

            // Throw an exception to stop the build
            throw new BuildFailedException(msg);
        }
    }
}

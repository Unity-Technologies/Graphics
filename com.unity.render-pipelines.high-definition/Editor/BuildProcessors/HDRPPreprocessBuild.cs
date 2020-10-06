using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        int GetMinimumMaxLoDValue(HDRenderPipelineAsset asset)
        {
            int minimumMaxLoD = int.MaxValue;
            var maxLoDs = asset.currentPlatformRenderPipelineSettings.maximumLODLevel;
            var schema = ScalableSettingSchema.GetSchemaOrNull(maxLoDs.schemaId);
            for (int lod = 0; lod < schema.levelCount; ++lod)
            {
                if (maxLoDs.TryGet(lod, out int maxLoD))
                    minimumMaxLoD = Mathf.Min(minimumMaxLoD, maxLoD);
            }

            if (minimumMaxLoD != int.MaxValue)
                return minimumMaxLoD;
            else
                return 0;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Detect if the users forget to assign an HDRP Asset
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                if (!Application.isBatchMode)
                {
                    if (!EditorUtility.DisplayDialog("Build Player",
                                                    "There is no HDRP Asset provided in GraphicsSettings.\nAre you sure you want to continue?\n Build time can be extremely long without it.", "Ok", "Cancel"))
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
            bool supported = HDUtils.AreGraphicsAPIsSupported(report.summary.platform, out unsupportedGraphicDevice)
                && HDUtils.IsSupportedBuildTarget(report.summary.platform)
                && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem);

            if (!supported)
            {
                unsupportedGraphicDevice = (unsupportedGraphicDevice == GraphicsDeviceType.Null) ? SystemInfo.graphicsDeviceType : unsupportedGraphicDevice;
                string msg = "The platform " + report.summary.platform.ToString() + " with the graphic API " + unsupportedGraphicDevice + " is not supported with High Definition Render Pipeline";

                // Throw an exception to stop the build
                throw new BuildFailedException(msg);
            }

            // Update all quality levels with the right max lod so that meshes can be stripped.
            // We don't take lod bias into account because it can be overridden per camera.
            int qualityLevelCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityLevelCount; ++i)
            {
                QualitySettings.SetQualityLevel(i, false);
                var renderPipeline = QualitySettings.renderPipeline as HDRenderPipelineAsset;
                if (renderPipeline != null)
                {
                    QualitySettings.maximumLODLevel = GetMinimumMaxLoDValue(renderPipeline);
                }
                else
                {
                    QualitySettings.maximumLODLevel = GetMinimumMaxLoDValue(hdPipelineAsset);
                }
            }
        }
    }
}

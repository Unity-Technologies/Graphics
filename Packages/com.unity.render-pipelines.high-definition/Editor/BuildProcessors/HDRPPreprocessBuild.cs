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
            // Don't execute the preprocess if we are not HDRenderPipeline
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdPipelineAsset == null)
                return;
            else if (!(hdPipelineAsset as IMigratableAsset).IsAtLastVersion())
                throw new BuildFailedException($"GraphicSetting's HDRenderPipelineAsset {AssetDatabase.GetAssetPath(hdPipelineAsset)} is a non updated asset. Please use HDRP wizard to fix it.");

            // If platform is not supported, throw an exception to stop the build
            if (!HDUtils.IsSupportedBuildTargetAndDevice(report.summary.platform, out GraphicsDeviceType deviceType))
                throw new BuildFailedException(HDUtils.GetUnsupportedAPIMessage(deviceType.ToString()));

            //ensure global settings exist and at last version
            if (HDRenderPipelineGlobalSettings.instance == null)
                throw new BuildFailedException("There is currently no HDRenderPipelineGlobalSettings in use. Please use HDRP wizard to fix it.");
            if (!(HDRenderPipelineGlobalSettings.instance as IMigratableAsset).IsAtLastVersion())
                throw new BuildFailedException($"Current HDRenderPipelineGlobalSettings {AssetDatabase.GetAssetPath(HDRenderPipelineGlobalSettings.instance)} is a non updated asset. Please use HDRP wizard to fix it.");

            // Update all quality levels with the right max lod so that meshes can be stripped.
            // We don't take lod bias into account because it can be overridden per camera.
            QualitySettings.ForEach((tier, name) =>
            {
                if (!((QualitySettings.renderPipeline as IMigratableAsset)?.IsAtLastVersion() ?? true))
                    throw new BuildFailedException(
                        $"Quality {tier} - {name} use a non updated asset {AssetDatabase.GetAssetPath(QualitySettings.renderPipeline)}. Please use HDRP wizard to fix it.");

                var renderPipeline = QualitySettings.renderPipeline as HDRenderPipelineAsset;
                if (renderPipeline != null)
                {
                    QualitySettings.maximumLODLevel = GetMinimumMaxLoDValue(renderPipeline);
                }
                else
                {
                    QualitySettings.maximumLODLevel = GetMinimumMaxLoDValue(hdPipelineAsset);
                }
            });
        }
    }
}

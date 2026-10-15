using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    internal static class URPProjectAuditorUtilities
    {
        public static bool IsURPActiveInProject()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                return true;

            bool found = false;
            QualitySettings.ForEach(() =>
            {
                if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset)
                    found = true;
            });
            return found;
        }

        public static bool UsesQuadraticFalloff(out bool lightCookiesDisabled)
        {
            lightCookiesDisabled = false;

            if (!IsURPActiveInProject() ||
                !EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<URPLightFalloffSettings, UniversalRenderPipeline>(out var falloffSettings) ||
                falloffSettings.lightFalloffMode != LightFalloffMode.Quadratic)
                return false;

            bool cookiesDisabled = false;
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset)
                cookiesDisabled = !urpAsset.supportsLightCookies;

            QualitySettings.ForEach(() =>
            {
                if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset)
                    cookiesDisabled |= !qualityUrpAsset.supportsLightCookies;
            });

            lightCookiesDisabled = cookiesDisabled;
            return true;
        }

        public static List<ScriptableRendererData> GetAllRendererDataAssets()
        {
            var assets = new List<ScriptableRendererData>();

            // Get from Graphics Settings
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                var rendererDataList = urpAsset.m_RendererDataList;
                if (rendererDataList != null)
                {
                    assets.AddRange(rendererDataList);
                }
            }

            // Get from Quality Settings
            QualitySettings.ForEach(() =>
            {
                if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset)
                {
                    var rendererDataList = qualityUrpAsset.m_RendererDataList;
                    if (rendererDataList != null)
                    {
                        foreach (var data in rendererDataList)
                        {
                            if (data != null && !assets.Contains(data))
                                assets.Add(data);
                        }
                    }
                }
            });

            return assets;
        }
    }
}

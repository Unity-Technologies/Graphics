using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using URPEditorShaderUtils = Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.XRGraphics
{
    /// <summary>
    /// Sends one event per URP player build with XR enabled, describing the rendering configuration in the build:
    /// Tile-Only Mode, On-Tile Post Processing, Render Objects depth input attachment, MSAA, rendering paths,
    /// renderer features and shader usage.
    /// </summary>
    class URPXRGraphicsAnalytic : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = XRGraphicsAnalyticsUtils.k_VendorKey;
        const string k_EventName = XRGraphicsAnalyticsUtils.k_EventPrefix + "urp_build_configuration";
        // SCHEMA: com.unity3d.data.schemas.editor.analytics.xrgraphics_urp_build_configuration_v1
        // TAXONOMY : editor.analytics.xrgraphics_urp_build_configuration.v1

        // Must run before URPPreprocessBuild disposes URPBuildData.
        public int callbackOrder => int.MinValue + 50;

        internal static readonly HashSet<Shader> s_ShadersInBuild = new();
        internal static bool s_Collecting;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_ShadersInBuild.Clear();
            s_Collecting = ShouldCollect(EditorAnalytics.enabled, report.summary.platformGroup);
        }

        /// <summary>
        /// Decides at build start whether shaders should be collected: analytics must be enabled and XR active for the target.
        /// The URP check happens at send time, because URPBuildData does not exist yet.
        /// </summary>
        internal static bool ShouldCollect(bool analyticsEnabled, BuildTargetGroup buildTargetGroup)
        {
            if (!analyticsEnabled)
                return false;

            var xrLoaders = s_GetActiveXRLoaders(buildTargetGroup);
            return xrLoaders != null && xrLoaders.Count > 0;
        }

        /// <summary>
        /// Records shaders that still have variants after all other shader preprocessors have stripped them.
        /// Kept separate from the outer class because it needs a different callback order.
        /// </summary>
        internal class ShaderCollector : IPreprocessShaders
        {
            public int callbackOrder => int.MaxValue;

            public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
            {
                if (s_Collecting && shader != null && data != null && data.Count > 0)
                    s_ShadersInBuild.Add(shader);
            }
        }

        // Overridable for tests.
        internal static Func<BuildTargetGroup, IReadOnlyList<string>> s_GetActiveXRLoaders = group => XRGraphicsAnalyticsUtils.GetActiveXRLoaders(group);
        internal static Action<IAnalytic> s_Send = analytic => EditorAnalytics.SendAnalytic(analytic);

        public void OnPostprocessBuild(BuildReport report)
        {
            try
            {
                if (!EditorAnalytics.enabled)
                    return;

                var buildData = URPBuildData.instance;
                TrySend(report.summary.platform, report.summary.platformGroup,
                    buildData.buildingPlayerForUniversalRenderPipeline, buildData.renderPipelineAssets, s_ShadersInBuild);
            }
            finally
            {
                s_ShadersInBuild.Clear();
                s_Collecting = false;
            }
        }

        /// <summary>
        /// Sends the event if the build is for URP and has at least one active XR loader.
        /// Returns true if the event was sent.
        /// </summary>
        internal static bool TrySend(BuildTarget buildTarget, BuildTargetGroup buildTargetGroup, bool buildingForURP,
            IReadOnlyList<UniversalRenderPipelineAsset> assets, IEnumerable<Shader> shaders)
        {
            if (!buildingForURP)
                return false;

            var xrLoaders = s_GetActiveXRLoaders(buildTargetGroup);
            if (xrLoaders == null || xrLoaders.Count == 0)
                return false;

            s_Send(new Analytic(buildTarget, xrLoaders, assets, shaders));
            return true;
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, maxEventsPerHour: k_MaxEventsPerHour, maxNumberOfElements: k_MaxNumberOfElements)]
        internal class Analytic : IAnalytic
        {
            readonly BuildTarget m_BuildTarget;
            readonly IReadOnlyList<string> m_XRLoaders;
            readonly IReadOnlyList<UniversalRenderPipelineAsset> m_Assets;
            readonly IEnumerable<Shader> m_Shaders;

            public Analytic(BuildTarget buildTarget, IReadOnlyList<string> xrLoaders, IReadOnlyList<UniversalRenderPipelineAsset> assets, IEnumerable<Shader> shaders)
            {
                m_BuildTarget = buildTarget;
                m_XRLoaders = xrLoaders;
                m_Assets = assets;
                m_Shaders = shaders;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = Gather(m_BuildTarget, m_XRLoaders, m_Assets, m_Shaders);
                error = null;
                return true;
            }

            [Serializable]
            internal struct AnalyticsData : IAnalytic.IData
            {
                public string category;
                public string build_target;
                public string[] xr_loaders;

                public int urp_asset_count;
                public int[] msaa_sample_counts;

                public int universal_renderer_count;
                public string[] rendering_modes;

                public int tile_only_mode_renderer_count;
                public bool tile_only_mode_supported_on_build_target;

                public int on_tile_post_processing_renderer_count;
                public int on_tile_post_processing_texture_fallback_count;

                public int render_objects_feature_count;
                public int render_objects_depth_input_attachment_count;

                public string[] builtin_renderer_features;
                public int custom_renderer_feature_count;
                public int custom_renderer_feature_type_count;

                public int shader_graph_shader_count;
                public string[] shader_graph_targets;
                public int custom_shader_count;
                public int custom_package_shader_count;
            }

            /// <summary>
            /// Builds the event payload from the URP assets and shaders included in the build.
            /// </summary>
            internal static AnalyticsData Gather(BuildTarget buildTarget, IReadOnlyList<string> xrLoaders, IReadOnlyList<UniversalRenderPipelineAsset> assets, IEnumerable<Shader> shaders)
            {
                var data = new AnalyticsData
                {
                    category = XRGraphicsAnalyticsUtils.k_Category,
                    build_target = buildTarget.ToString(),
                    xr_loaders = xrLoaders != null ? xrLoaders.ToArray() : Array.Empty<string>(),
                    tile_only_mode_supported_on_build_target = buildTarget != BuildTarget.WebGL,
                };

                var msaa = new List<int>();
                var renderers = new HashSet<UniversalRendererData>();

                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        if (asset == null)
                            continue;

                        data.urp_asset_count++;
                        msaa.Add(asset.msaaSampleCount);

                        foreach (var rendererData in asset.m_RendererDataList)
                        {
                            if (rendererData is UniversalRendererData universal)
                                renderers.Add(universal);
                        }
                    }
                }

                data.msaa_sample_counts = msaa.ToArray();
                data.universal_renderer_count = renderers.Count;

                var renderingModes = new List<string>(renderers.Count);
                var builtinFeatures = new SortedSet<string>(StringComparer.Ordinal);
                var customFeatureTypes = new HashSet<Type>();
                var urpRuntimeAssembly = typeof(ScriptableRendererFeature).Assembly;

                foreach (var renderer in renderers)
                {
                    renderingModes.Add(renderer.renderingMode.ToString());

                    if (renderer.tileOnlyMode)
                        data.tile_only_mode_renderer_count++;

                    foreach (var feature in renderer.rendererFeatures)
                    {
                        if (feature == null || !feature.isActive)
                            continue;

                        var type = feature.GetType();

                        switch (feature)
                        {
                            case OnTilePostProcessFeature:
                                data.on_tile_post_processing_renderer_count++;
                                if (!renderer.tileOnlyMode)
                                    data.on_tile_post_processing_texture_fallback_count++;
                                break;
                            case RenderObjects renderObjects:
                                data.render_objects_feature_count++;
                                if (renderObjects.settings != null && renderObjects.settings.depthInputAttachment)
                                    data.render_objects_depth_input_attachment_count++;
                                break;
                        }

                        if (type.Assembly == urpRuntimeAssembly)
                        {
                            builtinFeatures.Add(type.Name);
                        }
                        else
                        {
                            // Count only; user type names are not sent.
                            data.custom_renderer_feature_count++;
                            customFeatureTypes.Add(type);
                        }
                    }
                }

                data.rendering_modes = renderingModes.ToArray();
                data.builtin_renderer_features = builtinFeatures.ToArray();
                data.custom_renderer_feature_type_count = customFeatureTypes.Count;

                GatherShaderData(shaders, ref data);

                return data;
            }

            internal enum ShaderOrigin
            {
                Unknown,
                UnityPackage,
                Project,
                ThirdPartyPackage,
            }

            const string k_UnityPackagePrefix = "com.unity.";

            /// <summary>
            /// Classifies a shader by where it lives: the project, a Unity package (com.unity.*) or another package.
            /// </summary>
            internal static ShaderOrigin GetShaderOrigin(string assetPath, string packageName)
            {
                if (string.IsNullOrEmpty(assetPath))
                    return ShaderOrigin.Unknown;

                if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                    return ShaderOrigin.Project;

                if (string.IsNullOrEmpty(packageName))
                    return ShaderOrigin.Unknown;

                return packageName.StartsWith(k_UnityPackagePrefix, StringComparison.Ordinal)
                    ? ShaderOrigin.UnityPackage
                    : ShaderOrigin.ThirdPartyPackage;
            }

            static void GatherShaderData(IEnumerable<Shader> shaders, ref AnalyticsData data)
            {
                var shaderGraphTargets = new SortedDictionary<string, int>(StringComparer.Ordinal);

                if (shaders != null)
                {
                    foreach (var shader in shaders)
                    {
                        if (shader == null)
                            continue;

                        var path = AssetDatabase.GetAssetPath(shader);
                        var packageName = path.StartsWith("Packages/", StringComparison.Ordinal)
                            ? UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path)?.name
                            : null;

                        var origin = GetShaderOrigin(path, packageName);
                        if (origin is ShaderOrigin.Unknown or ShaderOrigin.UnityPackage)
                            continue;

                        if (shader.IsShaderGraphAsset())
                        {
                            data.shader_graph_shader_count++;
                            var id = URPEditorShaderUtils.GetShaderID(shader);
                            var key = id == URPEditorShaderUtils.ShaderID.Unknown ? "Other" : id.ToString();
                            shaderGraphTargets.TryGetValue(key, out var count);
                            shaderGraphTargets[key] = count + 1;
                        }
                        else if (origin == ShaderOrigin.Project)
                        {
                            data.custom_shader_count++;
                        }
                        else
                        {
                            data.custom_package_shader_count++;
                        }
                    }
                }

                data.shader_graph_targets = shaderGraphTargets
                    .Select(kv => $"{{\"shader_id\":\"{kv.Key}\",\"count\":\"{kv.Value}\"}}")
                    .ToArray();
            }
        }
    }
}

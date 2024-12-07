using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.VFX.UI;
using UnityEditor.PackageManager.UI;

using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    interface IEditorAnalytics
    {
        bool enabled { get; }
        bool CanBeSent(VFXAnalytics.UsageEventData data);
        AnalyticsResult SendAnalytic(IAnalytic analytic);
    }

    interface IBuildReport
    {
        IEnumerable<string> packedAssetsInfoPath { get; }
        BuildSummary summary { get; }
    }

    class BuildReportWrapper : IBuildReport
    {
        private readonly BuildReport m_BuildReport;

        public BuildReportWrapper(BuildReport buildReport)
        {
            m_BuildReport = buildReport;
        }

        public IEnumerable<string> packedAssetsInfoPath => m_BuildReport.packedAssets.SelectMany(x => x.contents).Select(x => x.sourceAssetPath);
        public BuildSummary summary => m_BuildReport.summary;
    }

    class EditorAnalyticsWrapper : IEditorAnalytics
    {
        public bool enabled => EditorAnalytics.enabled;

        public AnalyticsResult SendAnalytic(IAnalytic analytic)
        {
            return EditorAnalytics.SendAnalytic(analytic);
        }

        public bool CanBeSent(VFXAnalytics.UsageEventData data) => data.nb_vfx_assets > 0 || data.nb_vfx_opened > 0;
    }

    class VFXAnalyticsPostProcess : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            VFXAnalytics.GetInstance().OnPostprocessBuild(report);
        }
    }

    class VFXAnalytics
    {
       [Serializable]
        internal class VFXAnalyticsData
        {
            public List<GraphInfo> openedGraphInfo = new ();

            public List<string> usedSpecificSettingNames = new();
            public List<int> usedSpecificSettingsCount = new();

            public List<string> compilationErrorsMessages = new();
            public List<int> compilationErrorsCount = new();

            public List<string> systemTemplatesUsed = new();

            public void AddCompilationError(Exception exception)
            {
                var index = compilationErrorsMessages.IndexOf(exception.Message);
                if (index < 0)
                {
                    compilationErrorsMessages.Add(exception.Message);
                    compilationErrorsCount.Add(1);
                }
                else
                {
                    compilationErrorsCount[index]++;
                }
                Save();
            }

            public void AddSpecificSettingChanged(string settingPath)
            {
                var index = usedSpecificSettingNames.IndexOf(settingPath);
                if (index < 0)
                {
                    usedSpecificSettingNames.Add(settingPath);
                    usedSpecificSettingsCount.Add(1);
                }
                else
                {
                    usedSpecificSettingsCount[index]++;
                }
                Save();
            }

            public void AddSystemTemplateCreated(string templateName)
            {
                if (!systemTemplatesUsed.Contains(templateName))
                {
                    systemTemplatesUsed.Add(templateName);
                    Save();
                }
            }

            public void UpdateGraphData(VFXView view)
            {
                // This can happen during auto-tests
                if (view.controller.model.asset == null)
                {
                    return;
                }

                var instanceId = view.controller.model.asset.GetInstanceID();
                var graphInfo = openedGraphInfo.SingleOrDefault(x => x.graph_id == instanceId);
                if (graphInfo.graph_id > 0)
                {
                    openedGraphInfo.Remove(graphInfo);
                }

                var experimentalNodeUsage = view.GetAllNodes()
                    .Union(view.GetAllContexts().SelectMany(x => x.GetAllBlocks()))
                    .Select(x => x.controller.model)
                    .Where(x => VFXInfoAttribute.Get(x.GetType())?.experimental == true)
                    .Select(x => x.name)
                    .Distinct()
                    .ToList();

                if (experimentalNodeUsage.Any(string.IsNullOrEmpty))
                {
                    throw new Exception("Experimental node name is empty");
                }

                openedGraphInfo.Add(new GraphInfo
                {
                    graph_id = instanceId,
                    node_count = view.GetAllNodes().Count(),
                    experimentatl_node_names = experimentalNodeUsage,
                });

                Save();
            }

            public void Clear()
            {
                openedGraphInfo.Clear();
                usedSpecificSettingNames.Clear();
                usedSpecificSettingsCount.Clear();
                compilationErrorsMessages.Clear();
                compilationErrorsCount.Clear();
                systemTemplatesUsed.Clear();
                SessionState.EraseString(nameof(VFXAnalyticsData));
            }

            public static VFXAnalyticsData TryLoad()
            {
                var serializedData = SessionState.GetString(nameof(VFXAnalyticsData), null);
                if (!string.IsNullOrEmpty(serializedData))
                {
                    return JsonUtility.FromJson<VFXAnalyticsData>(serializedData);
                }

                return new VFXAnalyticsData();
            }

            private void Save()
            {
                var serializedData = JsonUtility.ToJson(this);
                SessionState.SetString(nameof(VFXAnalyticsData), serializedData);
            }
        }

        const string k_AdditionalSamples = "VisualEffectGraph Additions";
        const string k_AdditionalHelpers = "OutputEvent Helpers";

        static VFXAnalytics s_Instance;

        readonly IEditorAnalytics m_EditorAnalytics;

        bool m_IsDataRegistered;
        VFXAnalyticsData m_VFXAnalyticsData;

        [Serializable]
        internal struct GraphInfo
        {
            public int graph_id;
            public int node_count;
            public List<string> experimentatl_node_names;
        }

        protected internal enum EventKind
        {
            ProjectBuild,
            Quit,
        }

        [Serializable]
        internal struct UsageEventData : IAnalytic.IData
        {
            public string event_kind;
            public string build_target;
            public int nb_vfx_assets;
            public int nb_vfx_opened;
            public double mean_nb_node_per_assets;
            public double stdv_nb_node_per_assets;
            public int max_nb_node_per_assets;
            public int min_nb_node_per_assets;
            public List<string> experimental_node_names;
            public List<float> experimental_node_count_per_asset;
            public List<string> compilation_error_names;
            public List<int> compilation_error_count;
            public List<string> specific_setting_names;
            public List<int> specific_setting_Count;
            public int has_samples_installed;
            public int has_helpers_installed;
            public List<string> system_template_used;
        }

        [AnalyticInfo(eventName: "uVFXGraphUsage", vendorKey: "unity.vfxgraph", maxEventsPerHour: 10, maxNumberOfElements: 1000, version: 4)]
        internal class Analytic : IAnalytic
        {
            public Analytic(UsageEventData data) { m_Data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            public UsageEventData m_Data;
        }

        protected internal VFXAnalytics(IEditorAnalytics editorAnalytics)
        {
            m_EditorAnalytics = editorAnalytics;
        }

        public static VFXAnalytics GetInstance()
        {
            return s_Instance ??= new VFXAnalytics(new EditorAnalyticsWrapper());
        }

        public void OnCompilationError(Exception exception)
        {
            try
            {
                GetOrCreateAnalyticsData().AddCompilationError(exception);
            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log compilation error\n{e.Message}");
            }
        }

        public void OnSpecificSettingChanged(string settingPath)
        {
            try
            {
                GetOrCreateAnalyticsData().AddSpecificSettingChanged(settingPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log specific setting change '{settingPath}'\n{e.Message}");
            }
        }

        public void OnSystemTemplateCreated(string templateName)
        {
            try
            {
                GetOrCreateAnalyticsData().AddSystemTemplateCreated(templateName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log template use '{templateName}'\n{e.Message}");
            }
        }

        public void OnGraphClosed(VFXView view)
        {
            try
            {
                GetOrCreateAnalyticsData().UpdateGraphData(view);
            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log graph close event\n{e.Message}");
            }
        }

        public void OnQuitApplication()
        {
            try
            {
                var data = new UsageEventData
                {
                    event_kind = EventKind.Quit.ToString(),
                    build_target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    nb_vfx_assets = CalculateNumberOfVFXInScene(SceneManager.GetActiveScene()),
                };

                // Take all opened VFX Graph currently opened into account
                var vfxAnalyticsData = GetOrCreateAnalyticsData();
                VFXViewWindow.GetAllWindows()
                    .Where(x => x.graphView != null)
                    .ToList()
                    .ForEach(x => vfxAnalyticsData.UpdateGraphData(x.graphView));

                FillAndSendData(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log application quit event\n{e.Message}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            OnPostprocessBuildInternal(new BuildReportWrapper(report));
        }

        private void OnPostprocessBuildInternal(IBuildReport report)
        {
            try
            {
                if (m_EditorAnalytics.enabled)
                {
                    var assetsCount = 0;
                    foreach (var sourceAssetPath in report.packedAssetsInfoPath.Distinct())
                    {
                        if (sourceAssetPath.EndsWith(".vfx", StringComparison.OrdinalIgnoreCase))
                        {
                            assetsCount++;
                        }
                    }

                    var data = new UsageEventData
                    {
                        event_kind = EventKind.ProjectBuild.ToString(),
                        build_target = report.summary.platform.ToString(),
                        nb_vfx_assets = assetsCount,
                        nb_vfx_opened = 0,
                    };

                    Send(data);
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log project build event\n{e.Message}");
            }
        }


        // Uncomment for testing purpose
        /*
        public void OnSaveVFXAsset(VFXView vfxView)
        {
            // Useful for testing
            try
            {
                GetOrCreateAnalyticsData().UpdateGraphData(vfxView);
                OnQuitApplication();
            }
            catch (Exception e)
            {
                Debug.LogError($"Analytics could not log asset save event\n{e.Message}");
            }
        }*/

        private void FillAndSendData(UsageEventData data)
        {
            if (m_EditorAnalytics.enabled)
            {
                var analyticsData = GetOrCreateAnalyticsData();

                (data.mean_nb_node_per_assets,
                 data.stdv_nb_node_per_assets,
                 data.min_nb_node_per_assets,
                 data.max_nb_node_per_assets) = GetStats(analyticsData.openedGraphInfo.Select(x => x.node_count).ToArray());
                data.nb_vfx_opened = analyticsData.openedGraphInfo.Count;

                var experimentalNodeUsage = analyticsData.openedGraphInfo
                    .SelectMany(x => x.experimentatl_node_names)
                    .GroupBy(x => x)
                    .ToDictionary(x => x.Key, x => x.Count() / (float)data.nb_vfx_opened);

                data.experimental_node_names = experimentalNodeUsage.Keys.ToList();
                data.experimental_node_count_per_asset = experimentalNodeUsage.Values.ToList();

                data.has_samples_installed = HasPackage(k_AdditionalSamples) ? 1 : 0;
                data.has_helpers_installed = HasPackage(k_AdditionalHelpers) ? 1 : 0;

                data.system_template_used = analyticsData.systemTemplatesUsed;
                data.compilation_error_names = analyticsData.compilationErrorsMessages;
                data.compilation_error_count = analyticsData.compilationErrorsCount;

                data.specific_setting_names = analyticsData.usedSpecificSettingNames;
                data.specific_setting_Count = analyticsData.usedSpecificSettingsCount;

                Send(data);

                analyticsData.Clear();
            }
        }

        protected internal VFXAnalyticsData GetOrCreateAnalyticsData()
        {
            return m_VFXAnalyticsData ??= VFXAnalyticsData.TryLoad();
        }

        private static int CalculateNumberOfVFXInScene(Scene scene)
        {
            return scene.GetRootGameObjects().Sum(TraverseScene);
        }

        private void Send(UsageEventData data)
        {
            if (m_EditorAnalytics.CanBeSent(data))
            {
                Analytic analytic = new Analytic(data);
                m_EditorAnalytics.SendAnalytic(analytic);
            }
        }

        private bool HasPackage(string sampleName)
        {
            var sample = Sample.FindByPackage(VisualEffectGraphPackageInfo.name, null).SingleOrDefault(x => x.displayName == sampleName);
            return sample.isImported;
        }

        private static int TraverseScene(GameObject go)
        {
            var count = 0;
            if (go.GetComponent<VisualEffect>() is { } visualEffect && visualEffect != null)
            {
                count++;
            }

            foreach(var child in go.transform.OfType<UnityEngine.Transform>())
            {
                count += TraverseScene(child.gameObject);
            }

            return count;
        }

        private static (double mean, double stddev, int min, int max) GetStats(int[] nodeCounts)
        {
            if (nodeCounts.Length == 0)
            {
                return (0, 0, 0, 0);
            }

            var stddev = 0d;
            var mean = nodeCounts.Average();

            if (nodeCounts.Length  > 1)
            {
                var sum = nodeCounts.Sum(x => (x - mean) * (x - mean));
                stddev = Math.Sqrt(sum / nodeCounts.Length);
            }

            return (mean, stddev, nodeCounts.Min(), nodeCounts.Max());
        }
    }
}

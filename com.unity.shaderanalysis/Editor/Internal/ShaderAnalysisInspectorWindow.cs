using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public class ShaderAnalysisInspectorWindow : EditorWindow
    {
        #region Platform UI API
        public delegate void DrawProgramToolbar(Object asset, ShaderBuildReport report, ShaderBuildReport.GPUProgram program, ShaderBuildReport reference, string assetGUID);
        public delegate void DrawUnitToolbar(
            Object asset,
            ShaderBuildReport r,
            ShaderBuildReport.GPUProgram po,
            ShaderBuildReport.CompileUnit cu,
            ShaderBuildReport.PerformanceUnit pu,
            ShaderBuildReport.ProgramPerformanceMetrics p,
            ShaderBuildReport reference,
            string assetGUID);

        static Dictionary<BuildTarget, DrawProgramToolbar> s_DrawProgramToolbars = new Dictionary<BuildTarget, DrawProgramToolbar>();
        static Dictionary<BuildTarget, DrawUnitToolbar> s_DrawUnitToolbars = new Dictionary<BuildTarget, DrawUnitToolbar>();

        public static void SetDrawProgramToolbar(BuildTarget target, DrawProgramToolbar callback)
        {
            s_DrawProgramToolbars[target] = callback;
        }

        static DrawProgramToolbar GetProgramToolbar(BuildTarget target)
        {
            DrawProgramToolbar result = null;
            return s_DrawProgramToolbars.TryGetValue(target, out result) ? result : DefaultOnGUI_ProgramToolbar;
        }

        static void DefaultOnGUI_ProgramToolbar(
            Object asset,
            ShaderBuildReport report,
            ShaderBuildReport.GPUProgram program,
            ShaderBuildReport reference,
            string assetGUID)
        {
            if (!string.IsNullOrEmpty(program.sourceCode) && GUILayout.Button("Open", EditorStyles.toolbarButton))
                program.OpenSourceCode();
        }

        public static void SetDrawUnitToolbar(BuildTarget target, DrawUnitToolbar callback)
        {
            s_DrawUnitToolbars[target] = callback;
        }

        static DrawUnitToolbar GetUnitToolbar(BuildTarget target)
        {
            DrawUnitToolbar result = null;
            return s_DrawUnitToolbars.TryGetValue(target, out result) ? result : DefaultOnGUI_UnitToolbar;
        }

        static void DefaultOnGUI_UnitToolbar(
            Object asset,
            ShaderBuildReport r,
            ShaderBuildReport.GPUProgram po,
            ShaderBuildReport.CompileUnit cu,
            ShaderBuildReport.PerformanceUnit pu,
            ShaderBuildReport.ProgramPerformanceMetrics p,
            ShaderBuildReport reference,
            string assetGUID)
        {

        }
        #endregion

        public static string referenceFolderPath
        {
            get { return EditorPrefs.GetString("ShaderTools.Perfs.ReferenceFolder", "Library/ShaderAnalysisReference"); }
            set { EditorPrefs.SetString("ShaderTools.Perfs.ReferenceFolder", value); }
        }

        public static string shaderPassToAnalyse = "All";

        public static DirectoryInfo referenceFolder
        {
            get { return string.IsNullOrEmpty(referenceFolderPath) ? null : new DirectoryInfo(referenceFolderPath); }
        }

        public static string referenceSourceFolderPath
        {
            get { return EditorPrefs.GetString("ShaderTools.Perfs.ReferenceSourceFolder", string.Empty); }
            set { EditorPrefs.SetString("ShaderTools.Perfs.ReferenceSourceFolder", value); }
        }

        public static DirectoryInfo referenceSourceFolder
        {
            get { return string.IsNullOrEmpty(referenceSourceFolderPath) ? null : new DirectoryInfo(referenceSourceFolderPath); }
        }

        delegate void GUIDrawer();

        GUIDrawer m_GUI = null;

        Shader m_Shader;
        ComputeShader m_Compute;
        Material m_Material;

        BuildTarget m_CurrentPlatform = 0;
        IAsyncJob m_CurrentJob;
        bool m_AutoRefreshRegistered;

        AssetMetadata m_AssetMetadata;
        AssetMetadata m_AssetMetadataReference;
        Vector2 m_BuildScrollPosition = Vector2.zero;

        SimpleDataCache m_BuildReportCache = new SimpleDataCache();
        Dictionary<IAsyncJob, Object> m_JobAssets = new Dictionary<IAsyncJob, Object>();

        ReportExporterIndex m_ReportExporterIndex;
        ReportDiffExporterIndex m_ReportDiffExporterIndex;

        string[] m_SupportedPlatformNames;
        BuildTarget[] m_SupportedPlatforms;
        int m_SelectedPlatformIndex;
        Object m_SelectedAsset;

        void OnEnable()
        {
            m_CurrentPlatform = EditorUserBuildSettings.activeBuildTarget;
            m_AssetMetadata = ShaderAnalysisUtils.LoadAssetMetadatasFor(m_CurrentPlatform);
            m_AssetMetadataReference = ShaderAnalysisUtils.LoadAssetMetadatasFor(m_CurrentPlatform, referenceFolder);

            if (m_SupportedPlatformNames == null)
            {
                m_SupportedPlatformNames = EditorShaderTools.SupportedBuildTargets.Select(s => s.ToString()).ToArray();
                m_SupportedPlatforms = EditorShaderTools.SupportedBuildTargets.ToArray();
            }

            m_GUI = NOOPGUI;
        }

        void OpenAsset(Object asset)
        {
            ResetUI();
            var shader = Selection.activeObject as Shader;
            if (shader != null)
            {
                OpenAsset(shader);
                Repaint();
                return;
            }
            var compute = Selection.activeObject as ComputeShader;
            if (compute != null)
            {
                OpenAsset(compute);
                Repaint();
                return;
            }
            var material = Selection.activeObject as Material;
            if (material != null)
            {
                OpenAsset(material);
                Repaint();
                return;
            }

            m_GUI = NOOPGUI;
            Repaint();
        }

        void OpenAsset(Shader shader)
        {
            m_GUI = OnGUI_Shader;
            m_Shader = shader;
        }

        void OpenAsset(ComputeShader compute)
        {
            m_GUI = OnGUI_ComputeShader;
            m_Compute = compute;
        }

        void OpenAsset(Material material)
        {
            m_GUI = OnGUI_Material;
            m_Material = material;
        }

        void OnGUI()
        {
            m_GUI();
        }

        void OnGUI_Shader()
        {
            OnGUI_Header(m_Shader.name);

            OnGUI_ToolBar(BuildShaderReport, m_Shader, PlatformJob.BuildShaderPerfReport);
            OnGUI_PassSelection();
            OnGUI_BuildLogs(m_Shader);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.Box(GUIContent.none, GUIStyle.none);
            GUILayout.EndVertical();
            OnGUI_AsyncJob();
        }

        void OnGUI_ComputeShader()
        {
            OnGUI_Header(m_Compute.name);

            OnGUI_ToolBar(BuildComputeShaderReport, m_Compute, PlatformJob.BuildComputeShaderPerfReport);

            OnGUI_BuildLogs(m_Compute);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.Box(GUIContent.none, GUIStyle.none);
            GUILayout.EndVertical();
            OnGUI_AsyncJob();
        }

        void OnGUI_Material()
        {
            OnGUI_Header(string.Format("Material: {0}, Shader: {1}", m_Material.name, m_Material.shader.name));

            OnGUI_ToolBar(BuildMaterialReport, m_Material, PlatformJob.BuildMaterialPerfReport);

            OnGUI_BuildLogs(m_Material);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.Box(GUIContent.none, GUIStyle.none);
            GUILayout.EndVertical();
            OnGUI_AsyncJob();
        }

        void OnGUI_ToolBar(Func<IAsyncJob> buildReportJob, Object asset, PlatformJob capability)
        {
            if (m_AssetMetadata == null)
                return;

            var assetGUID = ShaderAnalysisUtils.CalculateGUIDFor(asset);
            var genDir = ShaderAnalysisUtils.GetTemporaryDirectory(asset, m_CurrentPlatform);
            var report = m_AssetMetadata.GetReport(assetGUID);
            var reportReference = m_AssetMetadataReference != null ? m_AssetMetadataReference.GetReport(assetGUID) : null;

            GUILayout.BeginHorizontal();
            GUI.enabled = EditorShaderTools.DoesPlatformSupport(m_CurrentPlatform, capability);
            if (GUILayout.Button(UIUtils.Text("Build Report"), EditorStyles.toolbarButton))
            {
                m_CurrentJob = buildReportJob();
                if (m_CurrentJob != null)
                {
                    m_JobAssets[m_CurrentJob] = asset;
                    m_CurrentJob.OnComplete(OnBuildReportJobComplete);
                }
            }

            GUI.enabled = genDir.Exists;
            if (GUILayout.Button(UIUtils.Text("Open Temp Dir"), EditorStyles.toolbarButton))
                Application.OpenURL(genDir.FullName);

            GUI.enabled = report != null;
            if (GUILayout.Button(UIUtils.Text("Set as reference"), EditorStyles.toolbarButton))
                SetAsReference(m_CurrentPlatform, assetGUID, report);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = report != null && ExporterUtilities.IsValid(m_ReportExporterIndex);
            if (GUILayout.Button(UIUtils.Text("Export to"), EditorStyles.toolbarButton))
            {
                var tempReportFile = ShaderAnalysisUtils.GetTemporaryReportFile(asset, m_CurrentPlatform);
                var tempReportFileName = ExporterUtilities.ChangeExtensionFor(m_ReportExporterIndex, tempReportFile.FullName);
                ExporterUtilities.Export(m_ReportExporterIndex, report, tempReportFileName);
                Application.OpenURL(tempReportFile.FullName);
            }

            GUI.enabled = true;
            m_ReportExporterIndex =
                (ReportExporterIndex)EditorGUILayout.Popup((int)m_ReportExporterIndex,
                    ExporterUtilities.ReportExporterNames);

            GUI.enabled = report != null && reportReference != null && ExporterUtilities.IsValid(m_ReportDiffExporterIndex);
            if (GUILayout.Button(UIUtils.Text("Export diff to"), EditorStyles.toolbarButton))
            {
                var diff = ShaderAnalysisUtils.DiffReports(report, reportReference);
                var exportFile = ShaderAnalysisUtils.GetTemporaryDiffFile(assetGUID, m_CurrentPlatform);
                var exportReportFileName = ExporterUtilities.ChangeExtensionFor(m_ReportExporterIndex, exportFile.FullName);
                ExporterUtilities.ExportDiff(m_ReportDiffExporterIndex, diff, exportReportFileName);
                Application.OpenURL(exportFile.FullName);
            }
            GUI.enabled = true;
            m_ReportDiffExporterIndex =
                (ReportDiffExporterIndex)EditorGUILayout.Popup((int)m_ReportDiffExporterIndex,
                    ExporterUtilities.ReportDiffExporterNames);

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        void OnGUI_BuildLogs(Object asset)
        {
            if (m_AssetMetadata == null)
                return;

            var assetGUID = ShaderAnalysisUtils.CalculateGUIDFor(asset);
            var report = m_AssetMetadata.GetReport(assetGUID);
            if (report == null)
                return;

            var reportReference = m_AssetMetadataReference != null ? m_AssetMetadataReference.GetReport(assetGUID) : null;

            EditorGUILayout.LabelField(string.Format("Skipped passes: {0}", report.skippedPasses.Aggregate(string.Empty, (s, v) => s += " " + v.Value)));

            m_BuildScrollPosition = GUILayout.BeginScrollView(m_BuildScrollPosition);
            EditorGUILayout.LabelField(UIUtils.Text(String.Format("Passes: {0}, MultiCompiles: {1}", report.programs.Count, report.compileUnits.Count)));
            for (var i = 0 ; i < report.programs.Count ; ++i)
            {
                var program = report.programs[i];
                var programHash = ComputeProgramHash(i);
                var isProgramFoldout = m_BuildReportCache.GetBool(programHash);
                GUILayout.BeginHorizontal();
                isProgramFoldout = EditorGUILayout.Foldout(isProgramFoldout, UIUtils.Text(program.name), true);
                GUILayout.FlexibleSpace();

                if (program.compileErrors > 0)
                    GUILayout.Box(EditorGUIUtility.IconContent("console.erroricon"), GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (program.compileWarnings > 0)
                    GUILayout.Box(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                GetProgramToolbar(m_CurrentPlatform)(asset, report, program, reportReference, assetGUID);
                GUILayout.EndHorizontal();
                m_BuildReportCache.Set(programHash, isProgramFoldout);

                if (isProgramFoldout)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.LabelField(UIUtils.Text(string.Format("{0} multicompiles", program.multicompiles.Length)));
                    EditorGUILayout.LabelField(UIUtils.Text(string.Format("{0} multicompiles sets", program.multicompileCombinations.Length)));

                    foreach (var cu in program.compileUnits)
                    {
                        var pu = cu.performanceUnit;
                        if (pu == null)
                            continue;

                        var cuHash = ComputeCompileUnitHash(programHash, cu.multicompileIndex);

                        GUILayout.BeginHorizontal();
                        var multiCompileFoldout = m_BuildReportCache.GetBool(cuHash);
                        multiCompileFoldout = EditorGUILayout.Foldout(multiCompileFoldout, UIUtils.Text(string.Join(" ", cu.defines)), true);
                        GUILayout.FlexibleSpace();
                        GetUnitToolbar(m_CurrentPlatform)(asset, report, program, cu, pu, pu.parsedReport, reportReference, assetGUID);
                        m_BuildReportCache.Set(cuHash, multiCompileFoldout);

                        if (cu.errors.Length > 0)
                            GUILayout.Box(EditorGUIUtility.IconContent("console.erroricon"), GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        if (cu.warnings.Length > 0)
                            GUILayout.Box(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        GUILayout.EndHorizontal();

                        if (multiCompileFoldout)
                        {
                            ++EditorGUI.indentLevel;

                            var perfs = pu.parsedReport;
                            EditorGUILayout.LabelField(UIUtils.Text("Performances"));
                            EditorGUILayout.LabelField(UIUtils.Text("Shader microcode size: {0} bytes", perfs.microCodeSize));
                            EditorGUILayout.LabelField(UIUtils.Text("VGPR count: {0} ({1} used)", perfs.VGPRCount, perfs.VGPRUsedCount));
                            EditorGUILayout.LabelField(UIUtils.Text("SGPR count: {0} ({1} used)", perfs.SGPRCount, perfs.SGPRUsedCount));
                            EditorGUILayout.LabelField(UIUtils.Text("User SGPR count: {0}", perfs.UserSGPRCount));
                            EditorGUILayout.LabelField(UIUtils.Text("LDS Size: {0} bytes", perfs.LDSSize));
                            EditorGUILayout.LabelField(UIUtils.Text("Threadgroup waves: {0}", perfs.threadGroupWaves));
                            EditorGUILayout.LabelField(UIUtils.Text("CU Occupancy: {0}/{1}", perfs.CUOccupancyCount, perfs.CUOccupancyMax));
                            EditorGUILayout.LabelField(UIUtils.Text("SIMD Occupancy: {0}/{1}", perfs.SIMDOccupancyCount, perfs.SIMDOccupancyMax));

                            foreach (var error in cu.errors)
                            {
                                EditorGUILayout.HelpBox(error.message, MessageType.Error);
                                GUILayout.Box(UIUtils.Text(UIUtils.ClampText(string.Join("\n", error.stacktrace))), EditorStyles.helpBox);
                            }
                            foreach (var warning in cu.warnings)
                            {
                                EditorGUILayout.HelpBox(UIUtils.ClampText(warning.message), MessageType.Warning);
                                GUILayout.Box(UIUtils.Text(UIUtils.ClampText(string.Join("\n", warning.stacktrace))), EditorStyles.helpBox);
                            }
                            --EditorGUI.indentLevel;
                        }
                    }
                    --EditorGUI.indentLevel;
                }
            }
            GUILayout.EndScrollView();
        }

        static int ComputeCompileUnitHash(int programHash, int multicompileIndex)
        {
            return programHash + (int)((Mathf.Abs(multicompileIndex) + 1) * Mathf.Sign(multicompileIndex));
        }

        void OnGUI_Header(string title)
        {
            var loadAssetMetadata = false;
            var loadAssetMetadataReference = false;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            m_SelectedAsset = EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("To Inspect"), m_SelectedAsset,
                typeof(Object), false);
            if (EditorGUI.EndChangeCheck() && m_SelectedAsset != null && !m_SelectedAsset.Equals(null))
                OpenAsset(m_SelectedAsset);
            EditorGUI.BeginChangeCheck();

            m_SelectedPlatformIndex = EditorGUILayout.Popup(EditorGUIUtility.TrTempContent("Target Platform"), m_SelectedPlatformIndex,
                m_SupportedPlatformNames);
            m_CurrentPlatform = m_SelectedPlatformIndex >= 0 && m_SelectedPlatformIndex < m_SupportedPlatforms.Length ? m_SupportedPlatforms[m_SelectedPlatformIndex] : BuildTarget.StandaloneWindows;
            loadAssetMetadata = EditorGUI.EndChangeCheck();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(UIUtils.Text("Reference Folder: "), GUILayout.Width(200));
            EditorGUILayout.LabelField(UIUtils.Text(referenceFolderPath), EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(UIUtils.Text("Pick"), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                referenceFolderPath = EditorUtility.OpenFolderPanel("Choose the reference folder", "Reference folder", "ShaderAnalysisReference");
                loadAssetMetadataReference = true;
            }
            EditorGUILayout.LabelField(UIUtils.Text("Reference Source Folder: "), GUILayout.Width(200));
            EditorGUILayout.LabelField(UIUtils.Text(referenceSourceFolderPath), EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(UIUtils.Text("Pick"), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                referenceSourceFolderPath = EditorUtility.OpenFolderPanel("Choose the reference source folder", "Reference source folder", "ShaderAnalysisReference");
                loadAssetMetadataReference = true;
            }
            GUILayout.EndHorizontal();

            if (loadAssetMetadata)
                m_AssetMetadata = ShaderAnalysisUtils.LoadAssetMetadatasFor(m_CurrentPlatform);
            if (loadAssetMetadata || loadAssetMetadataReference)
                m_AssetMetadataReference = ShaderAnalysisUtils.LoadAssetMetadatasFor(m_CurrentPlatform, referenceFolder);
        }


        void OnGUI_PassSelection()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(UIUtils.Text("Pass name: "), GUILayout.Width(200));
            shaderPassToAnalyse = EditorGUILayout.TextField(shaderPassToAnalyse, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

        }

        void OnGUI_AsyncJob()
        {
            if (m_CurrentJob == null)
            {
                if (m_AutoRefreshRegistered)
                {
                    EditorApplication.update -= Repaint;
                    m_AutoRefreshRegistered = false;
                }
                return;
            }

            if (!m_AutoRefreshRegistered)
            {
                m_AutoRefreshRegistered = true;
                EditorApplication.update += Repaint;
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            var progressRect = GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, float.MaxValue);
            EditorGUI.ProgressBar(progressRect, m_CurrentJob.progress, m_CurrentJob.message);
            if (GUILayout.Button(UIUtils.Text("Cancel"), EditorStyles.toolbarButton))
                m_CurrentJob.Cancel();
            GUILayout.EndVertical();

            if (m_CurrentJob.IsComplete())
                m_CurrentJob = null;
        }

        void OnBuildReportJobComplete(IAsyncJob obj)
        {
            var asset = m_JobAssets[obj];
            m_JobAssets.Remove(obj);

            var job = obj as AsyncBuildReportJob;
            Assert.IsNotNull(job);

            ShaderBuildReport report = null;
            if (job.IsComplete()
                && job.hasReport
                && (report = job.builtReport) != null)
            {
                var metadata = ShaderAnalysisUtils.LoadAssetMetadatasFor(job.target);
                var assetGUID = ShaderAnalysisUtils.CalculateGUIDFor(asset);
                metadata.SetReport(assetGUID, report);
                ShaderAnalysisUtils.SaveAssetMetadata(metadata);
            }
        }

        void ResetUI()
        {
            m_BuildScrollPosition = Vector2.zero;
            m_BuildReportCache.Clear();
        }

        IAsyncJob BuildShaderReport()
        {
            return EditorShaderTools.GenerateBuildReportAsync(m_Shader, m_CurrentPlatform); ;
        }

        IAsyncJob BuildComputeShaderReport()
        {
            return EditorShaderTools.GenerateBuildReportAsync(m_Compute, m_CurrentPlatform); ;
        }

        IAsyncJob BuildMaterialReport()
        {
            return EditorShaderTools.GenerateBuildReportAsync(m_Material, m_CurrentPlatform); ;
        }

        void NOOPGUI()
        {
            m_SelectedAsset = EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("To Inspect"), m_SelectedAsset,
                typeof(Object), false);
            if (EditorGUI.EndChangeCheck() && m_SelectedAsset != null && !m_SelectedAsset.Equals(null))
                OpenAsset(m_SelectedAsset);
        }

        static void SetAsReference(BuildTarget buildTarget, string assetGUID, ShaderBuildReport report)
        {
            var metadatas = ShaderAnalysisUtils.LoadAssetMetadatasFor(buildTarget, referenceFolder);
            metadatas.SetReport(assetGUID, report);
            ShaderAnalysisUtils.SaveAssetMetadata(metadatas, referenceFolder);
        }

        static int ComputeProgramHash(int programIndex)
        {
            return (programIndex + 1) * 100000;
        }
    }
}

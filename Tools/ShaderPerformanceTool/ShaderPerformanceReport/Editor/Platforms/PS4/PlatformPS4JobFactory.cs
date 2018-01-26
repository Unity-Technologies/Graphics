using System;
using System.IO;
using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    class PlatformPS4JobFactory : IPlatformJobFactory
    {
        static SCUI s_SCUI = new SCUI();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorShaderTools.SetPlatformJobs(BuildTarget.PS4, new PlatformPS4JobFactory());
            EditorShaderPerformanceReportWindow.SetDrawProgramToolbar(BuildTarget.PS4, OnGUI_ProgramToolbar);
            EditorShaderPerformanceReportWindow.SetDrawUnitToolbar(BuildTarget.PS4, OnGUI_UnitToolbar);
            s_SCUI.Initialize();
        }

        PlatformJob m_Capabilities = PlatformJob.BuildShaderPerfReport 
            | PlatformJob.BuildComputeShaderPerfReport
            | PlatformJob.BuildMaterialPerfReport;
        public PlatformJob capabilities { get { return m_Capabilities; } }

        public IAsyncJob CreateBuildReportJob(Shader shader)
        {
            return new BuildReportPS4JobAsync(BuildTarget.PS4, shader);
        }

        public IAsyncJob CreateBuildReportJob(ComputeShader compute)
        {
            return new BuildReportPS4JobAsync(BuildTarget.PS4, compute);
        }

        public IAsyncJob CreateBuildReportJob(Material material)
        {
            return new BuildReportPS4JobAsync(BuildTarget.PS4, material);
        }

        static void OnGUI_ProgramToolbar(
            Object asset, 
            ShaderBuildReport report, 
            ShaderBuildReport.GPUProgram program,
            ShaderBuildReport reference,
            string assetGUID)
        {
            GUI.enabled = !string.IsNullOrEmpty(program.sourceCode);
            if (GUILayout.Button("Open", EditorStyles.toolbarButton))
                program.OpenSourceCode();
               
            GUI.enabled = true;
        }

        static void OnGUI_UnitToolbar(
            Object asset, 
            ShaderBuildReport r, 
            ShaderBuildReport.GPUProgram po, 
            ShaderBuildReport.CompileUnit cu, 
            ShaderBuildReport.PerformanceUnit pu, 
            ShaderBuildReport.ProgramPerformanceMetrics p,
            ShaderBuildReport reference,
            string assetGUID)
        {
            if (po == null)
                return;

            var rpo = reference.GetProgramByName(po.name);
            var rcu = rpo != null ? rpo.GetCompileUnitByDefines(cu.defines) : null;
            var rdir = EditorShaderPerformanceReportWindow.referenceSourceFolder;

            if (GUILayout.Button(UIUtils.Text("SCUI"), EditorStyles.toolbarButton))
                cu.OpenInSCUI();
            GUI.enabled = rcu != null && rdir != null;
            if (GUILayout.Button(UIUtils.Text("SCUI Diff"), EditorStyles.toolbarButton))
                PS4Utility.DiffCUInSCUI(cu, PS4Utility.currentSourceDir, rcu, rdir);
            GUI.enabled = true;
        }
    }
}

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.Rendering
{
    class StrippingReportLifeTime : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {
        public int callbackOrder => -1;

        public void OnPreprocessBuild(BuildReport report)
        {
            ShaderStrippingReport.InitializeReport();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            ShaderStrippingReport.DumpReport();
        }
    }
}

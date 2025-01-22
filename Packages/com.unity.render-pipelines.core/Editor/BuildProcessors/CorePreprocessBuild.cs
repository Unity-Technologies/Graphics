using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.Rendering
{
    // Make CoreBuildData constructed and keep the instance until the end of build
    class CorePreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => int.MinValue + 50;

        private static CoreBuildData m_BuildData = null;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            m_BuildData?.Dispose();
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            m_BuildData = new CoreBuildData(EditorUserBuildSettings.activeBuildTarget, isDevelopmentBuild);
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            m_BuildData?.Dispose();
            m_BuildData = null;
        }
    }
}

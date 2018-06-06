using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

class HDRPPreprocessBuild : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        // Don't execute the preprocess if we are not HDRenderPipeline
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp == null)
            return;

        // Note: If you add new platform in this function, think about adding support in IsSupportedPlatform() function in HDRenderPipeline.cs

        // If platform is supported all good
        if (report.summary.platform == BuildTarget.StandaloneWindows ||
            report.summary.platform == BuildTarget.StandaloneWindows64 ||
            report.summary.platform == BuildTarget.StandaloneLinux64 ||
            report.summary.platform == BuildTarget.StandaloneLinuxUniversal ||
            report.summary.platform == BuildTarget.StandaloneOSX ||
            report.summary.platform == BuildTarget.XboxOne ||
            report.summary.platform == BuildTarget.PS4 ||
            report.summary.platform == BuildTarget.Switch)
        {
            return;
        }

        string msg = "The platform " + report.summary.platform.ToString() + " is not supported with Hight Definition Render Pipeline";

        // Throw an exception to stop the build
        throw new BuildFailedException(msg);
    }
}

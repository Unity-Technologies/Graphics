using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class URPPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var urpPipelineAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

            if (urpPipelineAsset == null)
                return;

            //ensure global settings exist and at last version
            if (UniversalRenderPipelineGlobalSettings.instance == null)
                throw new BuildFailedException("There is currently no UniversalRenderPipelineGlobalSettings in use. Please go to Project Settings > Graphics > URP Global Settings and fix any possible issues.");
        }
    }
}

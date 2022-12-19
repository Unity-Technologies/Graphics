using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalRenderPipelineGlobalSettingsPostprocessor : AssetPostprocessor
    {
        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        static void OnPostprocessAllAssets(string[] importedAssets , string[] __, string[] ___, string[] ____, bool didDomainReload)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return;

            if(didDomainReload || importedAssets.Contains(k_GraphicsSettingsPath))
                UniversalRenderPipelineGlobalSettings.Ensure();
        }
    }
}

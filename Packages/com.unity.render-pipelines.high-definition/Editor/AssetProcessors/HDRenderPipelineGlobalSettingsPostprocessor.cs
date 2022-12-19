using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRenderPipelineGlobalSettingsPostprocessor : AssetPostprocessor
    {
        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        static void OnPostprocessAllAssets(string[] importedAssets , string[] __, string[] ___, string[] ____, bool didDomainReload)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return;

            if(didDomainReload || importedAssets.Contains(k_GraphicsSettingsPath))
                HDRenderPipelineGlobalSettings.Ensure();
        }
    }
}

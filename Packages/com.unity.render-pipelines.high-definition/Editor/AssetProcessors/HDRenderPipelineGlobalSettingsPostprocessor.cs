using System.Linq;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    //We ensure and save GS after each domain reload because we need to make them valid for AssetImportWorkers.
    //They will create render pipeline separately but they can't migrate or create new Assets.
    class HDRenderPipelineGlobalSettingsPostprocessor : AssetPostprocessor
    {
        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        static void OnPostprocessAllAssets(string[] importedAssets , string[] __, string[] ___, string[] ____, bool didDomainReload)
        {
            if (didDomainReload || importedAssets.Contains(k_GraphicsSettingsPath))
                HDRenderPipelineGlobalSettings.Ensure();
        }
    }
}

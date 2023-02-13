using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class UniversalGlobalSettingsPanelProvider : RenderPipelineGlobalSettingsProvider<UniversalRenderPipeline, UniversalRenderPipelineGlobalSettings>
    {
        public UniversalGlobalSettingsPanelProvider()
            : base("Project/Graphics/URP Global Settings")
        {
            keywords = GetSearchKeywordsFromGUIContentProperties<UniversalRenderPipelineGlobalSettingsUI.Styles>().ToArray();
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() => new UniversalGlobalSettingsPanelProvider();

        #region RenderPipelineGlobalSettingsProvider

        protected override void Ensure()
        {
            UniversalRenderPipelineGlobalSettings.Ensure();
        }
        #endregion
    }
}

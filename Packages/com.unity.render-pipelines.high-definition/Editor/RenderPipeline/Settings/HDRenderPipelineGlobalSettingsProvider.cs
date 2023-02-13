using System.Linq;
using UnityEditor.VFX.HDRP;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class HDRenderPipelineGlobalSettingsPanelProvider : RenderPipelineGlobalSettingsProvider<HDRenderPipeline, HDRenderPipelineGlobalSettings>
    {
        public HDRenderPipelineGlobalSettingsPanelProvider()
            : base("Project/Graphics/HDRP Global Settings")
        {
            keywords = GetSearchKeywordsFromGUIContentProperties<HDRenderPipelineGlobalSettingsUI.Styles>().ToArray();
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() => new HDRenderPipelineGlobalSettingsPanelProvider();

        internal static bool needRefreshVfxErrors = false;

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            VFXHDRPSettingsUtility.RefreshVfxErrorsIfNeeded(ref needRefreshVfxErrors);
        }

        #region RenderPipelineGlobalSettingsProvider

        protected override void Ensure()
        {
            HDRenderPipelineGlobalSettings.Ensure();
        }
        #endregion
    }
}

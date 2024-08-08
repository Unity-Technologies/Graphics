using System.Linq;
using UnityEditor.VFX.HDRP;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
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

        protected override void Clone(RenderPipelineGlobalSettings src, bool activateAsset)
        {
            HDAssetFactory.HDRenderPipelineGlobalSettingsCreator.Clone(src as HDRenderPipelineGlobalSettings, assignToActiveAsset: activateAsset);
        }

        protected override void Create(bool useProjectSettingsFolder, bool activateAsset)
        {
            HDAssetFactory.HDRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: useProjectSettingsFolder, assignToActiveAsset: activateAsset);
        }

        protected override void Ensure()
        {
            HDRenderPipelineGlobalSettings.Ensure();
        }
        #endregion
    }
}

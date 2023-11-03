using System.Collections.Generic;

namespace UnityEngine.Rendering.DummyPipeline
{
    [SupportedOnRenderPipeline(typeof(DummyPipelineAsset))]
    [System.ComponentModel.DisplayName("Dummy")]
    public class DummyGlobals : RenderPipelineGlobalSettings<DummyGlobals, DummyPipeline>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
    }
}

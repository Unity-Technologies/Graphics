using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [SupportedOnRenderPipeline(typeof(DummyRenderPipelineAsset))]
    class DummyRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>
    {
        internal static string defaultPath => "Assets/Tests/DummyRenderPipelineGlobalSettings.asset";

        public bool initializedCalled = false;

        protected override List<IRenderPipelineGraphicsSettings> settingsList { get; } = new();

        public override void Initialize(RenderPipelineGlobalSettings source = null)
        {
            initializedCalled = true;
        }
    }
}

using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [SupportedOnRenderPipeline(typeof(DummyRenderPipelineAsset))]
    public class DummyRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<DummyRenderPipelineGlobalSettings, DummyRenderPipeline>
    {
        internal static string defaultPath => "Assets/Tests/DummyRenderPipelineGlobalSettings.asset";

        public bool initializedCalled = false;

        public override void Initialize(RenderPipelineGlobalSettings source = null)
        {
            initializedCalled = true;
        }
    }
}

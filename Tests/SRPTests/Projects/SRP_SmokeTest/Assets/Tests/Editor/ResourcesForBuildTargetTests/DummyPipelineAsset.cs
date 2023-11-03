namespace UnityEngine.Rendering.DummyPipeline
{
    //[CreateAssetMenu(fileName = "DummyRPAsset", menuName = "Dummy/RPAsset", order = 1)]
    public class DummyPipelineAsset : RenderPipelineAsset<DummyPipeline>
    {
        protected override RenderPipeline CreatePipeline() => new DummyPipeline();
    }
}

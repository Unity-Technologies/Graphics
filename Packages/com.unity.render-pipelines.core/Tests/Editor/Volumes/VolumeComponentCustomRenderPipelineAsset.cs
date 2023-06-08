namespace UnityEngine.Rendering.Tests
{
    [HideInInspector]
    [VolumeComponentMenu("Supported On Tests/Not Specified Pipeline Supported On")]
    [SupportedOnRenderPipeline(typeof(CustomRenderPipelineAsset))]
    public class VolumeComponentCustomRenderPipelineAsset : VolumeComponent
    {
    }
}

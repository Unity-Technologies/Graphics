using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    [VolumeComponentMenu("SupportedOnTests/SupportedOnCustomSRP")]
    [SupportedOnRenderPipeline(typeof(CustomRenderPipelineAsset))]
    public class VolumeComponentSupportedOnCustomSRP : VolumeComponent
    {
    }
}

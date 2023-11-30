using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    [VolumeComponentMenu("SupportedOnTests/SupportedOnCustomSRP")]
    [SupportedOnRenderPipeline(typeof(CustomRenderPipelineAsset))]
    class VolumeComponentSupportedOnCustomSRP : VolumeComponent
    {
    }
}

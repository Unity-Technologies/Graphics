using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

//#if ENABLE_HYBRID_RENDERER_V2 && UNITY_2020_1_OR_NEWER && (HDRP_9_0_0_OR_NEWER || URP_9_0_0_OR_NEWER)
namespace Scenes.TestDuplicateProperties
{
    [GenerateAuthoringComponent]
    [MaterialProperty("_DuplicateColor", MaterialPropertyFormat.Float4)]
    public struct DuplicateTestColorB : IComponentData
    {
        public float4 Value;
    }
}
//#endif

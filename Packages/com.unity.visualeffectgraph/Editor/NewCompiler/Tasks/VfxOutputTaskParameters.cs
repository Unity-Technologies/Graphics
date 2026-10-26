using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    static class VfxOutputTaskParameters
    {
        public static readonly NameDataKey IndirectDrawKey = new("indirectDraw");
        public static readonly NameDataKey IndirectIndexKey = new("indirectIndex");
        public static readonly NameDataKey IndirectPerCameraKey = new("indirectPerCamera");
    }
}

using System.Reflection;
using UnityEngine.Graphing;


namespace UnityEngine.MaterialGraph
{
    [Title("Normal/Unpack Normal")]
    internal class UnpackNormalNode : CodeFunctionNode
    {
        public UnpackNormalNode()
        {
            name = "UnpackNormal";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_UnpackNormal", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Sign(
            [Slot(0, Binding.None)] Vector4 packedNormal,
            [Slot(1, Binding.None)] out Vector3 normal)
        {
            normal = Vector3.up;
            return
                @"
{
    normal = UnpackNormal(packedNormal);
}
";
        }
    }
}

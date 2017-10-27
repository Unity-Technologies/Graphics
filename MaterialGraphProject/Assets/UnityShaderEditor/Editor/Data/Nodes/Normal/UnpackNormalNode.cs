using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;


namespace UnityEditor.ShaderGraph
{
    [Title("Normal/Unpack Normal")]
    internal class UnpackNormalNode : CodeFunctionNode
    {
        public UnpackNormalNode()
        {
            name = "UnpackNormal";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_UnpackNormal", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_UnpackNormal(
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

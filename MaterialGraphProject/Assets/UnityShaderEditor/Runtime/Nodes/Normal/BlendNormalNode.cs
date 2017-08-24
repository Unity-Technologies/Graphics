using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Normal/Blend Normal")]
    public class BlendNormalNode : CodeFunctionNode
    {
        public BlendNormalNode()
        {
            name = "BlendNormal";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Blendnormal", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Blendnormal(
            [Slot(0, Binding.None)] Vector3 first,
            [Slot(1, Binding.None)] Vector3 second,
            [Slot(2, Binding.None)] out Vector3 result)
        {
            result = Vector3.one;

            return @"
{
    result = normalize({precision}3(first.rg + second.rg, first.b * second.b));
}
";
        }
    }
}

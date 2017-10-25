using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Reflect")]
    class ReflectNode : CodeFunctionNode
    {
        public ReflectNode()
        {
            name = "Reflect";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Reflect", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Reflect(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Normal,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;

            return @"
{
    Out = reflect(In, Normal);
}";
        }
    }
}

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
            [Slot(0, Binding.None)] Vector3 normal,
            [Slot(1, Binding.None)] Vector3 direction,
            [Slot(2, Binding.None)] out Vector3 reflection)
        {
            reflection = Vector3.one;

            return @"
{
    {precision}3 vn = normalize(normal);
    {precision}3 vd = normalize(direction);
    reflection =  2 * dot(vn, vd) * vn - vd, 1.0;
}";
        }
    }
}

using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Repeating Dot")]
    public class RepeatingDotNode : CodeFunctionNode
    {
        public RepeatingDotNode()
        {
            name = "RepeatingDot";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Repreatingdot", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Repreatingdot(
            [Slot(0, Binding.None)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 count,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    uv *= 2.0 - 1.0;
    uv = fmod(uv * count, 1.0) * 2.0 - 1.0;
    result = length(uv);
}";
        }
    }
}

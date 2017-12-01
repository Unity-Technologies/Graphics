using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/MakeSubgraph/Repeating Dot")]
    public class RepeatingDotNode : CodeFunctionNode
    {
        public RepeatingDotNode()
        {
            name = "Repeating Dot";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Repreatingdot", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Repreatingdot(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 4.0f, 4.0f, 4.0f, 4.0f)] Vector1 Count,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector1 Radius,
            [Slot(4, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    UV *= 2.0 - 1.0;
    UV = fmod(UV * Count, 1.0) * 2.0 - 1.0;
    Out = step(length(UV),Radius);
}";
        }
    }
}

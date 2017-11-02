using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/Particle")]
    public class ParticleNode : CodeFunctionNode
    {
        public ParticleNode()
        {
            name = "Particle";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Particle", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Particle(
            [Slot(0, Binding.None)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 scaleFactor,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    uv = uv * 2.0 - 1.0;;
    result = abs(1.0/length(uv * scaleFactor));
}
";
        }
    }
}

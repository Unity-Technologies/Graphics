using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "MakeSubgraph", "Particle")]
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
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None, 100f, 100f, 100f, 100f)] Vector1 Sharpness,
            [Slot(4, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    UV *= 2.0 - 1.0;
    Out = abs(1.0/length((UV-Center) * Sharpness));
}
";
        }
    }
}*/

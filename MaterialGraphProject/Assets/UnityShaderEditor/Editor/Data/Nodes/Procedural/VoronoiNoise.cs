using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/Voronoi Noise")]
    public class VoronoiNoiseNode : CodeFunctionNode
    {
        public VoronoiNoiseNode()
        {
            name = "VoronoiNoise";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_VoronoiNoise", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_VoronoiNoise(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 2.0f, 0, 0, 0)] Vector1 angleOffset,
            [Slot(2, Binding.None)] out Vector1 n1,
            [Slot(2, Binding.None)] out Vector1 n2,
            [Slot(2, Binding.None)] out Vector1 n3)
        {
            return
                @"
{
    float2 g = floor(uv);
    float2 f = frac(uv);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, angleOffset);
            float d = distance(lattice + offset, f);

            if(d < res.x)
            {

                res = float3(d, offset.x, offset.y);
                n1 = res.x;
                n2 = res.y;
                n3 = 1.0 - res.x;

            }
        }

    }
}
";
        }

        public override void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var preamble = @"
inline float2 unity_voronoi_noise_randomVector (float2 uv, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    uv = frac(sin(mul(uv, m)) * 46839.32);
    return float2(sin(uv.y*+offset)*0.5+0.5, cos(uv.x*offset)*0.5+0.5);
}
";
            visitor.AddShaderChunk(preamble, true);
            base.GenerateNodeFunction(visitor, generationMode);
        }
    }
}

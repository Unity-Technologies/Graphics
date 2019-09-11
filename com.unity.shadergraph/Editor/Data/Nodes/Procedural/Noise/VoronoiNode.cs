using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.VoronoAbstractMaterialNode")]
    [Title("Procedural", "Noise", "Voronoi")]
    class VoronoiNode : CodeFunctionNode
    {
        public VoronoiNode()
        {
            name = "Voronoi";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Voronoi", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Voronoi(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 2.0f, 0, 0, 0)] Vector1 AngleOffset,
            [Slot(2, Binding.None, 5.0f, 5.0f, 5.0f, 5.0f)] Vector1 CellDensity,
            [Slot(3, Binding.None)] out Vector1 Out,
            [Slot(4, Binding.None)] out Vector1 Cells)
        {
            return
                @"
{
    $precision2 g = floor(UV * CellDensity);
    $precision2 f = frac(UV * CellDensity);
    $precision t = 8.0;
    $precision3 res = $precision3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            $precision2 lattice = $precision2(x,y);
            $precision2 offset = Unity_Voronoi_RandomVector_$precision(lattice + g, AngleOffset);
            $precision d = distance(lattice + offset, f);

            if(d < res.x)
            {
                res = $precision3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
            }
        }
    }
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction($"Unity_Voronoi_RandomVector_{concretePrecision.ToShaderString()}", s => s.Append(@"
inline $precision2 Unity_Voronoi_RandomVector_$precision ($precision2 UV, $precision offset)
{
    $precision2x2 m = $precision2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return $precision2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}
"));
            base.GenerateNodeFunction(registry, generationMode);
        }
    }
}

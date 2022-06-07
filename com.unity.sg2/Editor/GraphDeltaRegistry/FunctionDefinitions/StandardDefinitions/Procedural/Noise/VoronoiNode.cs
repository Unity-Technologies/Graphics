using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class VoronoiNode : IStandardNode
    {
        static string Name = "Voronoi";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "VoronoiDeterministic",
@"	g = floor(UV * CellDensity);
	f = frac(UV * CellDensity);
	for (int y = -1; y <= 1; y++)
	{
		for (int x = -1; x <= 1; x++)
		{
			lattice.x = x; lattice.y = y;
			offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
			d = distance(lattice + offset, f);
			if (d < res.x)
			{
				res.x = d;
				res.y = offset.x;
				res.z = offset.y;
				Out = d;
				Cells = offset.x;
			}
		}
	}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("AngleOffset", TYPE.Float, Usage.In, new float[] {2f}),
                    new ParameterDescriptor("CellDensity", TYPE.Float, Usage.In, new float[] {5f}),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("Cells", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("g", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("res", TYPE.Vec3, Usage.Local, new float[] {8.0f, 0.0f, 0.0f}),
                    new ParameterDescriptor("lattice", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("offset", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("d", TYPE.Float, Usage.Local)
/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
	Hash_Tchou_2_2_float(UV, UV);
	return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}
*/
                ),
                new(
                    1,
                    "VoronoiLegacySine",
@"	g = floor(UV * CellDensity);
	f = frac(UV * CellDensity);
	for (int y = -1; y <= 1; y++)
	{
		for (int x = -1; x <= 1; x++)
		{
			lattice.x = x; lattice.y = y;
			offset = Unity_Voronoi_RandomVector_LegacySine_float(lattice + g, AngleOffset);
			d = distance(lattice + offset, f);
			if (d < res.x)
			{
				res.x = d;
				res.y = offset.x;
				res.z = offset.y;
				Out = d;
				Cells = offset.x;
			}
		}
	}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("AngleOffset", TYPE.Float, Usage.In, new float[] {2f}),
                    new ParameterDescriptor("CellDensity", TYPE.Float, Usage.In, new float[] {5f}),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("Cells", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("g", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("res", TYPE.Vec3, Usage.Local, new float[] {8.0f, 0.0f, 0.0f}),
                    new ParameterDescriptor("lattice", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("offset", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("d", TYPE.Float, Usage.Local)
/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

float2 Unity_Voronoi_RandomVector_LegacySine_float (float2 UV, float offset)
{
    Hash_LegacySine_2_2_float(UV, UV);
    return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}
*/
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a cell noise pattern using ranomly-placed points as cell centers",
            categories: new string[2] { "Procedural", "Noise" },
            synonyms: new string[1] { "worley noise" },
            selectableFunctions: new()
            {
                { "VoronoiDeterministic", "Deterministic" },
                { "VoronoiLegacySine", "Legacy Sine" }
            },
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the coordinates used to create the noise",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "AngleOffset",
                    displayName:"Angle Offset",
                    tooltip: "offset value for cell center points"
                ),
                new ParameterUIDescriptor(
                    name: "CellDensity",
                    displayName:"Cell Density",
                    tooltip: "scale of generated cells"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a cell noise pattern using ranomly-placed points as cell centers"
                ),
                new ParameterUIDescriptor(
                    name: "Cells",
                    tooltip: "raw cell data"
                )
            }
        );
    }
}

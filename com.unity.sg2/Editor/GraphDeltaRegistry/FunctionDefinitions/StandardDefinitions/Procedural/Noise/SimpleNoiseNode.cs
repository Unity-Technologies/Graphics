using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SimpleNoiseNode : IStandardNode
    {
        static string Name = "SimpleNoise";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Unity_SimpleNoise_ValueNoise_Deterministic",
@"	i = floor(uv);
	f = frac(uv);
	f = f * f * (3.0 - 2.0 * f);
	uv = abs(frac(uv) - 0.5);
	c1 = i; c1.x += 1;
	c2 = i; c2.y += 1;
	Hash_Tchou_2_1_float(i, r0);//TODO: Call either the float version or the half version depending on precision
	Hash_Tchou_2_1_float(c1, r1);
	Hash_Tchou_2_1_float(c2, r2);
	Hash_Tchou_2_1_float(i + 1, r3);
	Out = lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("uv", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("c1", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("c2", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("r0", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("r1", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("r2", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("r3", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
                    }
                ),
                new(
                    "SimpleNoiseDeterministic",
@"  Unity_SimpleNoise_ValueNoise_Deterministic(UV.xy*Scale, sample1);
    Out = sample1 * 0.125;
    Unity_SimpleNoise_ValueNoise_Deterministic(UV.xy*(Scale * 0.5), sample2);
	Out += sample2 * 0.25;
    Unity_SimpleNoise_ValueNoise_Deterministic(UV.xy*(Scale * 0.25), sample3);
	Out += *0.5;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {500f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("sample1", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("sample2", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("sample3", TYPE.Float, Usage.Local)
                    }
                ),
                new(
                    "Unity_SimpleNoise_ValueNoise_LegacySine",
@"	i = floor(uv);
	f = frac(uv);
	f = f * f * (3.0 - 2.0 * f);
	uv = abs(frac(uv) - 0.5);
	c1 = i; c1.x += 1;
	c2 = i; c2.y += 1;
	Hash_LegacySine_2_1_float(i, r0);
	Hash_LegacySine_2_1_float(c1, r1);
	Hash_LegacySine_2_1_float(c2, r2);
	Hash_LegacySine_2_1_float(i + 1, r3);
	Out = lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("uv", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("c1", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("c2", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("r0", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("r1", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("r2", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("r3", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
                    }
                ),
                new(
                    "SimpleNoiseLegacySine",
@"  Unity_SimpleNoise_ValueNoise_LegacySine(UV.xy*Scale, sample1);
    Out = sample1 * 0.125;
    Unity_SimpleNoise_ValueNoise_LegacySine(UV.xy*(Scale * 0.5), sample2);
	Out += sample2 * 0.25;
    Unity_SimpleNoise_ValueNoise_LegacySine(UV.xy*(Scale * 0.25), sample3);
	Out += *0.5;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {500f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("sample1", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("sample2", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("sample3", TYPE.Float, Usage.Local)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a smooth, non-tiling noise pattern using a point lattice",
            categories: new string[2] { "Procedural", "Noise" },
            synonyms: new string[1] { "value noise" },
            selectableFunctions: new()
            {
                { "SimpleNoiseDeterministic", "Deterministic" },
                { "SimpleNoiseLegacySine", "Legacy Sine" }
            },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the coordinates used to create the noise",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "the size of the noise"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a smooth, non-tiling noise pattern using a point lattice"
                )
            }
        );
    }
}

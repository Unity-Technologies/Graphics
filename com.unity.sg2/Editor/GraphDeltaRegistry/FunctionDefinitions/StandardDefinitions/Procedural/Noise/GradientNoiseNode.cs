using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class GradientNoiseNode : IStandardNode
    {
        static string Name = "GradientNoise";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "GradientNoiseDeterministic",
@"	p = UV * Scale;
	ip = floor(p);
	fp = frac(p);
	d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
	d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
	d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
	d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
	fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
	Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {10f}),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("p", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("ip", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("fp", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("d00", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("d01", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("d10", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("d11", TYPE.Float, Usage.Local)
/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
{
	float x; Hash_Tchou_2_1_float(p, x);
	return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}
*/
                ),
                new(
                    1,
                    "GradientNoiseLegacyMod",
@"	p = UV * Scale;
	ip = floor(p);
	fp = frac(p);
	d00 = dot(Unity_GradientNoise_LegacyMod_Dir_float(ip), fp);
	d01 = dot(Unity_GradientNoise_LegacyMod_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
	d10 = dot(Unity_GradientNoise_LegacyMod_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
	d11 = dot(Unity_GradientNoise_LegacyMod_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
	fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
	Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {10f}),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("p", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("ip", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("fp", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("d00", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("d01", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("d10", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("d11", TYPE.Float, Usage.Local)
/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

float2 Unity_GradientNoise_LegacyMod_Dir_float(float2 p)
{
	float x; Hash_LegacyMod_2_1_float(p, x);
	return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}
*/
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a smooth, non-tiling noise pattern using a gradient lattice",
            categories: new string[2] { "Procedural", "Noise" },
            synonyms: new string[1] { "perlin noise" },
            selectableFunctions: new()
            {
                { "GradientNoiseDeterministic", "Deterministic" },
                { "GradientNoiseLegacyMod", "Legacy Mod" }
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
                    tooltip: "a smooth, non-tiling noise pattern using a gradient lattice"
                )
            }
        );
    }
}

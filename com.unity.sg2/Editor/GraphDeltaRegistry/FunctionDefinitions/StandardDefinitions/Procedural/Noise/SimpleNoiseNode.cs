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
            new FunctionDescriptor[] {
                new(
                    1,
                    "SimpleNoiseDeterministic",
@"	Out = Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale)))*0.125;
	Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale * 0.5)))*0.25;
	Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale * 0.25)))*0.5;",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {500f}),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

float Unity_SimpleNoise_ValueNoise_Deterministic_float(float2 uv)
{
	float2 i = floor(uv);
	float2 f = frac(uv);
	f = f * f * (3.0 - 2.0 * f);
	uv = abs(frac(uv) - 0.5);
	float2 c0 = i + float2(0.0, 0.0);
	float2 c1 = i + float2(1.0, 0.0);
	float2 c2 = i + float2(0.0, 1.0);
	float2 c3 = i + float2(1.0, 1.0);
	float r0; Hash_Tchou_2_1_float(c0, r0);
	float r1; Hash_Tchou_2_1_float(c1, r1);
	float r2; Hash_Tchou_2_1_float(c2, r2);
	float r3; Hash_Tchou_2_1_float(c3, r3);
	return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
}
*/
                ),
                new(
                    1,
                    "SimpleNoiseLegacySine",
@"	Out = Unity_SimpleNoise_ValueNoise_LegacySine_float(float2(UV.xy*(Scale)))*0.125;
	Out += Unity_SimpleNoise_ValueNoise_LegacySine_float(float2(UV.xy*(Scale * 0.5)))*0.25;
	Out += Unity_SimpleNoise_ValueNoise_LegacySine_float(float2(UV.xy*(Scale * 0.25)))*0.5;",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {500f}),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

float Unity_SimpleNoise_ValueNoise_LegacySine_float(float2 uv)
{
	float2 i = floor(uv);
	float2 f = frac(uv);
	f = f * f * (3.0 - 2.0 * f);
	uv = abs(frac(uv) - 0.5);
	float2 c0 = i + float2(0.0, 0.0);
	float2 c1 = i + float2(1.0, 0.0);
	float2 c2 = i + float2(0.0, 1.0);
	float2 c3 = i + float2(1.0, 1.0);
	float r0; Hash_LegacySine_2_1_float(c0, r0);
	float r1; Hash_LegacySine_2_1_float(c1, r1);
	float r2; Hash_LegacySine_2_1_float(c2, r2);
	float r3; Hash_LegacySine_2_1_float(c3, r3);
	return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
}
*/
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

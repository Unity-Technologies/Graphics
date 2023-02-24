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
            "Deterministic",
            functions: new FunctionDescriptor[] {
                new(
                    "Unity_GradientNoise_Deterministic_Dir",
@"	Hash_Tchou_2_1_float(p, x); //TODO: Call either the float version or the half version depending on precision
    Out.x = x - floor(x + 0.5); Out.y = abs(x) - 0.5;
	Out = normalize(Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    isHelper: true
                ),
                new(
                    "Deterministic",
@"	p = UV * Scale;
	ip = floor(p);
	fp = frac(p);
    ip2 = ip; ip2.y += 1; fp2 = fp; fp2.y -= 1;
    ip3 = ip; ip3.x += 1; fp3 = fp; fp3.x -= 1;
    Unity_GradientNoise_Deterministic_Dir(ip, d00Out); d00 = dot(d00Out, fp);
    Unity_GradientNoise_Deterministic_Dir(ip2, d01Out); d01 = dot(d01Out, fp2);
    Unity_GradientNoise_Deterministic_Dir(ip3, d10Out); d10 = dot(d10Out, fp3);
    Unity_GradientNoise_Deterministic_Dir(ip + 1, d11Out); d11 = dot(d11Out, fp - 1);
	fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
	Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {10f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("ip", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("fp", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("ip2", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("fp2", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("ip3", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("fp3", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d00", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d01", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d10", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d11", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d00Out", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d01Out", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d10Out", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d11Out", TYPE.Vec2, Usage.Local)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new(
                    "Unity_GradientNoise_LegacyMod_Dir",
@"	Hash_LegacyMod_2_1_float(p, x); //TODO: Call either the float version or the half version depending on precision
    Out.x = x - floor(x + 0.5); Out.y = abs(x) - 0.5;
	Out = normalize(Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                    },
                    isHelper: true
                ),
                new(
                    "LegacyMod",
@"	p = UV * Scale;
	ip = floor(p);
	fp = frac(p);
    ip2 = ip; ip2.y += 1; fp2 = fp; fp2.y -= 1;
    ip3 = ip; ip3.x += 1; fp3 = fp; fp3.x -= 1;
    Unity_GradientNoise_LegacyMod_Dir(ip, d00Out); d00 = dot(d00Out, fp);
    Unity_GradientNoise_LegacyMod_Dir(ip2, d01Out); d01 = dot(d01Out, fp2);
    Unity_GradientNoise_LegacyMod_Dir(ip3, d10Out); d10 = dot(d10Out, fp3);
    Unity_GradientNoise_LegacyMod_Dir(ip + 1, d11Out); d11 = dot(d11Out, fp - 1);
	fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
	Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] {10f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("ip", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("fp", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("ip2", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("fp2", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("ip3", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("fp3", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d00", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d01", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d10", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d11", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("d00Out", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d01Out", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d10Out", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d11Out", TYPE.Vec2, Usage.Local)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a smooth, non-tiling noise pattern using a gradient lattice",
            category: "Procedural/Noise",
            displayName: "Gradient Noise",
            synonyms: new string[1] { "perlin noise" },
            description: "pkg://Documentation~/previews/GradientNoise.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Deterministic", "Deterministic" },
                { "LegacyMod", "Legacy Mod" }
            },
            hasModes: true,
            functionSelectorLabel: "Hash Type",
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

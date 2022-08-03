using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalBlendNode : IStandardNode
    {
        public static string Name => "NormalBlend";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
@"    Out.rg = A.rg + B.rg;
	Out.b = A.b * B.b;
	Out = SafeNormalize(Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("B", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Reoriented",
@"    t += A;
    u *= B;
    Out = (t / t.z) * dot(t, u) - u;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("B", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("t", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("u", TYPE.Vec3, GraphType.Usage.Local, new float[] { -1.0f, -1.0f, 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "DefaultO",
@"    Out.rg = A.rg + B.rg;
	Out.b = A.b * B.b;
	Out = lerp(A, SafeNormalize(Out), Opacity);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("B", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "ReorientedO",
@"    t += A;
    u *= B;
    Out = lerp( A, ((t / t.z) * dot(t, u) - u), Opacity);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("B", TYPE.Vec3, GraphType.Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("t", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("u", TYPE.Vec3, GraphType.Usage.Local, new float[] { -1.0f, -1.0f, 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "combines two normals together",
            category: "Artistic/Normal",
            synonyms: new string[5] { "RNM", "whiteout", "blend", "mix", "combine" },
            displayName: "Normal Blend",
            selectableFunctions: new()
            {
                { "Default", "Whiteout" },
                { "Reoriented", "Reoriented" },
                { "DefaultO", "Whiteout Opacity" },
                { "ReorientedO", "Reoriented Opacity" }
            },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the base normal"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the overlay normal"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a combination of both normals"
                )
            }
        );
    }
}

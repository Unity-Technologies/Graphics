using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SmoothstepNode : IStandardNode
    {
        public static string Name = "Smoothstep";
        public static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Smooth",
                    "Out = smoothstep(Edge1, Edge2, In);",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Smoother",
@"
{
	In = saturate((In - Edge1)/(Edge2-Edge1));
	Out = (In*In*In) * (In * (In * 6.0 - 15.0) + 10.0);
}",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Smoothest",
@"
{
	In = saturate((In - Edge1)/(Edge2-Edge1));
	Out = (-20.0 * pow(In, 7)) + (70.0 * pow(In, 6)) - (84.0 * pow(In, 5)) + (35.0 * pow(In, 4));
}",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Linear",
                    "Out = saturate((In - Edge1)/(Edge2-Edge1));",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "EaseOut",
@"
{
	In = saturate((In - Edge1)/(Edge2-Edge1));
	Out = In*In;
}",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "EaseIn",
@"
{
	In = saturate((In - Edge1)/(Edge2-Edge1));
	Out = 1.0 - pow(In - 1.0, 2);
}",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "SquareStep",
@"
{
	In = saturate((In - Edge1)/(Edge2-Edge1));
	Out = step(0.5, In);
}",
                    new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge1", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Edge2", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                    new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the result of the selected interpolation between 0 and 1",
            categories: new string[2] { "Math", "Interpolation" },
            synonyms: new string[1] { "curve" },
            selectableFunctions: new()
            {
                { "Smooth", "Smooth" },
                { "Smoother", "Smoother" },
                { "Smoothest", "Smoothest" },
                { "Linear", "Linear" },
                { "EaseOut", "Ease Out" },
                { "EaseIn", "Ease In" },
                { "SquareStep", "Square Step" }

            },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Edge1",
                    tooltip: "minimum step value"
                ),
                new ParameterUIDescriptor(
                    name: "Edge2",
                    tooltip: "maximum step value"
                ),

                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the result of the selected interpolation between 0 and 1"
                )
            }
        );
    }
}

using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ClampNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Clamp",
            "Out = clamp(In, Min, Max);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Min", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Max", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "Name.Synonyms", "limit" },
            { "Tooltip", "prevents the input from going below min or above max" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Min.Tooltip", "minimum value" },
            { "Parameters.Max.Tooltip", "maximum value" },
            { "Parameters.Out.Tooltip", "inupt value clamped between Min and Max" }
        };
    }
}

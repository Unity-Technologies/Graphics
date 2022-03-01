using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
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
            { "Parameters.In.Tooltip", "Input value" },
            { "Parameters.Min.Tooltip", "Minimum value" },
            { "Parameters.Max.Tooltip", "Maximum value" },
            { "Parameters.Out.Tooltip", "Inupt value clamped between Min and Max" }
        };
    }
}

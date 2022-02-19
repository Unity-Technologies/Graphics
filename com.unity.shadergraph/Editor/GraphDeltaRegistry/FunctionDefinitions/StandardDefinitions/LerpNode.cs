using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class LerpNode : IStandardNode
    {
        public FunctionDescriptor FunctionDescriptor => new(
            1,      // Version
            "Lerp", // Name
            "Out = lerp(Start, End, Alpha);",
            new ParameterDescriptor("Start", TYPE.Any, Usage.In),
            new ParameterDescriptor("End", TYPE.Any, Usage.In),
            new ParameterDescriptor("Alpha", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Lerp, Mix, Interpolate, Extrapolate, Interpolation, Extrapolation" },
            { "Tooltip", "Lerp function" },
            { "Parameters.Start.Tooltip", "Start" },
            { "Parameters.End.Tooltip", "End" },
            { "Parameters.Alpha.Tooltip", "Alpha" },
            { "Parameters.Out.Tooltip", "Start * (1-Alpha) + End * Alpha" }
        };
    }
}

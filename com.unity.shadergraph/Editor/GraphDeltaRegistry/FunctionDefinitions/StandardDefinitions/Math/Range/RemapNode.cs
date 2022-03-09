using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class RemapNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Remap",
            "Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { -1f, -1f, -1f, -1f }),
            new ParameterDescriptor("InMinMax", TYPE.Vec2, Usage.In, new float[] { -1f, 1f }),
            new ParameterDescriptor("OutMinMax", TYPE.Vec2, Usage.In, new float[] { 0f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "Tooltip", "returns a value between out min and max based on lerping the input between in min and max" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.InMinMax.Tooltip", "minimum and maximum values for input interpolation" },
            { "Parameters.OutMinMax.Tooltip", "minimum and maximum values for output interpolation" },
            { "Parameters.Out.Tooltip", "the input value with it's range remapped to the Out Min Max values" }
        };
    }
}

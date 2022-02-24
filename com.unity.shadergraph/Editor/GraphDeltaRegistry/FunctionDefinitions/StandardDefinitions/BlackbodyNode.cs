using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class BlackbodyNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Blackbody",
            @"
{
    //based on data by Mitchell Charity http://www.vendian.org/mncharity/dir3/blackbody/
    color = float3(255.0, 255.0, 255.0);
    color.x = 56100000. * pow(Temperature,(-3.0 / 2.0)) + 148.0;
    color.y = 100.04 * log(Temperature) - 623.6;
    if (Temperature > 6500.0) color.y = 35200000.0 * pow(Temperature,(-3.0 / 2.0)) + 184.0;
    color.z = 194.18 * log(Temperature) - 1448.6;
    color = clamp(color, 0.0, 255.0)/255.0;
    if (Temperature < 1000.0) color *= Temperature/1000.0;
    Out = color;
}",
            new ParameterDescriptor("Temperature", TYPE.Any, Usage.In, new float[] {512f, 0f, 0f, 0f}),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out),
            new ParameterDescriptor("color", TYPE.Vec3, Usage.Local)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "!" },
            { "Tooltip", "returns the opposite of the input " },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the opposite of the input " },
            { "Category", "Utility, Logic" }
        };
    }
}

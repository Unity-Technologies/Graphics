using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class PowerNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,          // Version
            "Power",    // Name
            "Out = UnsignedBase ? pow(abs(Base), Exp) : pow(Base, Exp);",
            new ParameterDescriptor("Base", TYPE.Any, Usage.In),
            new ParameterDescriptor("Exp", TYPE.Any, Usage.In, new float[] { 2f, 2f, 2f, 2f }),
            new ParameterDescriptor("UnsignBase", TYPE.Bool, Usage.Static, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Basic" },
            { "Name.Synonyms", "Exponentiation, ^" },
            { "Tooltip", "multiplies Base by itself the number of times given by Exp" },
            { "Parameters.Base.Tooltip", "Base" },
            { "Parameters.Exp.Tooltip", "Exponent" },
            { "Parameters.UnsignedBase.Tooltip", "Performing power on a negative values results in a NaN. When true, this feature prevents that." },
            { "Parameters.Out.Tooltip", "Base raised to the power of Exp" }

        };
    }
}

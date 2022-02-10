using System.Collections.Generic;

namespace com.unity.shadergraph.defs
{

    internal class Pow : IStandardNode
    {
        FunctionDescriptor functionDescriptor = new FunctionDescriptor(
            1,     // Version
            "Pow", // Name
            "Out = pow(In, Exp);",
            new List<ParameterDescriptor>()
            {
                new ParameterDescriptor(
                    "In",
                    TYPE.Any,
                    Usage.In
                ),
                new ParameterDescriptor(
                    "Out",
                    TYPE.Any,
                    Usage.Out
                ),
                new ParameterDescriptor(
                    "Exp",
                    TYPE.Any,
                    Usage.In
                ),
            }
        )
        };

        Dictionary<string, string> uIStrings = new()
        {

        };
    }

}

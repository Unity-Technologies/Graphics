using System.Collections.Generic;
using com.unity.shadergraph.defs;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

/// <summary>
/// StandardNodeDefintions is a holder for FunctionDescriptors that represent the
/// nodes available at graph load time by default.
/// </summary>
internal class StandardNodeDefinitions
{
    internal static readonly List<FunctionDescriptor> FUNCTIONS = new()
    {
        new FunctionDescriptor(
            1,      // Version
            "Pow",  // Name
            "Out = pow(In, Exp);",
            new List<ParameterDescriptor>()
            {
                new ParameterDescriptor(
                    "In",
                    TYPE.Any,
                    Usage.In
                ),
                new ParameterDescriptor(
                    "Exp",
                    TYPE.Any,
                    Usage.In
                ),
                new ParameterDescriptor(
                    "Out",
                    TYPE.Any,
                    Usage.Out
                )
            }
        )
    };

    // StandardNodeDefinitions should not be instantiated
    private StandardNodeDefinitions()
    { }
}

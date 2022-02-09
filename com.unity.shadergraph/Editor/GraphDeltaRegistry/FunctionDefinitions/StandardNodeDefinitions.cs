using System.Collections.Generic;
using com.unity.shadergraph.defs;

/// <summary>
/// StandardNodeDefintions is a holder for FunctionDescriptors that represent the
/// nodes available at graph load time by default.
/// </summary>
internal class StandardNodeDefinitions
{
    internal static readonly List<FunctionDescriptor> FUNCTIONS = new()
    {

    };

    // StandardNodeDefinitions should not be instantiated
    private StandardNodeDefinitions()
    { }
}

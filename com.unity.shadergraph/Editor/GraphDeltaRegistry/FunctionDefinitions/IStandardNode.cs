using System.Collections.Generic;

namespace com.unity.shadergraph.defs
{

    internal interface IStandardNode
    {
        FunctionDescriptor FunctionDescriptor { get; }
        Dictionary<string, string> UIStrings { get; }
    }

}

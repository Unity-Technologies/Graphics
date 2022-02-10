using System.Collections.Generic;

namespace com.unity.shadergraph.defs
{

    internal interface IStandardNode
    {
        FunctionDescriptor functionDescriptor { get; }
        Dictionary<string, string> uIStrings { get; }
    }

}

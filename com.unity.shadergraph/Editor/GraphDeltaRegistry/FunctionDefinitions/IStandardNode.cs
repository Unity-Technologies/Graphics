using System.Collections.Generic;

namespace com.unity.shadergraph.defs
{

    internal interface IStandardNode
    {
        static FunctionDescriptor FunctionDescriptor { get; }
        static Dictionary<string, string> UIStrings { get; }
        static Dictionary<string, float> UIParameters { get; }
    }

}

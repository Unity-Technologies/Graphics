using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Defs
{

    internal interface IStandardNode
    {
        static FunctionDescriptor FunctionDescriptor { get; }
        static Dictionary<string, string> UIStrings { get; }
        static Dictionary<string, float> UIHints { get; }
    }

}

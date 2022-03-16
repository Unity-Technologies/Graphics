using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    public interface IStandardNode
    {
        static FunctionDescriptor FunctionDescriptor { get; }
        static Dictionary<string, string> UIStrings { get; }
        static Dictionary<string, float> UIHints { get; }
    }

}

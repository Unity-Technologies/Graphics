
namespace UnityEditor.ShaderGraph.Defs
{
    internal interface IStandardNode
    {
        static string Name { get; }
        static int Version { get; }
        static FunctionDescriptor FunctionDescriptor { get; }
        static NodeUIDescriptor NodeUIDescriptor { get; }
    }
}

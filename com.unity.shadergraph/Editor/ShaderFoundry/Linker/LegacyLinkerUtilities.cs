using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    static class LegacyCustomizationPoints
    {
        internal const string VertexDescriptionCPName = "VertexDescription";
        internal const string SurfaceDescriptionCPName = "SurfaceDescription";

        internal const string VertexDescriptionFunctionName = "VertexDescriptionFunction";
        internal const string SurfaceDescriptionFunctionName = "SurfaceDescriptionFunction";

        internal const string VertexEntryPointInputName = "VertexDescriptionInputs";
        internal const string VertexEntryPointOutputName = "VertexDescription";

        internal const string SurfaceEntryPointInputName = "SurfaceDescriptionInputs";
        internal const string SurfaceEntryPointOutputName = "SurfaceDescription";
    }

    internal class VaryingVariable
    {
        internal ShaderType Type;
        internal string Name;
    }

    internal class LegacyEntryPoints
    {
        internal BlockInstance vertexDescBlockInstance;
        internal BlockInstance fragmentDescBlockInstance;
        internal List<VaryingVariable> customInterpolants = new List<VaryingVariable>();
    }
}

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Defs
{

    /// <summary>
    /// A FunctionDescriptor describes a shader function.
    ///
    /// In registration (See: Register(FunctionDescriptor funcDesc)), a
    /// FunctionDescriptor is registered as a node prototype.
    /// </summary>
    internal readonly struct FunctionDescriptor
    {
        public string Name { get; } // Must be a valid reference name
        public IReadOnlyCollection<ParameterDescriptor> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.
        public IReadOnlyCollection<string> Includes { get; }
        public bool IsHelper { get; } // helper functions are always included in compiled output

        public FunctionDescriptor(
            string name,
            string body,
            ParameterDescriptor[] parameters,
            string[] includes = null,
            bool isHelper = false)
        {
            Name = name;
            Parameters = parameters.ToList().AsReadOnly();
            Body = body;
            var includesList = includes == null  ? new List<string>() : includes.ToList();
            Includes = includesList.AsReadOnly();
            IsHelper = isHelper;
        }
    }
}


using System.Collections.Immutable;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal struct ShaderType : IShaderType
    {
        public bool IsValid { get; private set; }
        public string Name { get; private set; }
        internal ShaderType(string name)
        {
            IsValid = true;
            Name = name;
        }
    }

    internal struct ShaderField : IShaderField
    {
        public bool IsValid { get; private set; }
        public string Name { get; private set; }
        public bool IsInput { get; private set; }
        public bool IsOutput { get; private set; }
        public IShaderType ShaderType { get; private set; }
        public IReadOnlyDictionary<string, string> Hints { get; private set; }


        internal ShaderField(string name, bool isInput, bool isOutput, IShaderType shaderType, IReadOnlyDictionary<string, string> hints)
        {
            IsValid = true;
            Name = name;
            IsInput = isInput;
            IsOutput = isOutput;
            ShaderType = shaderType;
            Hints = hints ?? ImmutableDictionary<string, string>.Empty;
        }
    }

    internal struct ShaderFunction : IShaderFunction
    {
        public bool IsValid { get; private set; }
        public string Name { get; private set; }
        public IShaderType ReturnType { get; private set; }
        public string FunctionBody { get; private set; }
        public IEnumerable<string> Namespace { get; private set; }
        public IEnumerable<IShaderField> Parameters { get; private set; }
        public IReadOnlyDictionary<string, string> Hints { get; private set; }

        internal ShaderFunction(string name, IEnumerable<string> namespaces, IEnumerable<IShaderField> parameters, IShaderType returnType, string functionBody, IReadOnlyDictionary<string, string> hints)
        {
            IsValid = true;
            Name = name;
            ReturnType = returnType;
            FunctionBody = functionBody;

            Parameters = parameters ?? ImmutableArray<IShaderField>.Empty;
            Namespace = namespaces ?? ImmutableArray<string>.Empty;
            Hints = hints ?? ImmutableDictionary<string, string>.Empty;
        }
    }
}

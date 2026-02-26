using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Data needed to represent a valid shader object and supplemental information
    // on how this object might be presented in different contexts.
    // Shader Object itself is Context and Domain agnostic.
    internal interface IShaderObject
    {
        bool IsValid { get; }
        string Name { get; }
        IEnumerable<string> Namespace => ImmutableArray<string>.Empty;
        IReadOnlyDictionary<string, string> Hints => ImmutableDictionary<string, string>.Empty;
    }

    internal interface IShaderType : IShaderObject { }

    internal interface IShaderField : IShaderObject
    {
        bool IsInput { get; }
        bool IsOutput { get; }
        IShaderType ShaderType { get; }
    }

    internal interface IShaderFunction : IShaderObject
    {
        IShaderType ReturnType { get; }
        string FunctionBody { get; }
        IEnumerable<IShaderField> Parameters { get; }
    }

    // Abstracts how or where an IShaderObject may be defined,
    // allowing for various Domains to use IShaderObjects in a
    // context free manner.
    internal interface IProvider
    {
        string ProviderKey { get; }
        GUID AssetID { get; }
        bool IsValid { get; }

        void Reload() { }
        IProvider Clone();
    }

    internal interface IProvider<T> : IProvider where T : IShaderObject
    {
        T Definition { get; }
        bool IProvider.IsValid => Definition?.IsValid ?? false;
    }
}

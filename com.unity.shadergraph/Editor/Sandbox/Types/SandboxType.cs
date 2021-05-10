using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;


// Shader Value Types have the following requirements:
// serialize / deserialize (with support for singletons and data-based types)
//
// provide a unique type name -- maybe we don't care about renaming types to avoid collisions, just make it an error
// able to determine if two ShaderValueTypes are the same type
// if an HLSL type:
//   able to declare the type
//   able to declare a variable of the type
//   maybe:
//     allow to be declared in external include file (and store dependency on it?)
//
// able to easily get a reference to a type for common/built-in types
// able to describe certain attributes of the type:
//   HLSL type (expressible in a shader)
//   HLSL Built-in type (no need to declare it)
//   HLSL Scalar type
//   HLSL Vector type (base scalar type, cols)
//   HLSL Matrix type (base scalar type, rows/cols)
//   HLSL Object type
//   HLSL Texture type
//   HLSL Sampler type
//   HLSL Struct type


[Serializable]
public sealed class SandboxType
{
    [SerializeField]
    string name;

    [SerializeField]
    JsonData<SandboxTypeDefinition> definition;     // for types that have a definition (all except plain built-in types)

    // these flags are used to quickly query for various aspects of the type
    public enum Flags
    {
        Placeholder = 1,                    // is a placeholder type for generic types or functions, does not generate valid HLSL
        Scalar = 2,                         // is a scalar value, i.e. bool, int, float, half...
        Vector = 4,                         // is a vector value (bool2, int3, float4, half2, float1)
        Matrix = 8,                         // is a matrix value (float3x3, int2x4, bool1x4, half1x1, etc.)
        Struct = 16,
        Texture = 32,
        SamplerState = 64,
        BareResource = 128,                 // raw resource, not wrapped in Unity struct (Texture2D, SamplerState, cbuffer etc.)
        HasHLSLDeclaration = 512,           // has an HLSL Declaration

        VectorOrScalar = Scalar | Vector,
    }

    [SerializeField]
    Flags flags;

    // public interface
    public string Name => name;
    internal SandboxTypeDefinition Definition => definition.value;
    internal T GetDefinition<T>() where T : SandboxTypeDefinition { return definition.value as T; }

    public bool IsPlaceholder =>        (flags & Flags.Placeholder) != 0;
    public bool IsScalar =>             (flags & Flags.Scalar) != 0;
    public bool IsVector =>             (flags & Flags.Vector) != 0;
    public bool IsMatrix =>             (flags & Flags.Matrix) != 0;
    public bool IsStruct =>             (flags & Flags.Struct) != 0;
    public bool IsTexture =>            (flags & Flags.Texture) != 0;
    public bool IsSamplerState =>       (flags & Flags.SamplerState) != 0;
    public bool IsBareResource =>       (flags & Flags.BareResource) != 0;
    public bool IsVectorOrScalar =>     (flags & Flags.VectorOrScalar) != 0;
    public bool HasHLSLDeclaration =>   (flags & Flags.HasHLSLDeclaration) != 0;

    // returns the vector dimension (1, 2, 3 or 4) for vector types, 1 for scalar types, and 0 for all other types
    public int VectorDimension
    {
        get
        {
            if ((flags & Flags.Vector) != 0)
            {
                var vecDef = GetDefinition<VectorTypeDefinition>();
                return vecDef?.VectorDimension ?? 0;
            }
            if ((flags & Flags.Scalar) != 0)
                return 1;
            return 0;
        }
    }

    // returns the number of matrix columns (1, 2, 3 or 4) for matrix types, 0 for all other types
    public int MatrixColumns
    {
        get
        {
            if ((flags & Flags.Matrix) != 0)
            {
                var matDef = GetDefinition<MatrixTypeDefinition>();
                if (matDef != null)
                    return matDef.MatrixColumns;
            }
            return 0;
        }
    }

    // returns the number of matrix columns (1, 2, 3 or 4) for matrix types, 0 for all other types
    public int MatrixRows
    {
        get
        {
            if ((flags & Flags.Matrix) != 0)
            {
                var matDef = GetDefinition<MatrixTypeDefinition>();
                if (matDef != null)
                    return matDef.MatrixRows;
            }
            return 0;
        }
    }

    internal SandboxType(SandboxTypeDefinition definition)
    {
        this.name = definition.GetTypeName();
        this.definition = definition;
        this.flags = definition.GetTypeFlags();
    }

    // this constructor is only for basic built-in types that don't have to be serialized
    // (because we can always assume they will always exist in the Default)
    // this cannot be used by public users, they must go the TypeDefinition route
    internal SandboxType(string name, Flags flags)
    {
        this.name = name;
        this.flags = flags;
    }

    public bool ValueEquals(SandboxType other)
    {
        if (other == this)
            return true;

        return (name == other.name) &&
            (flags == other.flags) &&
            (definition.value.ValueEquals(other.definition.value));
    }

    public sealed override bool Equals(object other)
    {
        // names are enforced unique, so we can use those as a proxy for equality
        var otherType = other as SandboxType;
        if (otherType == null)
            return false;
        return otherType.name == name;
    }

    public sealed override int GetHashCode()
    {
        return name.GetHashCode();
    }

    // can auto-convert any string to the corresponding default type
    // note this only works with DEFAULT types, not any custom introduced types
    public static implicit operator SandboxType(string s) => Types.Default.GetShaderType(s);

    public delegate bool Filter(SandboxType type);

    internal void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        if (definition.value != null)
            definition.value.AddHLSLVariableDeclarationString(sb, id);
        else
            sb.Add(name, " ", id);
    }

    internal void AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        if (HasHLSLDeclaration && (definition.value != null))
            definition.value.AddHLSLTypeDeclarationString(sb);
    }
}

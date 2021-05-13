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
        Scalar = 2,                         // is a scalar value, i.e. bool, int, float, half... no type definition
        Vector = 4,                         // VectorTypeDefinition
        Matrix = 8,                         // MatrixTypeDefinition
        Struct = 16,                        // StructureTypeDefinition
        Texture = 32,                       // TextureTypeDefinition
        SamplerState = 64,                  // SamplerStateTypeDefinition
        Array = 128,                        // ArrayTypeDefinition
        BareResource = 256,                 // raw resource, not wrapped in Unity struct (Texture2D, SamplerState, cbuffer etc.)
        HasHLSLDeclaration = 512,           // has an HLSL Declaration
        IsDefaultType = 1024,               // is a default type, exists in the Types.Default collection

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
    public bool IsArray =>              (flags & Flags.Array) != 0;
    public bool IsBareResource =>       (flags & Flags.BareResource) != 0;
    public bool IsVectorOrScalar =>     (flags & Flags.VectorOrScalar) != 0;
    public bool HasHLSLDeclaration =>   (flags & Flags.HasHLSLDeclaration) != 0;
    public bool IsDefaultType =>        (flags & Flags.IsDefaultType) != 0;

    // returns the vector dimension (1, 2, 3 or 4) for vector types, 1 for scalar types, and 0 for all other types
    public int VectorDimension
    {
        get
        {
            if (IsVector)
            {
                var vecDef = GetDefinition<VectorTypeDefinition>();
                return vecDef?.VectorDimension ?? 0;
            }
            if (IsScalar)
                return 1;
            return 0;
        }
    }

    // returns the number of matrix columns (1, 2, 3 or 4) for matrix types, 0 for all other types
    public int MatrixColumns
    {
        get
        {
            if (IsMatrix)
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
            if (IsMatrix)
            {
                var matDef = GetDefinition<MatrixTypeDefinition>();
                if (matDef != null)
                    return matDef.MatrixRows;
            }
            return 0;
        }
    }

    public int ArrayElements
    {
        get
        {
            if (IsArray)
            {
                var arrDef = GetDefinition<ArrayTypeDefinition>();
                if (arrDef != null)
                    return arrDef.ArrayElements;
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
        // same reference means they are trivially equal
        // this is important to check first to reduce the work involved
        if (ReferenceEquals(other, this))
            return true;

        if (other == null)
            return false;

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

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

// hmm, how do we represent types.. as serializable classes we can record on the graph?
// if it's user extendable, we need to be able to represent "unknown" types, like we do for Targets and Nodes...


// Shader Value Types have the following requirements:
// serialize / deserialize (with support for singletons and data-based types)
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

// able to express pseudo-generic types like $precision switch, and $precision2, $precision4 from the function definition side
//
// Do we need full generic type support?  probably not...  but if we do, data-based types seems the way to go.
// trying to base it off of actual generic types in C# seems troublesome, lots of limitations to work around


[Serializable]
public abstract class SandboxValueTypeDefinition : JsonObject
{
    public abstract string GetTypeName();
    public abstract SandboxValueType.Flags GetTypeFlags();
    internal abstract void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id);
    internal abstract bool AddHLSLTypeDeclarationString(ShaderStringBuilder sb);
    public abstract bool ValueEquals(SandboxValueTypeDefinition other);
}


public sealed class SandboxTypeDecl
{
    enum DeclOp
    {
        TypeVoid = 1,             // void         0 bytes
        TypeBool = 2,             // bool         0 bytes
        TypeInt = 3,              // int          1 byte         (bits)
        TypeFloat = 4,            // float        1 byte         (bits)

        TypeImage,                // 5 bytes:  (dimension)  (depth?) (arrayed?) (multisample?) (sampled?)
        TypeSampler,              //
        TypeSampledImage,         // texture and sampler combined

        TypeVector,               // 3 bytes:  vec      (component count)     (subtype)
        TypeMatrix,               // 3 bytes:  mat      (column count)        (vector subtype)

        TypeFixedArray,           // fixed size array

        TypeUnboundedArray,       // unbounded array

        TypeStruct,               // structure
        TypeOpaque,               // structure with no declared members
        TypeFunction,             //
        TypePointer,              // :P
    };

    List<byte> declCode;

    // public must go through the Builder class
    internal SandboxTypeDecl(List<byte> declCode)
    {
        this.declCode = declCode;
    }

    public bool isVoid => (declCode[0] == (byte)DeclOp.TypeVoid);
    public bool isBool => (declCode[0] == (byte)DeclOp.TypeBool);
    public bool isInt => (declCode[0] == (byte)DeclOp.TypeInt);
    public bool isFloat => (declCode[0] == (byte)DeclOp.TypeFloat);
    public bool isVector => (declCode[0] == (byte)DeclOp.TypeVector);
    public bool isMatrix => (declCode[0] == (byte)DeclOp.TypeMatrix);
    public bool isFixedArray => (declCode[0] == (byte)DeclOp.TypeFixedArray);
    public bool isUnboundedArray => (declCode[0] == (byte)DeclOp.TypeUnboundedArray);
    public bool isStruct => (declCode[0] == (byte)DeclOp.TypeStruct);

    public class Builder
    {
        List<Byte> declCode;
    }
}


[Serializable]
public sealed class SandboxValueType // : JsonObject
{
    // public override int currentVersion => 1;

    [SerializeField]
    string name;                        // must be unique!!!

    [SerializeField]
    JsonData<SandboxValueTypeDefinition> definition;    // for types that have a data definition

    // these flags are used to quickly identify and query for various properties of the type
    public enum Flags
    {
        Placeholder = 1,        // is a placeholder type for generic functions
        Scalar = 2,             // is a scalar value, i.e. bool, int, float, half...
        Vector2 = 4,            // is a vector value of dimension 2, i.e. bool2, int2, float2, half2...
        Vector3 = 8,            // is a vector value of dimension 3, i.e. bool3, int3, float3, half3...
        Vector4 = 16,           // is a vector value of dimension 4, i.e. bool4, int4, float4, half4...
        AnyVector = Scalar | Vector2 | Vector3 | Vector4,
        Matrix = 32,
        Struct = 64,
        Object = 128,
        Texture = 256
    }

    [SerializeField]
    Flags flags;

    // public interface
    public string Name => name;
    public SandboxValueTypeDefinition Definition => definition.value;
    public T GetDefinition<T>() where T : SandboxValueTypeDefinition { return definition.value as T; }

    public bool IsPlaceholder => (flags & Flags.Placeholder) != 0;
    public bool IsScalar => (flags & Flags.Scalar) != 0;
    public bool IsVector => (flags & Flags.AnyVector) != 0;
    public bool IsMatrix => (flags & Flags.Matrix) != 0;
    public bool IsStruct => (flags & Flags.Struct) != 0;
    public bool IsObject => (flags & Flags.Object) != 0;
    public bool IsTexture => (flags & Flags.Texture) != 0;

    public int VectorSize
    {
        get
        {
            if ((flags & Flags.Vector4) != 0) return 4;
            if ((flags & Flags.Vector3) != 0) return 3;
            if ((flags & Flags.Vector2) != 0) return 2;
            if ((flags & Flags.Scalar) != 0) return 1;
            return 0;
        }
    }

    internal SandboxValueType(SandboxValueTypeDefinition definition)
    {
        this.name = definition.GetTypeName();
        this.definition = definition;
        this.flags = definition.GetTypeFlags();
    }

    // this constructor is only for basic built-in types that don't have to be serialized
    // (because we can always assume they will always exist in the Default)
    // this cannot be used by public users, they must go the TypeDefinition route
    internal SandboxValueType(string name, Flags flags)
    {
        this.name = name;
        //this.definition = null;
        this.flags = flags;
    }

    public bool ValueEquals(SandboxValueType other)
    {
        return (name == other.name) &&
            (flags == other.flags) &&
            (definition.value.ValueEquals(other.definition.value));
    }

    public sealed override bool Equals(object other)
    {
        // names are enforced unique, so we can use those as a proxy for equality
        var otherType = other as SandboxValueType;
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
    public static implicit operator SandboxValueType(string s) => Types.Default.GetShaderType(s);

    public delegate bool Filter(SandboxValueType type);

    internal void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        if (definition.value != null)
            definition.value.AddHLSLVariableDeclarationString(sb, id);
        else
            sb.Add(name, " ", id);
    }

    internal bool AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        if (definition.value != null)
            return definition.value.AddHLSLTypeDeclarationString(sb);
        return false;
    }
}

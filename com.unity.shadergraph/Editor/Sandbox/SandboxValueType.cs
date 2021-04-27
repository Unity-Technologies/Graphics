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


[Serializable]
public sealed class SandboxValueType // : JsonObject          // TODO: public
{
    // public override int currentVersion => 1;

    [SerializeField]
    string name;                        // must be unique!!!

    [SerializeField]
    JsonRef<SandboxValueTypeDefinition> definition;    // for types that have a data definition

    public enum Flags
    {
        Placeholder = 1,
        Scalar = 2,
        Vector2 = 4,
        Vector3 = 8,
        Vector4 = 16,
        AnyVector = Scalar | Vector2 | Vector3 | Vector4,
        Matrix = 32,
        Struct = 64,
        Object = 128,
        Texture = 256
    }

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
        this.definition = null;
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

/*
public abstract class SandboxValueTypeCaster
{
    // aka target type
    public abstract ShaderValueType leftType { get; }
    // aka source types
    public abstract bool IsAssignableFrom(ShaderValueType rightType);
    // generate code to actually do the type cast between two variables
    public abstract bool HLSLAssignVariable(string leftName, ShaderValueType rightType, string rightName, ShaderBuilder sb);
}
*/

/*
public class HLSLVector4TypeCaster : ShaderValueTypeCaster
{
    // aka target type
    public override ShaderValueType leftType { get { return HLSLVector4Type.Instance; } }
    // aka source types
    public override bool IsAssignableFrom(ShaderValueType rightType)
    {
        if (rightType is HLSLVectorType)
            return true;
        if (rightType is HLSLIntType)
            return true;
        return false;
    }
    // generate code to actually do the type cast between two variables
    public override bool HLSLAssignVariable(string leftName, ShaderValueType rightType, string rightName, ShaderBuilder sb)
    {
        if (rightType is HLSLVectorType)
        {
            // TODO: this isn't correct, but shows the basic idea
            sb.Add(leftName, " = (", rightType.GetTypeName(), ") ", rightName);
            return true;
        }
        if (rightType is HLSLIntType)
        {
            // TODO: this isn't correct, but shows the basic idea
            sb.Add(leftName, " = (", rightType.GetTypeName(), ") ", rightName);
            return true;
        }
        return false;
    }
}
*/

[Serializable]
public class UnityTexture2DTypeDefinition : SandboxValueTypeDefinition
{
    public override int latestVersion => 1;

    internal override bool AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        // defined in the include file -- TODO
        return false;
    }

    internal override void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        sb.Add(GetTypeName(), " ", id);
    }

    public override SandboxValueType.Flags GetTypeFlags()
    {
        // TODO: not clear what category this falls into, struct and/or texture
        // Although it is really a struct,
        // I guess this is the default type we use to represent Texture, so...
        return SandboxValueType.Flags.Texture | SandboxValueType.Flags.Object;
    }

    public override string GetTypeName()
    {
        return "UnityTexture2D";
    }

    public override bool ValueEquals(SandboxValueTypeDefinition other)
    {
        return (other is UnityTexture2DTypeDefinition);
    }

    // TODO: make use an interface to add to an include collection
    // public override string GetIncludes()               { return "com.unity.core/textures.hlsl";  }
}

// can we make some kind of wrapper class for C# variable types?
// that we can use for inline operations?

/*
// non shader type (static eval only)
public class StringType : SingletonShaderValueType
{
    public override int currentVersion => 1;
    static StringType _instance;
    public static StringType Instance { get { return _instance ?? (_instance = new StringType()); } }
    private StringType() { }
    // TODO: need some way to identify that this is a static build-time only type
    // i.e. it cannot be evaluated dynamically in a shader
    public static string GetSingletonTypeName() { return "string"; }
    public override bool AddHLSLTypeDeclarationString(ShaderBuilder sb, string nameOverride) { return false; }
    public override void AddHLSLVariableDeclarationString(ShaderBuilder sb, string id)
    {
    }
}
*/

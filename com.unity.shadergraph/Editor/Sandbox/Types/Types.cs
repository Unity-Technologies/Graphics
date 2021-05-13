using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Types
{
    // inherit type collections so we can pull in shared global definitions easily, and add local definitions
    Types parent;

    // each type is stored using its unique name as the key
    Dictionary<string, SandboxType> shaderTypes = new Dictionary<string, SandboxType>();

    bool readOnly = false;

    // store all TypeCasters
    //   Dictionary<SandboxValueType, List<SandboxValueTypeCaster>> casts = new Dictionary<SandboxValueType, List<SandboxValueTypeCaster>>();

    // TODO: rename to "local Types" or something like that, as it doesn't iterate parent types...  make a real AllTypes that does iterate parent
    public IEnumerable<SandboxType> AllTypes => shaderTypes.Values;

    public Types(Types parent)
    {
        this.parent = parent;
        parent?.SetReadOnly();
    }

    public void SetReadOnly()
    {
        readOnly = true;
    }

    public SandboxType AddType(StructTypeDefinition structType)
    {
        return AddTypeInternal(structType);
    }

    public SandboxType AddType(ArrayTypeDefinition structType)
    {
        return AddTypeInternal(structType);
    }

    internal SandboxType AddTypeInternal(SandboxTypeDefinition typeDef)
    {
        if (readOnly)
        {
            // Debug.LogError("Cannot add a type to a readonly Types collection");
            return null;
        }

        if (typeDef == null)
            return null;

        string typeName = typeDef.GetTypeName();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            // Debug.LogError("Cannot add Type with no name");
            return null;
        }

        var existingType = FindTypeByName(typeName);
        if (existingType != null)
        {
            // name collision:  check if it's actually the same type or not
            var existingDef = existingType.GetDefinition<SandboxTypeDefinition>();
            if (existingDef?.ValueEquals(typeDef) ?? false)
                return existingType;

            // cannot add, type has conflicting name
            // Debug.LogError("Type name conflicts with existing Type name: " + typeName);
            return null;
        }

        // new type, add it
        var type = new SandboxType(typeDef);
        shaderTypes.Add(typeName, type);
        return type;
    }

    // returns the ShaderType added on success, or null on failure
    // NOTE: the returned ShaderType may not be the one you passed in --
    // -- you may have tried to add a duplicate definition, and it returns the existing one
    internal SandboxType AddTypeInternal(SandboxType type)
    {
        if (readOnly)
        {
            // Debug.LogError("Cannot add a type to a readonly Types collection");
            return null;
        }

        var typeName = type.Name;
        if (ContainsTypeName(typeName))
        {
            // name collision: ok as long as it's exactly the same
            return FindExactType(type);
        }
        else
        {
            // new type, add it
            shaderTypes.Add(typeName, type);
            return type;
        }
    }

    public SandboxType GetShaderType(string name)
    {
        SandboxType result = null;
        shaderTypes.TryGetValue(name, out result);
        if ((result == null) && parent != null)
            result = parent.GetShaderType(name);
        return result;
    }

    public bool ContainsTypeName(string name)
    {
        if (shaderTypes.ContainsKey(name))
            return true;
        if ((parent != null) && parent.ContainsTypeName(name))
            return true;
        return false;
    }

    public SandboxType FindExactType(SandboxType type)
    {
        if (shaderTypes.TryGetValue(type.Name, out SandboxType match))
        {
            if (match.ValueEquals(type))
                return match;   // the type matches, we found it
            else
                return null;    // name matches, but types do not
        }
        if (parent != null)
            return parent.FindExactType(type);
        return null;
    }

    public SandboxType FindTypeByName(string name)
    {
        if (shaderTypes.TryGetValue(name, out SandboxType match))
            return match;       // found it
        if (parent != null)
            return parent.FindTypeByName(name);
        return null;
    }

    static Types _default = null;

    // TODO: need a way to make this Read Only -- don't allow anyone to modify the shared defaults
    public static Types Default { get { return _default ?? (_default = BuildDefaultTypeSystem()); } }

    // cache of commonly used built-in types
    public static SandboxType _bool = Default.GetShaderType("bool");

    public static SandboxType _int = Default.GetShaderType("int");

    public static SandboxType _float = Default.GetShaderType("float");
    public static SandboxType _float2 = Default.GetShaderType("float2");
    public static SandboxType _float3 = Default.GetShaderType("float3");
    public static SandboxType _float4 = Default.GetShaderType("float4");

    public static SandboxType _half = Default.GetShaderType("half");
    public static SandboxType _half2 = Default.GetShaderType("half2");
    public static SandboxType _half3 = Default.GetShaderType("half3");
    public static SandboxType _half4 = Default.GetShaderType("half4");

    public static SandboxType _precision = Default.GetShaderType("$precision");
    public static SandboxType _precision2 = Default.GetShaderType("$precision2");
    public static SandboxType _precision3 = Default.GetShaderType("$precision3");
    public static SandboxType _precision4 = Default.GetShaderType("$precision4");
    public static SandboxType _precision4x4 = Default.GetShaderType("$precision4x4");
    public static SandboxType _precision3x3 = Default.GetShaderType("$precision3x3");
    public static SandboxType _precision2x2 = Default.GetShaderType("$precision2x2");

    public static SandboxType _dynamicVector = Default.GetShaderType("$dynamicVector$");
    public static SandboxType _dynamicMatrix = Default.GetShaderType("$dynamicMatrix$");
    public static SandboxType _dynamic = Default.GetShaderType("$dynamic$");

    public static SandboxType _UnityTexture2D = Default.GetShaderType("UnityTexture2D");
    public static SandboxType _UnitySamplerState = Default.GetShaderType("UnitySamplerState");

    public static SandboxType PrecisionVector(int vectorDimension)
    {
        if (vectorDimension == 1)
            return Types._precision;
        if (vectorDimension == 2)
            return Types._precision2;
        if (vectorDimension == 3)
            return Types._precision3;
        if (vectorDimension == 4)
            return Types._precision4;
        return null;
    }

    static SandboxType[,] _precisionMatrices = new SandboxType[4, 4];
    public static SandboxType PrecisionMatrix(int rows, int cols)
    {
        if ((rows >= 1) && (rows <= 4) && (cols >= 1) && (cols <= 4))
        {
            SandboxType result = _precisionMatrices[rows - 1, cols - 1];
            if (result == null)
                result = Default.GetShaderType("$precision" + rows + "x" + cols);
            return result;
        }
        return null;
    }

    /*
        public void AddTypeCast(SandboxValueTypeCaster cast)
        {
            if (!casts.TryGetValue(cast.leftType, out var typeCasters))
            {
                typeCasters = new List<SandboxValueTypeCaster>();
                casts.Add(cast.leftType, typeCasters);
            }
            typeCasters.Add(cast);
        }
    */
    /*
        public bool IsAssignable(SandboxValueType leftType, SandboxValueType rightType)
        {
            // only assignable if they are equivalent types
            // OR if there exists a cast between them
            if (ContainsType(leftType) && ContainsType(rightType))
            {
                if (leftType == rightType)
                    return true;
                if (casts.TryGetValue(leftType, out var typeCasters))
                {
                    foreach (var tc in typeCasters)
                        if (tc.IsAssignableFrom(rightType))
                            return true;
                }
                if (parent?.IsAssignable(leftType, rightType) ?? false)
                    return true;
            }
            return false;
        }
        public bool HLSLAssignVariable(SandboxValueType leftType, string leftName, SandboxValueType rightType, string rightName, ShaderBuilder sb)
        {
            if (ContainsType(leftType) && ContainsType(rightType))
            {
                if (leftType == rightType)
                {
                    // easy, equivalent types should be directly assignable in HLSL
                    sb.Add(leftName, " = ", rightName, ";");
                    return true;
                }
                if (casts.TryGetValue(leftType, out var typeCasters))
                {
                    foreach (var tc in typeCasters)
                        if (tc.HLSLAssignVariable(leftName, rightType, rightName, sb))
                            return true;
                }
                if (parent?.HLSLAssignVariable(leftType, leftName, rightType, rightName, sb) ?? false)
                    return true;
            }
            return false;
        }
    */
}

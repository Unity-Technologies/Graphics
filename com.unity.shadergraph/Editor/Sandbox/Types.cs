using System;
using System.Collections.Generic;
using UnityEngine;

public class Types
{
    // inherit type collections so we can pull in shared global definitions easily, and add local definitions
    Types parent;

    // each type is stored using its unique name as the key
    Dictionary<string, SandboxValueType> shaderTypes = new Dictionary<string, SandboxValueType>();

    bool readOnly = false;

    // store all TypeCasters
    //         Dictionary<SandboxValueType, List<SandboxValueTypeCaster>> casts = new Dictionary<SandboxValueType, List<SandboxValueTypeCaster>>();

    public Types(Types parent)
    {
        this.parent = parent;
        parent?.SetReadOnly();
    }

    public void SetReadOnly()
    {
        readOnly = true;
    }

    public SandboxValueType AddType(SandboxValueTypeDefinition typeDef)
    {
        if (readOnly)
        {
            Debug.LogError("Cannot add a type to a readonly Types collection");
            return null;
        }
        var shaderType = new SandboxValueType(typeDef);
        return AddType(shaderType);
    }

    // returns the ShaderType added on success, or null on failure
    // NOTE: the returned ShaderType may not be the one you passed in --
    // -- you may have tried to add a duplicate definition, and it returns the existing one
    internal SandboxValueType AddType(SandboxValueType type)
    {
        if (readOnly)
        {
            Debug.LogError("Cannot add a type to a readonly Types collection");
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

    public SandboxValueType GetShaderType(string name)
    {
        SandboxValueType result = null;
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

    public SandboxValueType FindExactType(SandboxValueType type)
    {
        if (shaderTypes.TryGetValue(type.Name, out SandboxValueType match))
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

    static Types _default = null;

    // TODO: need a way to make this Read Only -- don't allow anyone to modify the shared defaults
    public static Types Default { get { return _default ?? (_default = BuildDefaultTypeSystem()); } }

    // cache of commonly used built-in types
    public static SandboxValueType _bool = Default.GetShaderType("bool");

    public static SandboxValueType _int = Default.GetShaderType("int");

    public static SandboxValueType _float = Default.GetShaderType("float");
    public static SandboxValueType _float2 = Default.GetShaderType("float2");
    public static SandboxValueType _float3 = Default.GetShaderType("float3");
    public static SandboxValueType _float4 = Default.GetShaderType("float4");

    public static SandboxValueType _half = Default.GetShaderType("half");
    public static SandboxValueType _half2 = Default.GetShaderType("half2");
    public static SandboxValueType _half3 = Default.GetShaderType("half3");
    public static SandboxValueType _half4 = Default.GetShaderType("half4");

    public static SandboxValueType _precision = Default.GetShaderType("$precision");
    public static SandboxValueType _precision2 = Default.GetShaderType("$precision2");
    public static SandboxValueType _precision3 = Default.GetShaderType("$precision3");
    public static SandboxValueType _precision4 = Default.GetShaderType("$precision4");

    public static SandboxValueType _dynamicVector = Default.GetShaderType("$dynamicVector");
    public static SandboxValueType _dynamicMatrix = Default.GetShaderType("$dynamicMatrix");

    public static SandboxValueType _UnityTexture2D = Default.GetShaderType("UnityTexture2D");

    public static SandboxValueType Precision(int vectorSize)
    {
        if (vectorSize == 1)
            return Types._precision;
        if (vectorSize == 2)
            return Types._precision2;
        if (vectorSize == 3)
            return Types._precision3;
        if (vectorSize == 4)
            return Types._precision4;
        return null;
    }

    static Types BuildDefaultTypeSystem()
    {
        var types = new Types(null);

        List<SandboxValueType> scalarTypes = new List<SandboxValueType>()
        {
            new SandboxValueType("bool",   SandboxValueType.Flags.Scalar),
            new SandboxValueType("int",    SandboxValueType.Flags.Scalar),
            new SandboxValueType("half",   SandboxValueType.Flags.Scalar),
            new SandboxValueType("float",  SandboxValueType.Flags.Scalar),
            new SandboxValueType("double", SandboxValueType.Flags.Scalar),
            new SandboxValueType("$precision", SandboxValueType.Flags.Scalar | SandboxValueType.Flags.Placeholder)
        };

        foreach (var s in scalarTypes)
        {
            // scalar type
            types.AddType(s);

            // TODO: could just copy flags from s, remove scalar, add vector.. ?
            var baseFlags = (s.IsPlaceholder ? SandboxValueType.Flags.Placeholder : 0);

            // vector variants
            var vec2Type = new SandboxValueType(s.Name + "2", baseFlags | SandboxValueType.Flags.Vector2);
            types.AddType(vec2Type);
            var vec3Type = new SandboxValueType(s.Name + "3", baseFlags | SandboxValueType.Flags.Vector3);
            types.AddType(vec3Type);
            var vec4Type = new SandboxValueType(s.Name + "4", baseFlags | SandboxValueType.Flags.Vector4);
            types.AddType(vec4Type);

            // matrix variants
            for (int rows = 1; rows < 4; rows++)
            {
                string r = rows.ToString();
                var mat1Type = new SandboxValueType(s.Name + r + "x1", baseFlags | SandboxValueType.Flags.Matrix);
                types.AddType(mat1Type);
                var mat2Type = new SandboxValueType(s.Name + r + "x2", baseFlags | SandboxValueType.Flags.Matrix);
                types.AddType(mat2Type);
                var mat3Type = new SandboxValueType(s.Name + r + "x3", baseFlags | SandboxValueType.Flags.Matrix);
                types.AddType(mat3Type);
                var mat4Type = new SandboxValueType(s.Name + r + "x4", baseFlags | SandboxValueType.Flags.Matrix);
                types.AddType(mat4Type);
            }
        }

        // dynamic placeholder type for scalar or vectors
        types.AddType(new SandboxValueType("$dynamicVector", SandboxValueType.Flags.AnyVector | SandboxValueType.Flags.Placeholder));
        types.AddType(new SandboxValueType("$dynamicMatrix", SandboxValueType.Flags.Matrix | SandboxValueType.Flags.Placeholder));

        // texture types
        List<SandboxValueType> textureTypes = new List<SandboxValueType>()
        {
            //new ShaderType("Texture1D",           ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("Texture1DArray",      ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("Texture2D",           ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("Texture2DArray",      ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("Texture3D",           ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("TextureCube",         ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("TextureCubeArray",    ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("Texture2DMS",         ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("Texture2DMSArray",    ShaderType.Flags.Texture | ShaderType.Flags.Object),
            // ShaderType.FromDefinition<UnityTexture2DTypeDefinition>(),
            new SandboxValueType(new UnityTexture2DTypeDefinition()),
            //new ShaderType("UnityTexture2DArray",      ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("UnityTexture3D",           ShaderType.Flags.Texture | ShaderType.Flags.Object),
            //new ShaderType("UnityTextureCube",         ShaderType.Flags.Texture | ShaderType.Flags.Object),
        };

        foreach (var t in textureTypes)
        {
            types.AddType(t);
        }
        ;

        // sampler state type
        types.AddType(new SandboxValueType("SamplerState", SandboxValueType.Flags.Object));

        // TODO:
        types.SetReadOnly();

        return types;
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

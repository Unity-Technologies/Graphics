using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Types
{
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
            // TODO: $precision$
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
        types.AddType(new SandboxValueType("$dynamicVector$", SandboxValueType.Flags.AnyVector | SandboxValueType.Flags.Placeholder));
        types.AddType(new SandboxValueType("$dynamicMatrix$", SandboxValueType.Flags.Matrix | SandboxValueType.Flags.Placeholder));

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
            new SandboxValueType(new UnityTexture2DArrayTypeDefinition()),
            new SandboxValueType(new UnityTexture3DTypeDefinition()),
            new SandboxValueType(new UnityTextureCubeTypeDefinition()),
            new SandboxValueType(new UnitySamplerStateTypeDefinition()),
        };

        foreach (var t in textureTypes)
        {
            types.AddType(t);
        }

        // sampler state type
        types.AddType(new SandboxValueType("SamplerState", SandboxValueType.Flags.Object));

        // TODO:
        types.SetReadOnly();

        return types;
    }
}

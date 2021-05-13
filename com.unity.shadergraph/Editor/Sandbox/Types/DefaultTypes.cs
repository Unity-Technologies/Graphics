using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Types
{
    static Types BuildDefaultTypeSystem()
    {
        var types = new Types(null);

        List<SandboxType> scalarTypes = new List<SandboxType>()
        {
            new SandboxType("bool",   SandboxType.Flags.Scalar),
            new SandboxType("int",    SandboxType.Flags.Scalar),
            new SandboxType("half",   SandboxType.Flags.Scalar),
            new SandboxType("float",  SandboxType.Flags.Scalar),
            new SandboxType("double", SandboxType.Flags.Scalar),
            // TODO: $precision$
            new SandboxType("$precision", SandboxType.Flags.Scalar | SandboxType.Flags.Placeholder)
        };

        foreach (var s in scalarTypes)
        {
            // add the scalar type
            types.AddTypeInternal(s);

            var baseFlags = (s.IsPlaceholder ? SandboxType.Flags.Placeholder : 0);

            // vector variants
            for (int dim = 2; dim <= 4; dim++)
                types.AddTypeInternal(new VectorTypeDefinition(s, dim, baseFlags));

            // matrix variants
            for (int rows = 1; rows <= 4; rows++)
                for (int cols = 1; cols <= 4; cols++)
                    types.AddTypeInternal(new MatrixTypeDefinition(s, rows, cols, baseFlags));
        }

        // standard dynamic placeholder types
        types.AddTypeInternal(new SandboxType("$dynamicVector$", SandboxType.Flags.Vector | SandboxType.Flags.Placeholder));
        types.AddTypeInternal(new SandboxType("$dynamicMatrix$", SandboxType.Flags.Matrix | SandboxType.Flags.Placeholder));
        types.AddTypeInternal(new SandboxType("$dynamic$", SandboxType.Flags.Placeholder));

        // bare types -- TODO: should these use TextureTypeDefinition ?
        List<SandboxType> bareTypes = new List<SandboxType>()
        {
            new SandboxType("Texture1D",           SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("Texture1DArray",      SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("Texture2D",           SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("Texture2DArray",      SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("Texture3D",           SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("TextureCube",         SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("TextureCubeArray",    SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("Texture2DMS",         SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("Texture2DMSArray",    SandboxType.Flags.Texture | SandboxType.Flags.BareResource),
            new SandboxType("SamplerState",        SandboxType.Flags.SamplerState | SandboxType.Flags.BareResource)
        };
        foreach (var b in bareTypes)
            types.AddTypeInternal(b);

        // Unity wrapped resource types
        types.AddTypeInternal(new TextureTypeDefinition("UnityTexture2D"));
        types.AddTypeInternal(new TextureTypeDefinition("UnityTexture2DArray"));
        types.AddTypeInternal(new TextureTypeDefinition("UnityTexture3DArray"));
        types.AddTypeInternal(new TextureTypeDefinition("UnityTextureCube"));
        types.AddTypeInternal(new SamplerStateTypeDefinition("UnitySamplerState"));

        types.SetReadOnly();

        return types;
    }
}

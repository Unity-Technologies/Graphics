using System;
using UnityEditor.ShaderGraph;


[Serializable]
public abstract class UnityTextureBaseDefinition : SandboxValueTypeDefinition
{
    internal override bool AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        // defined in the include file -- TODO
        return false;
    }

    internal override void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        sb.Add(GetTypeName(), " ", id);
    }

    // TODO: make use an interface to add to an include collection
    // public override string GetIncludes()               { return "com.unity.core/textures.hlsl";  }
}


[Serializable]
public class UnitySamplerStateTypeDefinition : SandboxValueTypeDefinition
{
    public override int latestVersion => 1;

    public override SandboxValueType.Flags GetTypeFlags()
    {
        // TODO: not clear what category this falls into, struct and/or texture
        // Although it is really a struct,
        // I guess this is the default type we use to represent Texture, so...
        return SandboxValueType.Flags.Object;
    }

    public override string GetTypeName()
    {
        return "UnitySamplerState";
    }

    public override bool ValueEquals(SandboxValueTypeDefinition other)
    {
        return (other is UnitySamplerStateTypeDefinition);
    }

    internal override bool AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        // defined in the include file -- TODO
        return false;
    }

    internal override void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        sb.Add(GetTypeName(), " ", id);
    }

    // TODO: make use an interface to add to an include collection
    // public override string GetIncludes()               { return "com.unity.core/textures.hlsl";  }
}


[Serializable]
public class UnityTexture2DTypeDefinition : UnityTextureBaseDefinition
{
    public override int latestVersion => 1;

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
}


[Serializable]
public class UnityTexture3DTypeDefinition : UnityTextureBaseDefinition
{
    public override int latestVersion => 1;

    public override SandboxValueType.Flags GetTypeFlags()
    {
        // TODO: not clear what category this falls into, struct and/or texture
        // Although it is really a struct,
        // I guess this is the default type we use to represent Texture, so...
        return SandboxValueType.Flags.Texture | SandboxValueType.Flags.Object;
    }

    public override string GetTypeName()
    {
        return "UnityTexture3D";
    }

    public override bool ValueEquals(SandboxValueTypeDefinition other)
    {
        return (other is UnityTexture3DTypeDefinition);
    }
}


[Serializable]
public class UnityTexture2DArrayTypeDefinition : UnityTextureBaseDefinition
{
    public override int latestVersion => 1;

    public override SandboxValueType.Flags GetTypeFlags()
    {
        // TODO: not clear what category this falls into, struct and/or texture
        // Although it is really a struct,
        // I guess this is the default type we use to represent Texture, so...
        return SandboxValueType.Flags.Texture | SandboxValueType.Flags.Object;
    }

    public override string GetTypeName()
    {
        return "UnityTexture2DArray";
    }

    public override bool ValueEquals(SandboxValueTypeDefinition other)
    {
        return (other is UnityTexture2DArrayTypeDefinition);
    }
}


[Serializable]
public class UnityTextureCubeTypeDefinition : UnityTextureBaseDefinition
{
    public override int latestVersion => 1;

    public override SandboxValueType.Flags GetTypeFlags()
    {
        // TODO: not clear what category this falls into, struct and/or texture
        // Although it is really a struct,
        // I guess this is the default type we use to represent Texture, so...
        return SandboxValueType.Flags.Texture | SandboxValueType.Flags.Object;
    }

    public override string GetTypeName()
    {
        return "UnityTextureCube";
    }

    public override bool ValueEquals(SandboxValueTypeDefinition other)
    {
        return (other is UnityTextureCubeTypeDefinition);
    }
}

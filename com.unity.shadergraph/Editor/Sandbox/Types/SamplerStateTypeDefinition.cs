using System;
using UnityEngine;


[Serializable]
internal class SamplerStateTypeDefinition : SandboxTypeDefinition
{
    [SerializeField]
    string name;

    internal SamplerStateTypeDefinition(string name)
    {
        this.name = name;
    }

    public override string GetTypeName()
    {
        return name;
    }

    public override SandboxType.Flags GetTypeFlags()
    {
        return SandboxType.Flags.SamplerState;
    }

    public override bool ValueEquals(SandboxTypeDefinition other)
    {
        var otherSSType = other as SamplerStateTypeDefinition;
        if (otherSSType == null)
            return false;
        if (otherSSType == this)
            return true;
        return otherSSType.name == this.name;
    }

    // TODO: make use an interface to add to an include collection
    // public override string GetIncludes()               { return "com.unity.core/textures.hlsl";  }
}

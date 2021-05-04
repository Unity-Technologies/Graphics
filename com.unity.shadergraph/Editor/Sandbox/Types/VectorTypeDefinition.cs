using System;
using UnityEngine;

[Serializable]
public class VectorTypeDefinition : SandboxTypeDefinition
{
    [SerializeField]
    string name;

    [SerializeField]
    int dimension;

    [SerializeField]
    SandboxType.Flags flags;

    [SerializeField]
    SandboxType scalarType;

    internal VectorTypeDefinition(SandboxType scalarType, int dimension, SandboxType.Flags baseFlags)
    {
        if ((dimension < 1) || (dimension > 4) || (scalarType == null))
            throw new ArgumentOutOfRangeException();

        this.name = scalarType.Name + dimension.ToString();
        this.dimension = dimension;
        this.flags = baseFlags | SandboxType.Flags.Vector;
        this.scalarType = scalarType;
    }

    public override SandboxType.Flags GetTypeFlags()
    {
        return flags;
    }

    public override string GetTypeName()
    {
        return name;
    }

    public override bool ValueEquals(SandboxTypeDefinition other)
    {
        var otherVecType = other as VectorTypeDefinition;
        if (otherVecType == null)
            return false;

        if (otherVecType == this)
            return true;

        return (otherVecType.name == this.name) &&
            (otherVecType.dimension == this.dimension) &&
            (otherVecType.flags == this.flags) &&
            (otherVecType.scalarType.ValueEquals(this.scalarType));
    }

    public override int VectorDimension => dimension;
}

using System;
using UnityEngine;


[Serializable]
public class ArrayTypeDefinition : SandboxTypeDefinition
{
    [SerializeField]
    string name;

    [SerializeField]
    int elements;

    [SerializeField]
    SandboxType.Flags flags;

    [SerializeField]
    SandboxType elementType;

    internal ArrayTypeDefinition(SandboxType elementType, int elements, SandboxType.Flags baseFlags)
    {
        if ((elements < 0) || (elementType == null))
            throw new ArgumentOutOfRangeException();

        this.name = elementType.Name + "[" + elements.ToString() + "]";
        this.elements = elements;
        this.flags = baseFlags | SandboxType.Flags.Array;
        this.elementType = elementType;
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
        var otherVecType = other as ArrayTypeDefinition;
        if (otherVecType == null)
            return false;

        if (otherVecType == this)
            return true;

        return (otherVecType.name == this.name) &&
            (otherVecType.elements == this.elements) &&
            (otherVecType.flags == this.flags) &&
            (otherVecType.elementType.ValueEquals(this.elementType));
    }

    public override int ArrayElements => elements;
}

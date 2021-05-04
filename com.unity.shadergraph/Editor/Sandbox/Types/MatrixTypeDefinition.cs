using System;
using UnityEngine;

[Serializable]
public class MatrixTypeDefinition : SandboxTypeDefinition
{
    [SerializeField]
    string name;

    [SerializeField]
    int rows;

    [SerializeField]
    int cols;

    [SerializeField]
    SandboxType.Flags flags;

    [SerializeField]
    SandboxType scalarType;

    internal MatrixTypeDefinition(SandboxType scalarType, int rows, int cols, SandboxType.Flags baseFlags)
    {
        if ((rows < 1) || (rows > 4) || (cols < 1) || (cols > 4) || (scalarType == null))
            throw new ArgumentOutOfRangeException();

        this.name = scalarType.Name + rows.ToString() + "x" + cols.ToString();
        this.rows = rows;
        this.cols = cols;
        this.flags = baseFlags | SandboxType.Flags.Matrix;
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
        var otherMatType = other as MatrixTypeDefinition;
        if (otherMatType == null)
            return false;

        if (otherMatType == this)
            return true;

        return (otherMatType.name == this.name) &&
            (otherMatType.rows == this.rows) &&
            (otherMatType.cols == this.cols) &&
            (otherMatType.flags == this.flags) &&
            (otherMatType.scalarType.ValueEquals(this.scalarType));
    }

    public override int MatrixRows => rows;
    public override int MatrixColumns => cols;
}

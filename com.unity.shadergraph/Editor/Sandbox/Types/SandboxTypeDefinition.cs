using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;


[Serializable]
public abstract class SandboxTypeDefinition : JsonObject
{
    public abstract string GetTypeName();
    public abstract SandboxType.Flags GetTypeFlags();
    public abstract bool ValueEquals(SandboxTypeDefinition other);

    public virtual int VectorDimension => 0;
    public virtual int MatrixRows => 0;
    public virtual int MatrixColumns => 0;
    public virtual int ArrayElements => 0;

    internal virtual void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        sb.Add(GetTypeName(), " ", id);
    }

    internal virtual void AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        // no declaration by default
    }
}

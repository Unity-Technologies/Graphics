using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;


[Serializable]
public class StructTypeDefinition : SandboxTypeDefinition
{
    [SerializeField]
    string name;

    [SerializeField]
    List<Member> members;

    internal StructTypeDefinition(string name, List<Member> members)
    {
        this.name = name;
        this.members = members;
    }

    public override SandboxType.Flags GetTypeFlags()
    {
        return SandboxType.Flags.Struct;
    }

    public override string GetTypeName()
    {
        return name;
    }

    public override bool ValueEquals(SandboxTypeDefinition other)
    {
        var otherType = other as StructTypeDefinition;
        if (otherType == null)
            return false;

        if (otherType == this)
            return true;

        if ((otherType.name != name) || (otherType.members.Count != members.Count))
            return false;

        for (int i = 0; i < members.Count; i++)
        {
            if ((members[i].Name != otherType.members[i].Name) ||
                (members[i].Type.ValueEquals(otherType.members[i].Type)))
                return false;
        }

        return true;
    }

    internal override bool AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        sb.Add("struct ");
        sb.AddLine(name);
        sb.AddLine("{");
        foreach (var m in members)
        {
            m.Type.AddHLSLVariableDeclarationString(sb, m.Name);
            sb.AddLine(";");
        }
        sb.Add("}");
        return true;
    }

    internal override void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        sb.Add(name, " ", id);
    }

    [Serializable]
    public struct Member // would be marked readonly, except that makes it non-serializable
    {
        public Member(SandboxType type, string name)
        {
            this.type = type;
            this.name = name;
        }

        [SerializeField]
        SandboxType type;

        [SerializeField]
        string name;

        public SandboxType Type { get { return type; } }
        public string Name { get { return name; } }
    }
}

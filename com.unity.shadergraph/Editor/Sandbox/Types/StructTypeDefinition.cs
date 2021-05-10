using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;


[Serializable]
public class StructTypeDefinition : SandboxTypeDefinition
{
    // serialized state
    [SerializeField]
    string name;

    [SerializeField]
    List<Field> fields;

    [SerializeField]
    List<ShaderFunction> functions;

    // public API
    public IReadOnlyList<Field> Fields => fields?.AsReadOnly() ?? ListUtils.EmptyReadOnlyList<Field>();
    public IReadOnlyList<ShaderFunction> Functions => functions?.AsReadOnly() ?? ListUtils.EmptyReadOnlyList<ShaderFunction>();

    // public Builder
    public struct Builder
    {
        string name;
        List<Field> fields;
        List<ShaderFunction> functions;

        public Builder(string name)
        {
            this.name = name;
            this.fields = null;
            this.functions = null;
        }

        public void AddField(SandboxType type, string name)
        {
            if (fields == null)
                fields = new List<Field>();

            // TODO: check for name collision..

            fields.Add(new Field(type, name));
        }

        public void AddFunction(ShaderFunction function)
        {
            if (functions == null)
                functions = new List<ShaderFunction>();

            // TODO: check for name collision...

            functions.Add(function);
        }

        public StructTypeDefinition Build()
        {
            return new StructTypeDefinition(name, fields, functions);
        }
    }

    internal StructTypeDefinition(string name, List<Field> members, List<ShaderFunction> functions)
    {
        this.name = name;
        this.fields = members;
        this.functions = functions;
    }

    public override SandboxType.Flags GetTypeFlags()
    {
        return SandboxType.Flags.Struct | SandboxType.Flags.HasHLSLDeclaration;
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

        if ((otherType.name != name) || (otherType.fields.Count != fields.Count))
            return false;

        for (int i = 0; i < fields.Count; i++)
        {
            if ((fields[i].Name != otherType.fields[i].Name) ||
                (fields[i].Type.ValueEquals(otherType.fields[i].Type)))
                return false;
        }

        return true;
    }

    internal override void AddHLSLTypeDeclarationString(ShaderStringBuilder sb)
    {
        sb.Add("struct ");
        sb.AddLine(name);
        sb.AddLine("{");
        // declare the member fields
        foreach (var f in Fields)
        {
            f.Type.AddHLSLVariableDeclarationString(sb, f.Name);
            sb.AddLine(";");
        }
        // declare the member functions
        foreach (var f in Functions)
        {
            f.AppendHLSLDeclarationString(sb);
        }
        sb.AddLine("};");       // remember stucts must have a semicolon after their declaration in HLSL!  ;)
    }

    internal override void AddHLSLVariableDeclarationString(ShaderStringBuilder sb, string id)
    {
        sb.Add(name, " ", id);
    }

    [Serializable]
    public struct Field // would be marked readonly, except that makes it non-serializable
    {
        public Field(SandboxType type, string name)
        {
            this.type = type;
            this.name = name;
        }

        [SerializeField]
        SandboxType type;

        [SerializeField]
        string name;

        public SandboxType Type => type;
        public string Name => name;
    }
}

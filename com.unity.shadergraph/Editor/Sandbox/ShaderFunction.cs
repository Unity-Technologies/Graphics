using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;


[Serializable]
public class ShaderFunction : ShaderFunctionSignature
{
    public override int latestVersion => 1;

    // public API
    public string Body { get { return body; } }
    public virtual bool isGeneric => false;

    // state
    [SerializeField]
    List<JsonData<ShaderFunctionSignature>> functions;
    [SerializeField]
    List<string> includePaths;
    [SerializeField]
    string body;

    // constructor is internal, public must use the Builder class instead
    internal ShaderFunction(string name, List<Parameter> parameters, string body, List<JsonData<ShaderFunctionSignature>> functions, List<string> includePaths) : base(name, parameters)
    {
        this.body = body;                   // only null if externally implemented
        this.functions = functions;         // can be null or empty
        this.includePaths = includePaths;   // can be null or empty
    }

    // "new" here means hide the inherited ShaderFunctionSignature.Builder, and replace it with this declaration
    public new class Builder : ShaderFunctionSignature.Builder
    {
        protected ShaderBuilder body;
        internal List<JsonData<ShaderFunctionSignature>> functions;     // can't make this protected because JsonData is internal  :(
        protected List<string> includePaths;

        // builder-only state
        int currentIndent;
        int tabSize;

        public Builder(string name) : base(name)
        {
            this.body = new ShaderBuilder();
            this.currentIndent = 4;
            this.tabSize = 4;
        }

        public ShaderFunctionSignature AddFunctionInput(ShaderFunctionSignature sig)
        {
            // TODO: register as function input
            return sig;
        }

        public void ImplementedInIncludeFile(string filePath)
        {
            // TODO
            // declares that the function is implemented in an external include file
            // this flags the function name as external, non-renamable, and
            // automatically adds the include file as a dependency of the function
            AddIncludeFileDependency(filePath);
        }

        public void AddIncludeFileDependency(string filePath)
        {
            if (includePaths == null)
                includePaths = new List<string>();
            includePaths.Add(filePath);
        }

        public Arguments CallFunction(ShaderFunctionSignature func)
        {
            if (functions == null)
                functions = new List<JsonData<ShaderFunctionSignature>>();
            functions.Add(func);

            // Do we want to insert a special string marker to body,
            // so we can translate the function name at generation time?
            // Maybe name is enough as long as it is a locally unique identifier.
            Indentation();
            body.Add(func.Name);
            body.Add("(");

            return new Arguments(body, func.Parameters.Count);
        }

        public ref struct Arguments
        {
            internal ShaderBuilder body;
            bool first;
            int paramCount;
            int argCount;

            internal Arguments(ShaderBuilder body, int paramCount)
            {
                this.body = body;
                this.first = true;
                this.argCount = 0;
                this.paramCount = paramCount;
            }

            public void Add(string arg)
            {
                if (argCount <= paramCount)
                {
                    if (!first)
                        body.Add(", ");

                    body.Add(arg);
                    first = false;
                }
                else
                {
                    // ERROR: too many arguments
                    Debug.Log("ERROR: too many arguments");
                }
                argCount++;
            }

            public void Dispose()
            {
                if (argCount != paramCount)
                {
                    // ERROR: mismatched argument count
                    Debug.Log("ERROR: mismatched argument count");
                }
                body.AddLine(");");
                body = null;
            }
        }

        public void NewLine()
        {
            body.NewLine();
        }

        // could do generic implementation
        public void AddLine(string l0)
        {
            Indentation();
            body.Add(l0);
            body.NewLine();
        }

        public void AddLine(string l0, string l1)
        {
            Indentation();
            body.Add(l0);
            body.Add(l1);
            body.NewLine();
        }

        public void AddLine(string l0, string l1, string l2)
        {
            Indentation();
            body.Add(l0);
            body.Add(l1);
            body.Add(l2);
            body.NewLine();
        }

        public void AddLine(string l0, string l1, string l2, string l3)
        {
            Indentation();
            body.Add(l0);
            body.Add(l1);
            body.Add(l2);
            body.Add(l3);
            body.NewLine();
        }

        public void AddLine(string l0, string l1, string l2, string l3, string l4)
        {
            Indentation();
            body.Add(l0);
            body.Add(l1);
            body.Add(l2);
            body.Add(l3);
            body.Add(l4);
            body.NewLine();
        }

        public void AddLine(string l0, string l1, string l2, string l3, string l4, string l5)
        {
            Indentation();
            body.Add(l0);
            body.Add(l1);
            body.Add(l2);
            body.Add(l3);
            body.Add(l4);
            body.Add(l5);
            body.NewLine();
        }

        public Line LineScope()
        {
            Indentation();
            return new Line(body);
        }

        public Block BlockScope()
        {
            AddLine("{");
            Indent();
            return new Block(this);
        }

        public void Indent()
        {
            currentIndent += tabSize;
        }

        public void Deindent()
        {
            currentIndent -= tabSize;
        }

        public void Indentation()
        {
            // TODO: need a better way
            // for (int i = 0; i < currentIndent; i++)
            //    body.Add(" ");
        }

        public readonly ref struct Line
        {
            readonly ShaderBuilder body;

            // TODO: ideally we have a write-only ShaderBuilder here, without the ability to ConvertToString
            public ShaderBuilder sb => body;

            public Line(ShaderBuilder body)
            {
                this.body = body;
            }

            public void Add(string s0)
            {
                body.Add(s0);
            }

            public void Add(string s0, string s1)
            {
                body.Add(s0);
                body.Add(s1);
            }

            public void Add(string s0, string s1, string s2)
            {
                body.Add(s0);
                body.Add(s1);
                body.Add(s2);
            }

            public void Add(string s0, string s1, string s2, string s3)
            {
                body.Add(s0);
                body.Add(s1);
                body.Add(s2);
                body.Add(s3);
            }

            public void Add(string s0, string s1, string s2, string s3, string s4)
            {
                body.Add(s0);
                body.Add(s1);
                body.Add(s2);
                body.Add(s3);
                body.Add(s4);
            }

            public void Dispose()
            {
                // Debug.Log("Line Dispose");
                body.NewLine();
            }
        }

        public readonly ref struct Block
        {
            readonly ShaderFunction.Builder parent;

            public Block(ShaderFunction.Builder parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                Debug.Log("Block.Dispose");
                parent.Deindent();
                parent.AddLine("}");
            }
        }

        public new ShaderFunction Build()
        {
            var func = new ShaderFunction(name, parameters, body.ConvertToString(), functions, includePaths);

            // clear data so we can't accidentally re-use it
            this.name = null;
            this.parameters = null;
            this.body = null;
            this.functions = null;
            this.includePaths = null;

            return func;
        }
    }

    public sealed override bool Equals(object other)
    {
        return (other.GetType() == this.GetType()) && ValueEquals(other as ShaderFunction);
    }

    public bool Equals(ShaderFunction other)
    {
        return (other.GetType() == this.GetType()) && ValueEquals(other);
    }

    public override bool ValueEquals(ShaderFunctionSignature other)
    {
        // same reference means they are trivially equal
        // this is important to check first to reduce the work involved
        if (other == this)
            return true;

        if (other == null)
            return false;

        var otherFunc = other as ShaderFunction;
        if (otherFunc == null)
            return false;

        // Signature comparison
        if (!base.ValueEquals(other))
            return false;

        if (body != otherFunc.body)
            return false;

        if (parameters != otherFunc.parameters)
        {
            int inputCount = parameters?.Count ?? 0;
            int otherInputCount = otherFunc.parameters?.Count ?? 0;
            if (inputCount != otherInputCount)
                return false;

            // not obvious, but if either is null, count will be zero here
            for (int i = 0; i < inputCount; i++)
                if (!parameters[i].Equals(otherFunc.parameters[i]))
                    return false;
        }

        if (includePaths != otherFunc.includePaths)
        {
            int count = includePaths?.Count ?? 0;
            int otherCount = otherFunc.includePaths?.Count ?? 0;
            if (count != otherCount)
                return false;

            // not obvious, but if either is null, count will be zero here
            for (int i = 0; i < count; i++)
                if (!includePaths[i].Equals(otherFunc.includePaths[i]))
                    return false;
        }

        // this can get expensive if we have long function dependency chains that
        // use separate instances but are otherwise identical.
        // Try to minimize that by re-using shared global static ShaderFunctions
        // instead of creating multiple instances to represent the same function.
        if (functions != otherFunc.functions)
        {
            int count = functions?.Count ?? 0;
            int otherCount = otherFunc.functions?.Count ?? 0;
            if (count != otherCount)
                return false;

            // not obvious, but if either is null, count will be zero here
            for (int i = 0; i < count; i++)
                if (!functions[i].value.ValueEquals(otherFunc.functions[i].value))
                    return false;
        }

        return true;
    }

    public sealed override int GetHashCode()
    {
        // names should be unique enough that we can directly use them as the hash
        return name.GetHashCode();
    }

    /*
    internal string GetHLSLDeclarationString(string nameOverride = null)
    {
        ShaderStringBuilder sb = new ShaderStringBuilder();
        AppendHLSLDeclarationString(sb, nameOverride);
        return sb.ToCodeBlock();
    }
    */

    internal void AppendHLSLDeclarationString(ShaderStringBuilder sb, string nameOverride = null, string precision = "float")
    {
        var funcName = name;
        if (nameOverride != null)
            funcName = nameOverride;
        /*
                sb.EnsureCapacity(sb.Capacity + 64 +
                                    funcName.Length +
                                    body.Length +
                                    (inputs?.Count ?? 0) * 30 +
                                    (outputs?.Count ?? 0) * 35);
        */

        sb.Add("void ", funcName);
        /*
                if (genericTypes != null)
                {
                    for (int g = 0; g < genericTypes.Count; g++)
                    {
                        var gt = genericTypes[g].value;
                        var at = sb.FindLocalGenericType(gt.LocalName);
                        if (at == null)
                        {
                            sb.Add("_$");
                            sb.Add(gt.LocalName);
                        }
                        else
                        {
                            sb.Add("_", at.Name);
                        }
                    }
                }
        */
        sb.Add("(");

        bool first = true;
        if (parameters != null)
        {
            foreach (var p in parameters)
            {
                if (!first)
                    sb.Add(", ");
                first = false;

                if (p.IsInOut)
                    sb.Add("inout ");
                else if (p.IsInput)
                    sb.Add("in ");
                else if (p.IsOutput)
                    sb.Add("out ");

                p.Type.AddHLSLVariableDeclarationString(sb, p.Name);
            }
        }

        sb.AddLine(")");
        sb.AddLine("{");
        sb.Add(body);
        sb.AddLine("}");
    }
}

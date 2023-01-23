using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal static class ShaderBuilderExtensions
    {
        const string m_SpaceToken = " ";
        const string m_EqualToken = "=";
        const string m_ScopeToken = "::";
        const string m_SemicolonToken = ";";
        const string m_CommaToken = ",";
        const string m_BeginCurlyBraceToken = "{";
        const string m_EndCurlyBraceToken = "}";
        const string m_BeginParenthesisToken = "(";
        const string m_EndParenthesisToken = ")";
        const string m_StructKeyword = "struct";
        const string inoutKeyword = "inout";
        const string outKeyword = "out";
        public readonly ref struct SemicolonBlock
        {
            readonly ShaderBuilder parent;

            public SemicolonBlock(ShaderBuilder parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                parent.Deindent();
                parent.Indentation();
                parent.Add(m_EndCurlyBraceToken, m_SemicolonToken);
                parent.NewLine();
            }
        }

        public static SemicolonBlock BlockSemicolonScope(this ShaderBuilder builder)
        {
            builder.AddLine(m_BeginCurlyBraceToken);
            builder.Indent();
            return new SemicolonBlock(builder);
        }

        public static void AppendLine(this ShaderBuilder builder, string str)
        {
            builder.AddLine(str);
        }

        public static void AppendLines(this ShaderBuilder builder, string lines)
        {
            if (string.IsNullOrEmpty(lines))
                return;
            var splitLines = lines.Split('\n');
            var lineCount = splitLines.Length;
            var lastLine = splitLines[lineCount - 1];
            if (string.IsNullOrEmpty(lastLine) || lastLine == "\r")
                lineCount--;
            for (var i = 0; i < lineCount; i++)
                builder.AppendLine(splitLines[i].Trim('\r'));
        }

        public static void Append(this ShaderBuilder builder, string str)
        {
            builder.Add(str);
        }

        internal static void AppendScopeName(this ShaderBuilder builder, Block block)
        {
            const string blockSuffix = "Block";
            builder.Append(block.Name);
            builder.Append(blockSuffix);
        }

        internal static void AddVariableDeclarationString(this ShaderBuilder builder, ShaderType type, string name, string defaultValue = null)
        {
            builder.DeclareVariable(type, name, defaultValue);
        }

        internal static void AddVariableDeclarationStatement(this ShaderBuilder builder, ShaderType type, string name, string defaultValue = null)
        {
            builder.DeclareVariable(type, name, defaultValue);
        }

        internal static void AddVariableDeclarationStatement(this ShaderBuilder builder, Block.Builder blockBuilder, ShaderType type, string name, string defaultValue = null)
        {
            builder.DeclareVariable(type, name, defaultValue);
        }

        internal static void AddTypeDeclarationString(this ShaderBuilder builder, ShaderType structType)
        {
            if (structType.IsStruct)
                builder.DeclareStruct(structType);
        }

        // declare function
        internal static void AddDeclarationString(this ShaderBuilder builder, ShaderFunction function)
        {
            builder.DeclareFunction(function);
        }

        internal static void AddCallStatementWithReturn(this ShaderBuilder builder, ShaderFunction function, string returnVariableName, params string[] arguments)
        {
            builder.CallFunctionWithReturn(function, returnVariableName, arguments);
        }

        internal static void AddCallStatementWithNewReturn(this ShaderBuilder builder, ShaderFunction function, string returnVariableName, params string[] arguments)
        {
            builder.CallFunctionWithDeclaredReturn(function, function.ReturnType, returnVariableName, arguments);
        }

        internal static void DeclareAttribute(this ShaderBuilder builder, ShaderAttribute attribute)
        {
            builder.Add("[");
            builder.Add(attribute.Name);
            var paramCount = 0;
            foreach (var param in attribute.Parameters)
            {
                if (paramCount == 0)
                    builder.Add("(");
                else
                    builder.Add(", ");
                ++paramCount;
                builder.Add(param.Value);
            }
            if (paramCount != 0)
                builder.Add(")");
            builder.Add("]");
        }
    }
}

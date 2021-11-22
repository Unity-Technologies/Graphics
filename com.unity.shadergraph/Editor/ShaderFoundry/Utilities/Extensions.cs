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
    }

    internal static class ShaderFunctionExtensions
    {
        // Get the input and output types for this function (assumed to be an entry point)
        internal static bool GetInOutTypes(this ShaderFunction function, out ShaderType inputType, out ShaderType outputType)
        {
            if (function.IsValid)
            {
                var parameters = function.Parameters.GetEnumerator();
                if (parameters.MoveNext())
                {
                    inputType = parameters.Current.Type;
                    outputType = function.ReturnType;
                    return true;
                }
            }
            inputType = outputType = ShaderType.Invalid;
            return false;
        }
    }

    static class VariableLinkInstanceExtensions
    {
        internal static void Declare(this VariableLinkInstance varInstance, ShaderBuilder builder)
        {
            if (varInstance.Parent != null)
            {
                const string dotToken = ".";
                Declare(varInstance.Parent, builder);
                builder.Add(dotToken);
            }
            builder.Add(varInstance.Name);
        }

        internal static string GetDeclarationString(this VariableLinkInstance varInstance)
        {
            ShaderBuilder builder = new ShaderBuilder();
            varInstance.Declare(builder);
            return builder.ToString();
        }
    }

    static class BlockBuilderExtensions
    {
        internal static void MergeTypesAndFunctions(this Block.Builder builder, Block block)
        {
            // Make sure to visit referenced items before owned. Owned items may depend on referenced ones.
            foreach (var item in block.ReferencedTypes)
                builder.AddReferencedType(item);
            foreach (var item in block.Types)
                builder.AddReferencedType(item);
            foreach (var item in block.ReferencedFunctions)
                builder.AddReferencedFunction(item);
            foreach (var item in block.Functions)
                builder.AddReferencedFunction(item);
        }

        internal static void MergeDescriptors(this Block.Builder builder, Block block)
        {
            foreach (var item in block.Commands)
                builder.AddCommand(item);
            foreach (var item in block.Defines)
                builder.AddDefine(item);
            foreach (var item in block.Includes)
                builder.AddInclude(item);
            foreach (var item in block.Keywords)
                builder.AddKeyword(item);
            foreach (var item in block.Pragmas)
                builder.AddPragma(item);
        }

        internal static void MergeTypesFunctionsDescriptors(this Block.Builder builder, Block block)
        {
            builder.MergeTypesAndFunctions(block);
            builder.MergeDescriptors(block);
        }

        internal static void BuildInterface(this Block.Builder blockBuilder, ShaderContainer container, ShaderFunction entryPointFn)
        {
            var parameters = entryPointFn.Parameters.GetEnumerator();
            if (!parameters.MoveNext())
                return;

            blockBuilder.BuildInterface(container, parameters.Current.Type, entryPointFn.ReturnType);
        }

        internal static void BuildInterface(this Block.Builder blockBuilder, ShaderContainer container, ShaderType inputType, ShaderType outputType)
        {
            BlockVariable.Builder BuildFromField(StructField field)
            {
                var varBuilder = new BlockVariable.Builder(container);
                varBuilder.DisplayName = varBuilder.ReferenceName = field.Name;
                varBuilder.Type = field.Type;
                foreach (var attribute in field.Attributes)
                    varBuilder.AddAttribute(attribute);
                string defaultValueStr = field.Attributes.FindFirstAttributeParamValue(CommonShaderAttributes.DefaultValue, 0);
                if (defaultValueStr != null)
                    varBuilder.DefaultExpression = defaultValueStr;
                return varBuilder;
            }

            foreach (var field in inputType.StructFields)
            {
                var builder = BuildFromField(field);
                var input = builder.Build();
                blockBuilder.AddInput(input);
                if (input.Attributes.FindFirst(CommonShaderAttributes.Property).IsValid)
                    blockBuilder.AddProperty(input);
            }
            foreach (var field in outputType.StructFields)
            {
                var builder = BuildFromField(field);
                blockBuilder.AddOutput(builder.Build());
            }
        }
    }

    internal static class SwizzleUtils
    {
        // Convert the string to 4 channels per element where each bit corresponds to the element
        internal static bool FromString(string swizzle, out int result)
        {
            result = 0;
            if (swizzle == null)
                return true;

            if (swizzle.Length > 4)
                return false;

            for (var i = 0; i < swizzle.Length; ++i)
            {
                var charValue = swizzle[i];
                int swizzleIndex = charValue - 'x';
                if (swizzleIndex < -1 || 3 <= swizzleIndex)
                    return false;

                if (swizzleIndex == -1)
                    swizzleIndex = 3;

                var elementMask = (1 << swizzleIndex);
                result |= elementMask << (i * 4);
            }
            return true;
        }

        internal static string ToString(int value)
        {
            if (value == 0)
                return null;

            string result = "";
            for (var i = 0; i < 4; ++i)
            {
                var mask = (value >> (i * 4)) & 0b1111;
                if (mask == 0)
                    break;
                else if (mask == 0b0001)
                    result += 'x';
                else if (mask == 0b0010)
                    result += 'y';
                else if (mask == 0b0100)
                    result += 'z';
                else if (mask == 0b1000)
                    result += 'w';
            }
            return result;
        }

        // Get the vector size of the swizzle
        internal static int GetCount(int value)
        {
            for (var i = 0; i < 4; ++i)
            {
                var mask = (value >> (i * 4)) & 0b1111;
                if (mask == 0)
                    return i;
            }
            return 4;
        }

        // Finds the largest size required to swizzle (e.g. vector.zx requires a size of 3)
        internal static int GetRequiredSize(int value)
        {
            int result = 0;
            for (var i = 0; i < 4; ++i)
            {
                var mask = (value >> (i * 4)) & 0b1111;
                if (mask == 0)
                    break;
                int elementSize = 0;
                if (mask == 0b0001)
                    elementSize = 1;
                else if (mask == 0b0010)
                    elementSize = 2;
                if (mask == 0b0100)
                    elementSize = 3;
                if (mask == 0b1000)
                    elementSize = 4;
                result = (elementSize > result) ? elementSize : result;
            }
            return result;
        }
    }
}

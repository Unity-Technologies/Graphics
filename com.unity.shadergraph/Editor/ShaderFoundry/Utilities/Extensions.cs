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

        static void AppendTypeName(this ShaderBuilder builder, ShaderType type, Block currentScope)
        {
            if (currentScope == type.ParentBlock)
                builder.Add(type.Name);
            else
                builder.AppendFullyQualifiedName(type);
        }

        internal static void AddVariableDeclarationString(this ShaderBuilder builder, ShaderType type, string name, string defaultValue = null)
        {
            builder.AppendFullyQualifiedName(type);
            builder.Append(m_SpaceToken);
            builder.Append(name);
            if (!string.IsNullOrEmpty(defaultValue))
                builder.Add(m_SpaceToken, m_EqualToken, m_SpaceToken, defaultValue);
        }

        internal static void AddVariableDeclarationStatement(this ShaderBuilder builder, ShaderType type, string name, string defaultValue = null)
        {
            builder.Indentation();
            builder.AddVariableDeclarationString(type, name, defaultValue);
            builder.Add(m_SemicolonToken);
            builder.NewLine();
        }

        internal static void AppendFullyQualifiedName(this ShaderBuilder builder, ShaderType type)
        {
            var parentBlock = type.ParentBlock;
            if (parentBlock.IsValid)
            {
                builder.AppendScopeName(parentBlock);
                builder.Append(m_ScopeToken);
                builder.Append(type.Name);
                return;
            }
            builder.Append(type.Name);
        }

        internal static void AddTypeDeclarationString(this ShaderBuilder builder, ShaderType type)
        {
            builder.AddLine(m_StructKeyword, m_SpaceToken, type.Name);

            using (builder.BlockSemicolonScope())
            {
                foreach (var field in type.StructFields)
                {
                    builder.Indentation();
                    builder.AddVariableDeclarationString(field.Type, field.Name);
                    builder.Add(m_SemicolonToken);
                    builder.NewLine();
                }
            }
        }

        internal static void AppendFullyQualifiedName(this ShaderBuilder builder, ShaderFunction function)
        {
            var parentBlock = function.ParentBlock;
            if (parentBlock.IsValid)
            {
                builder.AppendScopeName(parentBlock);
                builder.Add(m_ScopeToken, function.Name);
                return;
            }
            builder.Add(function.Name);
        }

        internal static void AddDeclarationString(this ShaderBuilder builder, ShaderFunction function)
        {
            var parentBlock = function.ParentBlock;
            builder.Indentation();
            builder.AppendTypeName(function.ReturnType, parentBlock);
            builder.Add(m_SpaceToken, function.Name, m_BeginParenthesisToken);

            var paramIndex = 0;
            foreach (var param in function.Parameters)
            {
                if (paramIndex != 0)
                    builder.Add(", ");
                if (param.IsOutput)
                {
                    if (param.IsInput)
                        builder.Add(inoutKeyword, m_SpaceToken);
                    else
                        builder.Add(outKeyword, m_SpaceToken);
                }

                builder.AppendTypeName(param.Type, parentBlock);
                builder.Add(m_SpaceToken, param.Name);
                ++paramIndex;
            }
            builder.Add(m_EndParenthesisToken);
            builder.NewLine();

            builder.AddLine(m_BeginCurlyBraceToken);
            builder.Indent();

            builder.Add(function.Body);

            builder.Deindent();
            builder.AddLine(m_EndCurlyBraceToken);
        }

        internal static void AddCallString(this ShaderBuilder builder, ShaderFunction function, params string[] arguments)
        {
            // Can't yet use builder.Call due to namespacing
            builder.AppendFullyQualifiedName(function);
            builder.Add(m_BeginParenthesisToken);
            for (var i = 0; i < arguments.Length; ++i)
            {
                builder.Add(arguments[i]);
                if (i != arguments.Length - 1)
                    builder.Add(m_CommaToken, m_SpaceToken);
            }
            builder.Add(m_EndParenthesisToken);
        }

        internal static void AddCallStatementWithReturn(this ShaderBuilder builder, ShaderFunction function, string returnVariableName, params string[] arguments)
        {
            builder.Indentation();
            builder.Add(returnVariableName);
            builder.Add(m_SpaceToken, m_EqualToken, m_SpaceToken);
            builder.AddCallString(function, arguments);
            builder.Add(m_SemicolonToken);
        }

        internal static void AddCallStatementWithNewReturn(this ShaderBuilder builder, ShaderFunction function, string returnVariableName, params string[] arguments)
        {
            builder.Indentation();
            builder.AddVariableDeclarationString(function.ReturnType, returnVariableName);
            builder.Add(m_SpaceToken, m_EqualToken, m_SpaceToken);
            builder.AddCallString(function, arguments);
            builder.Add(m_SemicolonToken);
            builder.NewLine();
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

    internal static class BlockVariableExtensions
    {
        internal static void DeclarePassProperty(this BlockProperty prop, UniformDeclarationContext context)
        {
            var passProps = PassPropertyInfo.Extract(prop);
            foreach (var passProp in passProps)
            {
                passProp.Declare(context);
            }
        }

        internal static void CopyPassPassProperty(this BlockVariable variable, ShaderFunction.Builder builder, BlockVariableLinkInstance owningVariable)
        {
            var passProps = PassPropertyInfo.Extract(variable);
            foreach (var passProp in passProps)
            {
                passProp.Copy(builder, owningVariable);
            }
        }

        internal static void DeclareMaterialProperty(this BlockProperty prop, ShaderBuilder sb)
        {
            var props = MaterialPropertyInfo.Extract(prop);
            foreach (var matProp in props)
            {
                matProp.Declare(sb, prop.ReferenceName);
            }
        }

        internal static BlockVariable Clone(this BlockVariable variable, ShaderContainer container)
        {
            return variable.Clone(container, variable.ReferenceName, variable.DisplayName);
        }

        internal static BlockVariable Clone(this BlockVariable variable, ShaderContainer container, string newName)
        {
            return variable.Clone(container, newName, newName);
        }

        internal static BlockVariable Clone(this BlockVariable variable, ShaderContainer container, string referenceName, string displayName)
        {
            var builder = new BlockVariable.Builder(container);
            builder.Type = variable.Type;
            builder.ReferenceName = referenceName;
            builder.DisplayName = displayName;
            builder.DefaultExpression = variable.DefaultExpression;
            foreach (var attribute in variable.Attributes)
                builder.AddAttribute(attribute);
            return builder.Build();
        }
    }

    static class BlockVariableLinkInstanceExtensions
    {
        internal static void Declare(this BlockVariableLinkInstance varInstance, ShaderBuilder builder)
        {
            if (varInstance.Owner != null)
            {
                const string dotToken = ".";
                Declare(varInstance.Owner, builder);
                builder.Add(dotToken);
            }
            builder.Add(varInstance.ReferenceName);
        }

        internal static string GetDeclarationString(this BlockVariableLinkInstance varInstance)
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

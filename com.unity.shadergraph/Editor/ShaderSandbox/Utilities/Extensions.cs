using UnityEditor.ShaderSandbox;
using BlockProperty = UnityEditor.ShaderSandbox.BlockVariable;

namespace ShaderSandbox
{
    public static class ShaderBuilderExtensions
    {
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
                parent.AddLine("};");
            }
        }

        public static SemicolonBlock BlockSemicolonScope(this ShaderBuilder builder)
        {
            builder.AddLine("{");
            builder.Indent();
            return new SemicolonBlock(builder);
        }

        public static void AppendLine(this ShaderBuilder builder, string str)
        {
            builder.AddLine(str);
        }

        public static void Append(this ShaderBuilder builder, string str)
        {
            builder.Add(str);
        }
    }

    internal static class ShaderFunctionExtensions
    {
        // Get the name of the input and output type for this function (assumed to be an entry point)
        internal static bool GetInOutTypeNames(this ShaderFunction function, out string inputTypeName, out string outputTypeName)
        {
            if (function.IsValid)
            {
                var parameters = function.Parameters.GetEnumerator();
                if (parameters.MoveNext())
                {
                    inputTypeName = parameters.Current.Type.Name;
                    outputTypeName = function.ReturnType.Name;
                    return true;
                }
            }
            inputTypeName = outputTypeName = null;
            return false;
        }

        internal static void AddDeclarationString(this ShaderFunction function, ShaderBuilder builder)
        {
            builder.Indentation();
            builder.Add($"{function.ReturnType.Name} {function.Name}(");

            var paramIndex = 0;
            foreach(var param in function.Parameters)
            {
                if (paramIndex != 0)
                    builder.Add(", ");
                builder.Add($"{param.Type.Name} {param.Name}");
                ++paramIndex;
            }
            builder.Add(")");
            builder.NewLine();

            builder.AddLine("{");
            builder.Indent();

            builder.Add(function.Body);

            builder.Deindent();
            builder.AddLine("}");
        }
    }

    internal static class TypeExtensions
    {
        internal static void AddVariableDeclarationString(this ShaderType type, ShaderBuilder builder, string name)
        {
            builder.Add($"{type.Name} {name}");
        }

        internal static void AddTypeDeclarationString(this ShaderType type, ShaderBuilder builder)
        {
            builder.AddLine($"struct {type.Name}");

            using (builder.BlockSemicolonScope())
            {
                foreach (var field in type.StructFields)
                {
                    builder.Indentation();
                    field.Type.AddVariableDeclarationString(builder, field.Name);
                    builder.Add(";");
                    builder.NewLine();
                }
            }
        }

        internal static bool Equals(this ShaderType self, ShaderType rhs)
        {
            return self.Name == rhs.Name;
        }
    }

    internal static class BlockVariableExtensions
    {
        internal static void DeclarePassProperty(this BlockProperty prop, ShaderBuilder perMaterialBuilder, ShaderBuilder globalBuilder)
        {
            var passProps = PassPropertyInfo.Extract(prop);
            foreach (var passProp in passProps)
            {
                passProp.Declare(perMaterialBuilder, globalBuilder, prop.ReferenceName);
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
            var builder = new BlockVariable.Builder();
            builder.Type = variable.Type;
            builder.ReferenceName = referenceName;
            builder.DisplayName = displayName;
            builder.DefaultExpression = variable.DefaultExpression;
            foreach (var attribute in variable.Attributes)
                builder.AddAttribute(attribute);
            return builder.Build(container);
        }
    }

    static class BlockVariableLinkInstanceExtensions
    {
        internal static void Declare(this BlockVariableLinkInstance varInstance, ShaderBuilder builder)
        {
            if (varInstance.Owner != null)
            {
                Declare(varInstance.Owner, builder);
                builder.Add(".");
            }
            builder.Add(varInstance.ReferenceName);
        }
    }

    static class BlockBuilderExtensions
    {
        internal static void MergeTypesAndFunctions(this Block.Builder builder, Block block)
        {
            foreach (var item in block.Types)
                builder.AddType(item);
            foreach (var item in block.Functions)
                builder.AddFunction(item);
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
            for(var i = 0; i < 4; ++i)
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

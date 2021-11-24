using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    internal class BlockDisassembler
    {
        internal void Generate(ShaderBuilder builder, Block block)
        {
            builder.AddLine($"Block \"{block.Name}\"");
            using (var scope = builder.BlockSemicolonScope())
            {
                WriteVariableScopeBlock(builder, "Inputs", block.Inputs);
                WriteVariableScopeBlock(builder, "Outputs", block.Outputs);
                WriteVariableScopeBlock(builder, "Properties", block.Properties());

                foreach (var type in block.Types)
                    Write(builder, type);
                foreach (var function in block.Functions)
                    Write(builder, function);
            }
        }

        internal void WriteVariableScopeBlock(ShaderBuilder builder, string scopeName, IEnumerable<BlockVariable> variables)
        {
            builder.AddLine(scopeName);
            using (var scope = builder.BlockSemicolonScope())
            {
                foreach(var variable in variables)
                {
                    builder.Indentation();
                    Write(builder, variable.Attributes, variable.Type, variable.Name);
                    builder.Append(";");
                    builder.NewLine();
                }
            }
        }

        internal void Write(ShaderBuilder builder, ShaderType type)
        {
            Write(builder, type.Attributes);
            builder.AddLine($"struct {type.Name}");
            using (var scope = builder.BlockSemicolonScope())
            {
                foreach(var field in type.StructFields)
                {
                    builder.Indentation();
                    Write(builder, field.Attributes, field.Type, field.Name);
                    builder.Append(";");
                    builder.NewLine();
                }
            }
        }

        internal void Write(ShaderBuilder builder, ShaderFunction function)
        {
            //Write(builder, function.a);
            builder.Indentation();
            builder.Append($"{function.ReturnType.Name} {function.Name}(");
            Write(builder, function.Parameters);
            builder.Append($")");
            builder.NewLine();
            builder.AddLine("{");
            builder.Indent();
            builder.Append(function.Body);
            builder.Deindent();
            builder.AddLine("}");
        }

        internal void Write(ShaderBuilder builder, IEnumerable<FunctionParameter> parameters)
        {
            bool isFirst = true;
            foreach (var parameter in parameters)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }
                Write(builder, parameter);
                isFirst = false;
            }
        }

        internal void Write(ShaderBuilder builder, FunctionParameter parameter)
        {
            Write(builder, null, parameter.Type, parameter.Name);
        }

        internal void Write(ShaderBuilder builder, IEnumerable<ShaderAttribute> attributes, ShaderType type, string name)
        {
            Write(builder, attributes);
            builder.Append($"{type.Name} {name}");
        }

        internal void Write(ShaderBuilder builder, IEnumerable<ShaderAttribute> attributes)
        {
            if (attributes == null)
                return;

            foreach (var attribute in attributes)
                Write(builder, attribute);
        }

        internal void Write(ShaderBuilder builder, ShaderAttribute attribute)
        {
            builder.Append("[");
            builder.Append(attribute.Name);
            if(attribute.Parameters.Count() != 0)
            {
                builder.Append("(");
                Write(builder, attribute.Parameters);
                builder.Append(")");
            }
            builder.Append("]");
        }

        internal void Write(ShaderBuilder builder, IEnumerable<ShaderAttributeParam> parameters)
        {
            bool isFirst = true;
            foreach(var parameter in parameters)
            {
                if(!isFirst)
                {
                    builder.Append(", ");
                }
                Write(builder, parameter);
                isFirst = false;
            }
        }

        internal void Write(ShaderBuilder builder, ShaderAttributeParam parameter)
        {
            if (parameter.Name != null)
            {
                builder.Append($"\"{parameter.Name}\"");
                builder.Append(", ");
            }
            builder.Append($"\"{parameter.Value}\"");
        }
    }
}

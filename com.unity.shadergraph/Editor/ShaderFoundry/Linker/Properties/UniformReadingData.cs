namespace UnityEditor.ShaderFoundry
{
    class UniformReadingData
    {
        internal string Rhs;
        internal delegate void ReadUniformDelegate(ShaderBuilder builder, VariableLinkInstance owningVariable);
        internal ReadUniformDelegate ReadUniformCallback = null;

        internal static UniformReadingData BuildSimple(FieldPropertyContext context, FieldPropertyData resultProperty)
        {
            var result = new UniformReadingData();
            // There's no uniform, make sure to zero initialize the property if possible
            if (context.DataSource == UniformDataSource.None)
                result.ReadUniformCallback = ZeroInitializeVariable;
            else
                result.Rhs = BuildStandardReadingExpression(context.FieldType, context.UniformName, context.DataSource);
            resultProperty.UniformReadingData = result;
            return result;
        }

        internal void Copy(ShaderBuilder builder, VariableLinkInstance owningVariable)
        {
            if (ReadUniformCallback != null)
                ReadUniformCallback(builder, owningVariable);
            else
            {
                string variableDeclaration = owningVariable.GetDeclarationString();
                builder.AddLine($"{variableDeclaration} = {Rhs};");
            }
        }

        static string BuildStandardReadingExpression(ShaderType type, string name, UniformDataSource dataSource)
        {
            if (dataSource == UniformDataSource.PerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({name}, {type.Name})";
            else
                return name;
        }

        static void ZeroInitializeVariable(ShaderBuilder builder, VariableLinkInstance owningVariable)
        {
            var fieldType = owningVariable.Type;
            if (fieldType.IsVectorOrScalar || fieldType.IsMatrix || fieldType.IsArray)
                builder.AddLine($"ZERO_INITIALIZE({owningVariable.Type.Name}, {owningVariable.GetDeclarationString()});");
        }
    }
}

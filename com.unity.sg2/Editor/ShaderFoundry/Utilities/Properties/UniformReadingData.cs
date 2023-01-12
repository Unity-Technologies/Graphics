namespace UnityEditor.ShaderFoundry
{
    class UniformReadingData
    {
        internal string Rhs;

        internal delegate void WriteFieldNameDelegate(ShaderBuilder builder);
        internal delegate void ReadUniformDelegate(ShaderBuilder builder, WriteFieldNameDelegate callback);
        internal ReadUniformDelegate ReadUniformCallback = null;

        internal static UniformReadingData BuildSimple(FieldPropertyContext context, FieldPropertyData resultProperty)
        {
            var result = new UniformReadingData();
            if (context.DataSource != UniformDataSource.None)
                result.Rhs = BuildStandardReadingExpression(context.FieldType, context.UniformName, context.DataSource);
            resultProperty.UniformReadingData = result;
            return result;
        }

        internal void Copy(ShaderBuilder builder, WriteFieldNameDelegate callback)
        {
            if (ReadUniformCallback != null)
                ReadUniformCallback(builder, callback);
            else
            {
                builder.Indentation();
                callback(builder);
                builder.Add($" = {Rhs};");
                builder.NewLine();
            }
        }

        internal static string BuildStandardReadingExpression(ShaderType type, string name, UniformDataSource dataSource)
        {
            if (dataSource == UniformDataSource.PerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({name}, {type.Name})";
            else
                return name;
        }
    }
}

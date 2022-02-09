namespace UnityEditor.ShaderFoundry
{
    class UniformDeclarationData
    {
        internal ShaderType Type;
        internal string Name;
        internal UniformDataSource dataSource;
        // If non-null, this string is used to as the declaration instead of it being auto generated from the type and name.
        internal string DeclarationOverride = null;

        internal static UniformDeclarationData BuildSimple(FieldPropertyContext context, FieldPropertyData resultProperty)
        {
            if (context.DataSource == UniformDataSource.None)
                return null;

            var uniformInfo = new UniformDeclarationData
            {
                Type = context.FieldType,
                Name = context.UniformName,
                dataSource = context.DataSource
            };
            resultProperty.UniformDeclarations.Add(uniformInfo);
            return uniformInfo;
        }

        internal void Declare(UniformDeclarationContext context)
        {
            if (DeclarationOverride != null)
            {
                DeclareUniform(context, DeclarationOverride, dataSource);
                return;
            }

            if (Type == Type.Container._Texture2D)
                DeclareUniform(context, $"TEXTURE2D({Name})", UniformDataSource.Global);
            else if (Type == Type.Container._Texture2DArray)
                DeclareUniform(context, $"TEXTURE2D_ARRAY({Name})", UniformDataSource.Global);
            else if (Type == Type.Container._TextureCube)
                DeclareUniform(context, $"TEXTURECUBE({Name})", UniformDataSource.Global);
            else if (Type == Type.Container._Texture3D)
                DeclareUniform(context, $"TEXTURE3D({Name})", UniformDataSource.Global);
            else if (Type == Type.Container._SamplerState)
                DeclareUniform(context, $"SAMPLER({Name})", UniformDataSource.Global);
            else
                DeclareUniform(context, $"{Type.Name} {Name}", dataSource);
        }

        static void DeclareUniform(UniformDeclarationContext context, string uniformDeclaration, UniformDataSource dataSource)
        {
            if (uniformDeclaration == null || dataSource == UniformDataSource.None)
                return;

            var builder = context.PerMaterialBuilder;
            if (dataSource == UniformDataSource.Global)
                builder = context.GlobalBuilder;
            if (dataSource == UniformDataSource.PerInstance)
            {
                builder.AddLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                builder.AddLine($"{uniformDeclaration}_dummy;");
                builder.AddLine("#else // V2");
                builder.AddLine($"{uniformDeclaration};");
                builder.AddLine("#endif");
            }
            else
                builder.AddLine($"{uniformDeclaration};");
        }
    }
}

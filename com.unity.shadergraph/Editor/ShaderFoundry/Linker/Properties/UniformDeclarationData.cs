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
            return BuildSimple(context.FieldType, context.UniformName, context.DataSource, resultProperty);
        }

        internal static UniformDeclarationData BuildSimple(ShaderType uniformType, string uniformName, UniformDataSource dataSource, FieldPropertyData resultProperty)
        {
            if (dataSource == UniformDataSource.None)
                return null;

            var uniformInfo = new UniformDeclarationData
            {
                Type = uniformType,
                Name = uniformName,
                dataSource = dataSource,
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

            // TODO @ SHADERS: This currently only handles global and per-material cbuffers. We need to update this later to be more robust.
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

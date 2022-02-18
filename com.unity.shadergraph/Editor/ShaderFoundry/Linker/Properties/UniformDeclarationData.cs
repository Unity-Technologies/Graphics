namespace UnityEditor.ShaderFoundry
{
    class UniformDeclarationData
    {
        readonly internal ShaderType Type;
        readonly internal string Name;
        readonly internal UniformDataSource DataSource;
        readonly internal string CustomBufferName;
        // If non-null, this string is used to as the declaration instead of it being auto generated from the type and name.
        readonly internal string DeclarationOverride = null;

        public UniformDeclarationData(ShaderType uniformType, string uniformName, UniformDataSource dataSource, string customBufferName)
        {
            Type = uniformType;
            Name = uniformName;
            DataSource = dataSource;
            CustomBufferName = customBufferName;
        }

        public UniformDeclarationData(string uniformName, string declarationOverride, UniformDataSource dataSource, string customBufferName)
        {
            Name = uniformName;
            DataSource = dataSource;
            CustomBufferName = customBufferName;
            DeclarationOverride = declarationOverride;
        }

        internal static UniformDeclarationData BuildFromField(FieldPropertyContext context, FieldPropertyData resultProperty)
        {
            return Build(context.FieldType, context.UniformName, context.DataSource, context.CustomBufferName, resultProperty);
        }

        internal static UniformDeclarationData BuildFromFieldWithOverrides(FieldPropertyContext context, ShaderType fieldType, string uniformName, FieldPropertyData resultProperty)
        {
            return Build(fieldType, uniformName, context.DataSource, context.CustomBufferName, resultProperty);
        }

        internal static UniformDeclarationData Build(ShaderType uniformType, string uniformName, UniformDataSource dataSource, string customBufferName, FieldPropertyData resultProperty)
        {
            if (dataSource == UniformDataSource.None)
                return null;

            var uniformInfo = new UniformDeclarationData(uniformType, uniformName, dataSource, customBufferName);
            resultProperty.UniformDeclarations.Add(uniformInfo);
            return uniformInfo;
        }

        internal static UniformDeclarationData BuildWithCustomDeclaration(string uniformName, string declarationOverride, UniformDataSource dataSource, string customBufferName, FieldPropertyData resultProperty)
        {
            if (dataSource == UniformDataSource.None)
                return null;

            var uniformInfo = new UniformDeclarationData(uniformName, declarationOverride, dataSource, customBufferName);
            resultProperty.UniformDeclarations.Add(uniformInfo);
            return uniformInfo;
        }

        internal void Declare(UniformBufferCollection uniformCollection)
        {
            if (DeclarationOverride != null)
            {
                DeclareUniform(uniformCollection, DeclarationOverride, DataSource, CustomBufferName);
                return;
            }

            if (Type == Type.Container._Texture2D)
                DeclareUniform(uniformCollection, $"TEXTURE2D({Name})", UniformDataSource.Global, null);
            else if (Type == Type.Container._Texture2DArray)
                DeclareUniform(uniformCollection, $"TEXTURE2D_ARRAY({Name})", UniformDataSource.Global, null);
            else if (Type == Type.Container._TextureCube)
                DeclareUniform(uniformCollection, $"TEXTURECUBE({Name})", UniformDataSource.Global, null);
            else if (Type == Type.Container._Texture3D)
                DeclareUniform(uniformCollection, $"TEXTURE3D({Name})", UniformDataSource.Global, null);
            else if (Type == Type.Container._SamplerState)
                DeclareUniform(uniformCollection, $"SAMPLER({Name})", UniformDataSource.Global, null);
            else
                DeclareUniform(uniformCollection, $"{Type.Name} {Name}", DataSource, CustomBufferName);
        }

        static void DeclareUniform(UniformBufferCollection uniformCollection, string uniformDeclaration, UniformDataSource dataSource, string customBufferName)
        {
            if (uniformDeclaration == null || dataSource == UniformDataSource.None)
                return;

            var bufferObj = uniformCollection.FindOrCreateBuffer(dataSource, customBufferName);
            var builder = bufferObj.Builder;

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

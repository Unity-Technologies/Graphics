using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.ShaderGraph
{
    class PropertyCollector
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public bool modifiable;
        }

        public readonly List<AbstractShaderProperty> properties = new List<AbstractShaderProperty>();

        public void AddShaderProperty(AbstractShaderProperty chunk)
        {
            if (properties.Any(x => x.referenceName == chunk.referenceName))
                return;
            properties.Add(chunk);
        }

        public string GetPropertiesBlock(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var prop in properties.Where(x => x.generatePropertyBlock))
            {
                for (var i = 0; i < baseIndentLevel; i++)
                {
                    //sb.Append("\t");
                    sb.Append("    "); // unity convention use space instead of tab...
                }
                sb.AppendLine(prop.GetPropertyBlockString());
            }
            return sb.ToString();
        }

        public string GetPropertiesDeclaration(int baseIndentLevel, GenerationMode mode)
        {
            var builder = new ShaderStringBuilder(baseIndentLevel);
            GetPropertiesDeclaration(builder, mode);
            return builder.ToString();
        }

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode)
        {
            var batchAll = mode == GenerationMode.Preview;
            builder.AppendLine("CBUFFER_START(UnityPerMaterial)");
            int instancedCount = 0;
            foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
            {
                if (!prop.gpuInstanced)
                    builder.AppendLine(prop.GetPropertyDeclarationString());
                else
                    instancedCount++;
            }

            if ( instancedCount > 0 )
            {
                builder.AppendLine("#ifndef UNITY_DOTS_INSTANCING_ENABLED");
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
                {
                    if (prop.gpuInstanced)
                        builder.AppendLine(prop.GetPropertyDeclarationString());
                }
                builder.AppendLine("#endif");
            }

            builder.AppendLine("CBUFFER_END");
            builder.AppendNewLine();

            if (batchAll)
                return;
            
            foreach (var prop in properties.Where(n => !n.isBatchable || !n.generatePropertyBlock))
            {
                builder.AppendLine(prop.GetPropertyDeclarationString());
            }
        }

        public  int GetDotsInstancingPropertiesCount(GenerationMode mode)
        {
            var batchAll = mode == GenerationMode.Preview;
            return properties.Where(n => (batchAll || (n.generatePropertyBlock && n.isBatchable)) && n.gpuInstanced).Count();
        }

        public string GetDotsInstancingPropertiesDeclaration(GenerationMode mode)
        {
            var builder = new ShaderStringBuilder();
            var batchAll = mode == GenerationMode.Preview;

            int instancedCount = GetDotsInstancingPropertiesCount(mode);

            if ( instancedCount > 0 )
            {
                builder.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
                builder.AppendLine("#define SHADER_GRAPH_GENERATED");
                builder.Append("#define DOTS_CUSTOM_ADDITIONAL_MATERIAL_VARS\t");

                int count = 0;
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
                {
                    if (prop.gpuInstanced)
                    {
                        string varName = $"{prop.referenceName}_Array";
                        string sType = prop.propertyType.GetHLSLType();
                        builder.Append("UNITY_DEFINE_INSTANCED_PROP({0}, {1})", sType, varName);
                        if ( count < instancedCount-1)
                            builder.Append("\\");
                        builder.AppendLine("");
                    }
                }
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
                {
                    if (prop.gpuInstanced)
                    {
                        string varName = $"{prop.referenceName}_Array";
                        builder.AppendLine("#define {0} UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, {1})", prop.referenceName, varName);
                    }
                }
            }
            builder.AppendLine("#endif");
            return builder.ToString();
        }

        public List<TextureInfo> GetConfiguredTexutres()
        {
            var result = new List<TextureInfo>();

            foreach (var prop in properties.OfType<TextureShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<Texture2DArrayShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.textureArray != null ? prop.value.textureArray.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<Texture3DShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<CubemapShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.cubemap != null ? prop.value.cubemap.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }
            return result;
        }
    }
}

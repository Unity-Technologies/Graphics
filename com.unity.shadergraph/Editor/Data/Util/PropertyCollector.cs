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
            // In preview we ignore the isBatchable of the properties and always bundle them in a constant buffer
            // not sure why...
            var batchAll = mode == GenerationMode.Preview;

            builder.AppendLine("CBUFFER_START(UnityPerMaterial)");
            foreach (var prop in properties)
            {
                string s = prop.GetPropertyDeclarationStringForBatchMode(AbstractShaderProperty.GenerationMode.InConstantBuffer);
                if (s != null) builder.AppendLine(s);
                if (batchAll)
                {
                    // BatchAll means BatchAll, so even things that would go in the root normally get batched in the CBUFFER
                    string s2 = prop.GetPropertyDeclarationStringForBatchMode(AbstractShaderProperty.GenerationMode.InRoot);
                    if (s2 != null) builder.AppendLine(s2);
                }
            }
            builder.AppendLine("CBUFFER_END");
            builder.AppendNewLine();

            if (batchAll)
                return;
            
            foreach (var prop in properties)
            {
                string s = prop.GetPropertyDeclarationStringForBatchMode(AbstractShaderProperty.GenerationMode.InRoot);
                if ( s != null) builder.AppendLine(s);
            }
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

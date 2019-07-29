using System.Collections.Generic;
using System.Linq;

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

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision inheritedPrecision)
        {
            foreach (var prop in properties)
            {
                prop.ValidateConcretePrecision(inheritedPrecision);
            }

            var cbDecls = new Dictionary<string, ShaderStringBuilder>();
            foreach (var prop in properties)
            {
                foreach (var (cbName, line) in prop.GetPropertyDeclarationStrings())
                {
                    var key = string.IsNullOrWhiteSpace(cbName) ? string.Empty : cbName;

                    //
                    // Old behaviours that I don't know why we do them:

                    // If the property is not exposed, put it to Global
                    if (key == AbstractShaderProperty.s_UnityPerMaterialCbName && !prop.generatePropertyBlock)
                        key = string.Empty;
                    // If we are in preview, put all CB variables to UnityPerMaterial CB
                    if (key != string.Empty && mode == GenerationMode.Preview)
                        key = AbstractShaderProperty.s_UnityPerMaterialCbName;

                    if (!cbDecls.TryGetValue(key, out var sb))
                    {
                        sb = new ShaderStringBuilder();
                        cbDecls.Add(key, sb);
                    }

                    if (line.Contains(System.Environment.NewLine))
                        // Don't append ; if cbName is empty and there are multiple lines - a hack for GradientShaderProperty to put some definitions in the global scope.
                        sb.AppendLines($"{line}{(string.IsNullOrWhiteSpace(cbName) ? "" : ";")}");
                    else
                        sb.AppendLine($"{line};");
                }
            }

            foreach (var kvp in cbDecls)
            {
                if (kvp.Key != string.Empty)
                    builder.AppendLine($"CBUFFER_START({kvp.Key})");
                builder.AppendLines(kvp.Value.ToString());
                if (kvp.Key != string.Empty)
                    builder.AppendLine($"CBUFFER_END");
            }
            builder.AppendNewLine();
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

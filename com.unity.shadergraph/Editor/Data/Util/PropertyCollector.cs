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

        private const string s_UnityPerMaterialCbName = "UnityPerMaterial";
        private const string s_UnitySplatMaterialCbName = "UnitySplatProperties";

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision inheritedPrecision, int splatCount)
        {
            foreach (var prop in properties)
            {
                prop.ValidateConcretePrecision(inheritedPrecision);
            }

            var cbDecls = new Dictionary<string, ShaderStringBuilder>();
            foreach (var prop in properties)
            {
                var cbName = prop.propertyType.IsBatchable() ? s_UnityPerMaterialCbName : string.Empty;

                //
                // Old behaviours that I don't know why we do them:

                // If the property is not exposed, put it to Global
                if (cbName == s_UnityPerMaterialCbName && !prop.generatePropertyBlock)
                    cbName = string.Empty;
                // If we are in preview, put all CB variables to UnityPerMaterial CB
                if (cbName != string.Empty && mode == GenerationMode.Preview)
                    cbName = s_UnityPerMaterialCbName;

                var splatProperty = prop as ISplattableShaderProperty;
                if (cbName == s_UnityPerMaterialCbName && splatProperty != null && splatProperty.splat)
                    cbName = s_UnitySplatMaterialCbName;

                if (!cbDecls.TryGetValue(cbName, out var sb))
                {
                    sb = new ShaderStringBuilder();
                    cbDecls.Add(cbName, sb);
                }

                if (prop is GradientShaderProperty gradientProperty)
                {
                    sb.AppendLine(gradientProperty.GetGradientDeclarationString());
                }
                else
                {
                    var referenceName = prop.referenceName;
                    if (splatProperty != null && splatProperty.splat)
                    {
                        for (int i = 0; i < splatCount; ++i)
                            sb.AppendLine($"{prop.propertyType.FormatDeclarationString(prop.concretePrecision, $"{referenceName}{i}")};");
                    }
                    else
                        sb.AppendLine($"{prop.propertyType.FormatDeclarationString(prop.concretePrecision, referenceName)};");
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

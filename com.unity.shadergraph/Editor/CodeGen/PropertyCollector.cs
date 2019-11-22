using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph.Internal;

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

            if (instancedCount > 0)
            {
                builder.AppendLine("// Hybrid instanced properties");
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
                {
                    if (prop.gpuInstanced)
                        builder.AppendLine(prop.GetPropertyDeclarationString());
                }
            }
            builder.AppendLine("CBUFFER_END");

            if (instancedCount > 0)
            {
                builder.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");

                builder.AppendLine("// DOTS instancing definitions");
                builder.AppendLine("UNITY_DOTS_INSTANCING_START(HybridPerMaterial)");
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
                {
                    if (prop.gpuInstanced)
                    {
                        builder.AppendLine($"    UNITY_DOTS_INSTANCED_PROP({prop.referenceName})");
                    }
                }
                builder.AppendLine("UNITY_DOTS_INSTANCING_END");

                builder.AppendLine("// DOTS instancing usage macros");
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.isBatchable)))
                {
                    if (prop.gpuInstanced)
                    {
                        var n = prop.referenceName;
                        string type = prop.concreteShaderValueType.ToShaderString(prop.concretePrecision);
                        builder.AppendLine($"#define {n} UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO({type}, Metadata_{n})");
                    }
                }

                builder.AppendLine("#endif");
            }

            if (batchAll)
                return;

            foreach (var prop in properties.Where(n => !n.isBatchable || !n.generatePropertyBlock))
            {
                builder.AppendLine(prop.GetPropertyDeclarationString());
            }
        }

        public int GetDotsInstancingPropertiesCount(GenerationMode mode)
        {
            // TODO: Refactor how DOTS instancing properties work
            var batchAll = mode == GenerationMode.Preview;
            return properties.Where(n => (batchAll || (n.generatePropertyBlock && n.isBatchable)) && n.gpuInstanced).Count();
        }

        public string GetDotsInstancingPropertiesDeclaration(GenerationMode mode)
        {
            // TODO: Refactor how DOTS instancing properties work
            return "";
        }

        public List<TextureInfo> GetConfiguredTexutres()
        {
            var result = new List<TextureInfo>();

            foreach (var prop in properties.OfType<Texture2DShaderProperty>())
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

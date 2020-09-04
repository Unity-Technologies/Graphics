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

            if (mode == GenerationMode.Preview)
            {
                builder.AppendLine("CBUFFER_START(UnityPerMaterial)");
                foreach (var prop in properties.Where(p => !p.gpuInstanced))    // all non-gpu instanced properties (even non-batchable ones) - preview is weird
                {
                    prop.AppendBatchablePropertyDeclarations(builder);
                    prop.AppendNonBatchablePropertyDeclarations(builder);
                }
                var GPUInstancedProperties = properties.Where(p => p.gpuInstanced);
                if (GPUInstancedProperties.Any())
                {
                    builder.AppendLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                    foreach (var prop in GPUInstancedProperties)
                    {
                        prop.AppendBatchablePropertyDeclarations(builder, "_dummy;");
                    }
                    builder.AppendLine("#else");
                    foreach (var prop in GPUInstancedProperties)
                    {
                        prop.AppendBatchablePropertyDeclarations(builder);
                    }
                    builder.AppendLine("#endif");
                }
                builder.AppendLine("CBUFFER_END");
                return;
            }

            // Hybrid V1 generates a special version of UnityPerMaterial, which has dummy constants for
            // instanced properties, and regular constants for other properties.
            // Hybrid V2 generates a perfectly normal UnityPerMaterial, but needs to append
            // a UNITY_DOTS_INSTANCING_START/END block after it that contains the instanced properties.

#if !ENABLE_HYBRID_RENDERER_V2
            builder.AppendLine("CBUFFER_START(UnityPerMaterial)");

            // non-GPU instanced properties go first in the UnityPerMaterial cbuffer
            var batchableProperties = properties.Where(n => n.generatePropertyBlock && n.hasBatchableProperties);
            foreach (var prop in batchableProperties)
            {
                if (!prop.gpuInstanced)
                    prop.AppendBatchablePropertyDeclarations(builder);
            }

            var batchableGPUInstancedProperties = batchableProperties.Where(p => p.gpuInstanced);
            if (batchableGPUInstancedProperties.Any())
            {
                builder.AppendLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                foreach (var prop in batchableGPUInstancedProperties)
                {
                    // TODO: why is this inserting a dummy value?  this won't work on complex properties...
                    prop.AppendBatchablePropertyDeclarations(builder, "_dummy;");
                }
                builder.AppendLine("#else");
                foreach (var prop in batchableGPUInstancedProperties)
                {
                    prop.AppendBatchablePropertyDeclarations(builder);
                }
                builder.AppendLine("#endif");
            }
            builder.AppendLine("CBUFFER_END");
#else
            // TODO: need to test this path with HYBRID_RENDERER_V2 ...

            builder.AppendLine("CBUFFER_START(UnityPerMaterial)");
            int instancedCount = 0;
            foreach (var prop in properties.Where(n => n.generatePropertyBlock && n.hasBatchableProperties))
            {
                if (!prop.gpuInstanced)
                    prop.AppendBatchablePropertyDeclarations(builder);
                else
                    instancedCount++;
            }

            if (instancedCount > 0)
            {
                builder.AppendLine("// Hybrid instanced properties");
                foreach (var prop in properties.Where(n => n.generatePropertyBlock && n.hasBatchableProperties))
                {
                    if (prop.gpuInstanced)
                        prop.AppendBatchablePropertyDeclarations(builder);
                }
            }
            builder.AppendLine("CBUFFER_END");

            if (instancedCount > 0)
            {
                builder.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");

                builder.AppendLine("// DOTS instancing definitions");
                builder.AppendLine("UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)");
                foreach (var prop in properties.Where(n => n.generatePropertyBlock && n.hasBatchableProperties))
                {
                    if (prop.gpuInstanced)
                    {
                        var n = prop.referenceName;
                        string type = prop.concreteShaderValueType.ToShaderString(prop.concretePrecision);
                        builder.AppendLine($"    UNITY_DOTS_INSTANCED_PROP({type}, {n})");
                    }
                }
                builder.AppendLine("UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)");

                builder.AppendLine("// DOTS instancing usage macros");
                foreach (var prop in properties.Where(n => n.generatePropertyBlock && n.hasBatchableProperties))
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
#endif

            // declare non-batchable properties
            foreach (var prop in properties.Where(n => n.hasNonBatchableProperties || !n.generatePropertyBlock))
            {
                if (prop.hasBatchableProperties && !prop.generatePropertyBlock) // batchable properties that don't generate property block can't be instanced, get put here
                    prop.AppendBatchablePropertyDeclarations(builder);

                prop.AppendNonBatchablePropertyDeclarations(builder);
            }
        }

        public IEnumerable<AbstractShaderProperty> DotsInstancingProperties(GenerationMode mode)
        {
            var previewMode = (mode == GenerationMode.Preview);
            return properties.Where(n => (previewMode || (n.generatePropertyBlock && n.hasBatchableProperties)) && n.gpuInstanced);
        }

        public string GetDotsInstancingPropertiesDeclaration(GenerationMode mode)
        {
            // Hybrid V1 needs to declare a special macro to that is injected into
            // builtin instancing variables.
            // Hybrid V2 does not need it.
            #if !ENABLE_HYBRID_RENDERER_V2
            var builder = new ShaderStringBuilder();
            var batchAll = mode == GenerationMode.Preview;

            var dotsInstancingProperties = DotsInstancingProperties(mode);

            if (dotsInstancingProperties.Any())
            {
                builder.AppendLine("#if defined(UNITY_HYBRID_V1_INSTANCING_ENABLED)");
                builder.Append("#define HYBRID_V1_CUSTOM_ADDITIONAL_MATERIAL_VARS\t");

                int count = 0;
                int instancedCount = dotsInstancingProperties.Count();
                foreach (var prop in dotsInstancingProperties)
                {
                    string varName = $"{prop.referenceName}_Array";
                    string sType = prop.concreteShaderValueType.ToShaderString(prop.concretePrecision);
                    builder.Append("UNITY_DEFINE_INSTANCED_PROP({0}, {1})", sType, varName);
                    // Combine the UNITY_DEFINE_INSTANCED_PROP lines with \ so the generated
                    // macro expands into multiple definitions if there are more than one.
                    if (count < instancedCount - 1)
                        builder.Append("\\");
                    builder.AppendLine("");
                    count++;
                }
                foreach (var prop in dotsInstancingProperties)
                {
                    string varName = $"{prop.referenceName}_Array";
                    builder.AppendLine("#define {0} UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, {1})", prop.referenceName, varName);
                }
            }
            builder.AppendLine("#endif");
            return builder.ToString();
            #else
            return "";
            #endif
        }

        public List<TextureInfo> GetConfiguredTexutres()
        {
            var result = new List<TextureInfo>();

            // TODO: this should be interface based instead of looking for hard codeded tyhpes

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

            foreach (var prop in properties.OfType<VirtualTextureShaderProperty>().Where(p => p.referenceName != null))
            {
                prop.AddTextureInfo(result);
            }

            return result;
        }
    }
}

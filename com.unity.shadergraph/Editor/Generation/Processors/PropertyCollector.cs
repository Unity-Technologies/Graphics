using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using TextureDimension = UnityEngine.Rendering.TextureDimension;

namespace UnityEditor.ShaderGraph
{
    class PropertyCollector
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public TextureDimension dimension;
            public bool modifiable;
        }

        bool m_ReadOnly;
        List<HLSLProperty> m_HLSLProperties = null;

        // reference name ==> property index in list
        Dictionary<string, int> m_ReferenceNames = new Dictionary<string, int>();

        // list of properties (kept in a list to maintain deterministic declaration order)
        List<AbstractShaderProperty> m_Properties = new List<AbstractShaderProperty>();

        public int propertyCount => m_Properties.Count;
        public IEnumerable<AbstractShaderProperty> properties => m_Properties;
        public AbstractShaderProperty GetProperty(int index) { return m_Properties[index]; }

        public void Sort()
        {
            if (m_ReadOnly)
            {
                Debug.LogError("Cannot sort the properties when the PropertyCollector is already marked ReadOnly");
                return;
            }

            m_Properties.Sort((a, b) => String.CompareOrdinal(a.referenceName, b.referenceName));

            // reference name indices are now messed up, rebuild them
            m_ReferenceNames.Clear();
            for (int i = 0; i < m_Properties.Count; i++)
                m_ReferenceNames.Add(m_Properties[i].referenceName, i);
        }

        public void SetReadOnly()
        {
            m_ReadOnly = true;
        }

        private static bool EquivalentHLSLProperties(AbstractShaderProperty a, AbstractShaderProperty b)
        {
            bool equivalent = true;
            var bHLSLProps = new List<HLSLProperty>();
            b.ForeachHLSLProperty(bh => bHLSLProps.Add(bh));
            a.ForeachHLSLProperty(ah =>
            {
                var i = bHLSLProps.FindIndex(bh => bh.name == ah.name);
                if (i < 0)
                    equivalent = false;
                else
                {
                    var bh = bHLSLProps[i];
                    if ((ah.name != bh.name) ||
                        (ah.type != bh.type) ||
                        (ah.precision != bh.precision) ||
                        (ah.declaration != bh.declaration) ||
                        ((ah.customDeclaration == null) != (bh.customDeclaration == null)))
                    {
                        equivalent = false;
                    }
                    else if (ah.customDeclaration != null)
                    {
                        var ssba = new ShaderStringBuilder();
                        var ssbb = new ShaderStringBuilder();
                        ah.customDeclaration(ssba);
                        bh.customDeclaration(ssbb);
                        if (ssba.ToCodeBlock() != ssbb.ToCodeBlock())
                            equivalent = false;
                    }
                    bHLSLProps.RemoveAt(i);
                }
            });
            return equivalent && (bHLSLProps.Count == 0);
        }

        public void AddShaderProperty(AbstractShaderProperty prop)
        {
            if (m_ReadOnly)
            {
                Debug.LogError("ERROR attempting to add property to readonly collection");
                return;
            }

            int propIndex = -1;
            if (m_ReferenceNames.TryGetValue(prop.referenceName, out propIndex))
            {
                // existing referenceName
                var existingProp = m_Properties[propIndex];
                if (existingProp != prop)
                {
                    // duplicate reference name, but different property instances
                    if (existingProp.GetType() != prop.GetType())
                    {
                        Debug.LogError("Two properties with the same reference name (" + prop.referenceName + ") using different types");
                    }
                    else
                    {
                        if (!EquivalentHLSLProperties(existingProp, prop))
                            Debug.LogError("Two properties with the same reference name (" + prop.referenceName + ") produce different HLSL properties");
                    }
                }
            }
            else
            {
                // new referenceName, new property
                propIndex = m_Properties.Count;
                m_Properties.Add(prop);
                m_ReferenceNames.Add(prop.referenceName, propIndex);
            }
        }

        private List<HLSLProperty> BuildHLSLPropertyList()
        {
            SetReadOnly();
            if (m_HLSLProperties == null)
            {
                m_HLSLProperties = new List<HLSLProperty>();
                foreach (var p in m_Properties)
                    p.ForeachHLSLProperty(h => m_HLSLProperties.Add(h));
            }
            return m_HLSLProperties;
        }

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision inheritedPrecision)
        {
            foreach (var prop in properties)
            {
                prop.ValidateConcretePrecision(inheritedPrecision);
            }

            // build a list of all HLSL properties
            var hlslProps = BuildHLSLPropertyList();

            if (mode == GenerationMode.Preview)
            {
                builder.AppendLine("CBUFFER_START(UnityPerMaterial)");

                // all non-gpu instanced properties (even non-batchable ones!)
                // this is because for preview we convert all properties to UnityPerMaterial properties
                // as we will be submitting the default preview values via the Material..  :)
                foreach (var h in hlslProps)
                {
                    if ((h.declaration == HLSLDeclaration.UnityPerMaterial) ||
                        (h.declaration == HLSLDeclaration.Global))
                    {
                        h.AppendTo(builder);
                    }
                }

                // gpu-instanced properties
                var gpuInstancedProps = hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance);
                if (gpuInstancedProps.Any())
                {
                    builder.AppendLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                    foreach (var h in gpuInstancedProps)
                    {
                        h.AppendTo(builder, name => name + "_dummy");
                    }
                    builder.AppendLine("#else // V2");
                    foreach (var h in gpuInstancedProps)
                    {
                        h.AppendTo(builder);
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

            // non-GPU-instanced batchable properties go first in the UnityPerMaterial cbuffer
            foreach (var h in hlslProps)
                if (h.declaration == HLSLDeclaration.UnityPerMaterial)
                    h.AppendTo(builder);

            // followed by GPU-instanced batchable properties
            var gpuInstancedProperties = hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance);
            if (gpuInstancedProperties.Any())
            {
                builder.AppendLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                foreach (var hlslProp in gpuInstancedProperties)
                    hlslProp.AppendTo(builder, name => name + "_dummy");
                builder.AppendLine("#else");
                foreach (var hlslProp in gpuInstancedProperties)
                    hlslProp.AppendTo(builder);
                builder.AppendLine("#endif");
            }
            builder.AppendLine("CBUFFER_END");
#else
            // TODO: need to test this path with HYBRID_RENDERER_V2 ...

            builder.AppendLine("CBUFFER_START(UnityPerMaterial)");

            int instancedCount = 0;
            foreach (var h in hlslProps)
            {
                if (h.declaration == HLSLDeclaration.UnityPerMaterial)
                    h.AppendTo(builder);
                else if (h.declaration == HLSLDeclaration.HybridPerInstance)
                    instancedCount++;
            }

            if (instancedCount > 0)
            {
                builder.AppendLine("// Hybrid instanced properties");
                foreach (var h in hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance))
                    h.AppendTo(builder);
            }
            builder.AppendLine("CBUFFER_END");

            if (instancedCount > 0)
            {
                builder.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");

                builder.AppendLine("// DOTS instancing definitions");
                builder.AppendLine("UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)");
                foreach (var h in hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance))
                {
                    var n = h.name;
                    string type = h.GetValueTypeString();
                    builder.AppendLine($"    UNITY_DOTS_INSTANCED_PROP({type}, {n})");
                }
                builder.AppendLine("UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)");

                builder.AppendLine("// DOTS instancing usage macros");
                foreach (var h in hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance))
                {
                    var n = h.name;
                    string type = h.GetValueTypeString();
                    builder.AppendLine($"#define {n} UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO({type}, Metadata_{n})");
                }
                builder.AppendLine("#endif");
            }
#endif

            builder.AppendNewLine();
            builder.AppendLine("// Object and Global properties");
            foreach (var h in hlslProps)
                if (h.declaration == HLSLDeclaration.Global)
                    h.AppendTo(builder);
        }

        public bool HasDotsProperties()
        {
            var hlslProps = BuildHLSLPropertyList();
            bool hasDotsProperties = false;
            foreach (var h in hlslProps)
            {
                if (h.declaration == HLSLDeclaration.HybridPerInstance)
                    hasDotsProperties = true;
            }
            return hasDotsProperties;
        }

        public string GetDotsInstancingPropertiesDeclaration(GenerationMode mode)
        {
            // Hybrid V1 needs to declare a special macro to that is injected into
            // builtin instancing variables.
            // Hybrid V2 does not need it.
#if !ENABLE_HYBRID_RENDERER_V2
            var builder = new ShaderStringBuilder();
            var batchAll = (mode == GenerationMode.Preview);

            // build a list of all HLSL properties
            var hybridHLSLProps = BuildHLSLPropertyList().Where(h => h.declaration == HLSLDeclaration.HybridPerInstance);

            if (hybridHLSLProps.Any())
            {
                builder.AppendLine("#if defined(UNITY_HYBRID_V1_INSTANCING_ENABLED)");
                builder.AppendLine("#define HYBRID_V1_CUSTOM_ADDITIONAL_MATERIAL_VARS \\");

                int count = 0;
                foreach (var prop in hybridHLSLProps)
                {
                    // Combine multiple UNITY_DEFINE_INSTANCED_PROP lines with \ so the generated
                    // macro expands into multiple definitions if there are more than one.
                    if (count > 0)
                    {
                        builder.Append("\\");
                        builder.AppendNewLine();
                    }
                    builder.Append("UNITY_DEFINE_INSTANCED_PROP(");
                    builder.Append(prop.GetValueTypeString());
                    builder.Append(", ");
                    builder.Append(prop.name);
                    builder.Append("_Array)");
                    count++;
                }
                builder.AppendNewLine();

                foreach (var prop in hybridHLSLProps)
                {
                    string varName = $"{prop.name}_Array";
                    builder.AppendLine("#define {0} UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, {1})", prop.name, varName);
                }
            }
            builder.AppendLine("#endif");
            return builder.ToString();
#else
            return "";
#endif
        }

        public List<TextureInfo> GetConfiguredTextures()
        {
            var result = new List<TextureInfo>();

            // TODO: this should be interface based instead of looking for hard coded types

            foreach (var prop in properties.OfType<Texture2DShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        dimension = TextureDimension.Tex2D,
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
                        dimension = TextureDimension.Tex2DArray,
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
                        dimension = TextureDimension.Tex3D,
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
                        dimension = TextureDimension.Cube,
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

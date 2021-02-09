using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

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

        private Target m_Target = null;
        private UInt64 m_DeclarerFlag = (UInt64)DeclarerFlags._NodeOrGraph;

        private bool m_ReadOnly;

        enum DeclarerFlags
        {
            _NodeOrGraph = (1 << 0),    // property declared by a graph (graph property) or a node (or slot on the node)
            _Target = (1 << 1),         // all bits starting with this one represent properties added by targets in the Target list
        }

        private struct Property
        {
            public AbstractShaderProperty property;
            public UInt64 declarerFlags;
        }

        private List<Target> m_Targets = new List<Target>();

        // reference name ==> property index in list
        Dictionary<string, int> m_ReferenceNames = new Dictionary<string, int>();

        // list of properties (kept in a list to maintain deterministic declaration order)
        private List<Property> m_Properties = new List<Property>();

        public int propertyCount => m_Properties.Count;
        public IEnumerable<AbstractShaderProperty> properties => m_Properties.Select(p => p.property);
        public AbstractShaderProperty GetProperty(int index) { return m_Properties[index].property; }

        public void Sort()
        {
            m_Properties.Sort((a, b) => String.CompareOrdinal(a.property.referenceName, b.property.referenceName));
        }

        public void SetReadOnly()
        {
            m_ReadOnly = true;
        }

        public void BeginTargetCollection(Target target)
        {
            // Debug.Log("Begin Collection for Target " + target.displayName);
            if (m_Target != null)
                Debug.LogError("BEGIN TARGET TWICE");

            int targetIndex = m_Targets.FindIndex(t => t == target);
            if (targetIndex < 0)
            {
                targetIndex = m_Targets.Count;
                m_Targets.Add(target);
            }
            m_Target = target;
            m_DeclarerFlag = ((UInt64)DeclarerFlags._Target) << targetIndex;
        }

        public void EndTargetCollection(Target target)
        {
            // Debug.Log("End Collection for Target " + target.displayName);
            if (m_Target != target)
                Debug.LogError("MISMATCHED END != BEGIN");
            m_Target = null;
            m_DeclarerFlag = (UInt64)DeclarerFlags._NodeOrGraph;
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
                if (existingProp.property != prop)
                {
                    // duplicate reference name, but different property instances
                    if (existingProp.property.GetType() != prop.GetType())
                    {
                        Debug.LogError("Two properties with the same reference name using different types");
                    }
                    else
                    {
                        // TODO: verify the property declarations are more or less equivalent.. somehow.. ?
                    }
                }
                // set target flag to record who has declared this property
                existingProp.declarerFlags = existingProp.declarerFlags | m_DeclarerFlag;
                m_Properties[propIndex] = existingProp;
            }
            else
            {
                // new referenceName, new property
                propIndex = m_Properties.Count;
                m_Properties.Add(new Property() { property = prop, declarerFlags = m_DeclarerFlag });
                m_ReferenceNames.Add(prop.referenceName, propIndex);
            }
        }

        public void ForEachPropertyUsedByTarget(Target target, Action<AbstractShaderProperty> action)
        {
            // filter to properties declared by a node, graph, or the current Target
            UInt64 declarerFlagFilter = (UInt64)DeclarerFlags._NodeOrGraph;
            int targetIndex = m_Targets.FindIndex(t => t == target);
            if (targetIndex >= 0)
                declarerFlagFilter = declarerFlagFilter | (((UInt64)DeclarerFlags._Target) << targetIndex);

            foreach (var p in m_Properties)
            {
                if (p.declarerFlags == 0)
                    Debug.LogError("No Declarer! " + p.property.referenceName);
                else if ((p.declarerFlags & declarerFlagFilter) != 0)
                {
                    action(p.property);
                }
            }
        }

        public void GetPropertiesDeclarationForTarget(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision inheritedPrecision, Target target)
        {
            ForEachPropertyUsedByTarget(target, p => p.ValidateConcretePrecision(inheritedPrecision));

            var hlslProps = new List<HLSLProperty>();
            ForEachPropertyUsedByTarget(target, p => p.ForeachHLSLProperty(h => hlslProps.Add(h)));

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

        public string GetDotsInstancingPropertiesDeclarationForTarget(Target target, GenerationMode mode)
        {
            // Hybrid V1 needs to declare a special macro to that is injected into
            // builtin instancing variables.
            // Hybrid V2 does not need it.
#if !ENABLE_HYBRID_RENDERER_V2
            var builder = new ShaderStringBuilder();
            var batchAll = (mode == GenerationMode.Preview);

            // build a list of all HLSL properties
            var hybridHLSLProps = new List<HLSLProperty>();
            ForEachPropertyUsedByTarget(target, p => p.ForeachHLSLProperty(h =>
            {
                if (h.declaration == HLSLDeclaration.HybridPerInstance)
                    hybridHLSLProps.Add(h);
            }));

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

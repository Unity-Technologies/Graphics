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
                    if (!ah.ValueEquals(bh))
                        equivalent = false;
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
                var dict = new Dictionary<string, int>();
                foreach (var p in m_Properties)
                {
                    p.ForeachHLSLProperty(
                        h =>
                        {
                            if (dict.TryGetValue(h.name, out int index))
                            {
                                // check if same property
                                if (!h.ValueEquals(m_HLSLProperties[index]))
                                    Debug.LogError("Two different HLSL Properties declared with the same name: " + h.name + " and " + m_HLSLProperties[index].name);
                                return;
                            }
                            dict.Add(h.name, m_HLSLProperties.Count);
                            m_HLSLProperties.Add(h);
                        }
                    );
                }
            }
            return m_HLSLProperties;
        }

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision defaultPrecision)
        {
            foreach (var prop in properties)
            {
                // set up switched properties to use the inherited precision
                prop.SetupConcretePrecision(defaultPrecision);
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

                // DOTS instanced properties
                var dotsInstancedProperties = hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance);
                if (dotsInstancedProperties.Any())
                {
                    foreach (var h in dotsInstancedProperties)
                    {
                        h.AppendTo(builder);
                    }
                }
                builder.AppendLine("CBUFFER_END");
                builder.AppendLine("#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var");
                return;
            }

            builder.AppendLine("CBUFFER_START(UnityPerMaterial)");
            int instancedCount = 0;
            foreach (var h in hlslProps)
            {
                if (h.declaration == HLSLDeclaration.UnityPerMaterial ||
                    h.declaration == HLSLDeclaration.HybridPerInstance)
                    h.AppendTo(builder);
                if (h.declaration == HLSLDeclaration.HybridPerInstance)
                    instancedCount++;
            }
            builder.AppendLine("CBUFFER_END");

            builder.AppendLine("");
            if (instancedCount > 0)
            {

                builder.AppendLine("#if defined(DOTS_INSTANCING_ON)");
                builder.AppendLine("// DOTS instancing definitions");
                builder.AppendLine("UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)");
                foreach (var h in hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance))
                {
                    var n = h.name;
                    string type = h.GetValueTypeString();
                    builder.AppendLine($"    UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED({type}, {n})");
                }
                builder.AppendLine("UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)");

                builder.AppendLine("// DOTS instancing usage macros");
                builder.AppendLine("#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)");

                builder.AppendLine("#elif defined(UNITY_INSTANCING_ENABLED)");
                builder.AppendLine("// Unity instancing definitions");
                builder.AppendLine("UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)");
                foreach (var h in hlslProps.Where(h => h.declaration == HLSLDeclaration.HybridPerInstance))
                {
                    var n = h.name;
                    string type = h.GetValueTypeString();
                    builder.AppendLine($"    UNITY_DEFINE_INSTANCED_PROP({type}, {n})");
                }
                builder.AppendLine("UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)");

                builder.AppendLine("// Unity instancing usage macros");
                builder.AppendLine("#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)");

                builder.AppendLine("#else");
                builder.AppendLine("#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var");
                builder.AppendLine("#endif");

            }

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

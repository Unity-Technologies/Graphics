using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    struct VariableSource
    {
        public string code;
        public HashSet<AbstractMaterialNode> nodes;
    }

    class VariableRegistry
    {
        Dictionary<string, VariableSource> m_Sources = new Dictionary<string, VariableSource>();
        bool m_Validate = false;
        ShaderStringBuilder m_Builder;

        public VariableRegistry(ShaderStringBuilder builder, bool validate = false)
        {
            m_Builder = builder;
            m_Validate = validate;
        }

        internal ShaderStringBuilder builder => m_Builder;

        public Dictionary<string, VariableSource> sources => m_Sources;

        public List<string> names { get; } = new List<string>();

        public void ProvideVariable(string name, Action<ShaderStringBuilder> generator)
        {
            VariableSource existingSource;
            if (m_Sources.TryGetValue(name, out existingSource))
            {
                existingSource.nodes.Add(builder.currentNode);
                if (m_Validate)
                {
                    var startIndex = builder.length;
                    generator(builder);
                    var length = builder.length - startIndex;
                    var code = builder.ToString(startIndex, length);
                    builder.length -= length;
                    if (code != existingSource.code)
                        Debug.LogErrorFormat(@"Variable `{0}` has varying implementations:{1}{1}{2}{1}{1}{3}", name, Environment.NewLine, code, existingSource);
                }
            }
            else
            {
                builder.AppendNewLine();
                var startIndex = builder.length;
                generator(builder);
                var length = builder.length - startIndex;
                var code = m_Validate ? builder.ToString(startIndex, length) : string.Empty;
                m_Sources.Add(name, new VariableSource { code = code, nodes = new HashSet<AbstractMaterialNode> {builder.currentNode} });
                names.Add(name);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class FunctionRegistry
    {
        Dictionary<string, string> m_Sources = new Dictionary<string, string>();
        bool m_Validate = false;
        ShaderStringBuilder m_Builder;

        public FunctionRegistry(ShaderStringBuilder builder, bool validate = false)
        {
            m_Builder = builder;
            m_Validate = validate;
        }

        internal ShaderStringBuilder builder => m_Builder;

        public Dictionary<string, string> sources => m_Sources;
        
        public List<string> names { get; } = new List<string>();

        public void ProvideFunction(string name, Action<ShaderStringBuilder> generator)
        {
            string existingSource;
            if (m_Sources.TryGetValue(name, out existingSource))
            {
                if (m_Validate)
                {
                    var startIndex = builder.length;
                    generator(builder);
                    var length = builder.length - startIndex;
                    var source = builder.ToString(startIndex, length);
                    builder.length -= length;
                    if (source != existingSource)
                        Debug.LogErrorFormat(@"Function `{0}` has varying implementations:{1}{1}{2}{1}{1}{3}", name, Environment.NewLine, source, existingSource);
                }
            }
            else
            {
                builder.AppendNewLine();
                var startIndex = builder.length;
                generator(builder);
                var length = builder.length - startIndex;
                var source = m_Validate ? builder.ToString(startIndex, length) : string.Empty;
                m_Sources.Add(name, source);
            }

            names.Add(name);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    struct IncludeInfo
    {
        public string code;
        public HashSet<AbstractMaterialNode> nodes;
    }

    class IncludeRegistry
    {
        Dictionary<string, IncludeInfo> m_Includes = new Dictionary<string, IncludeInfo>();
        ShaderStringBuilder m_Builder;

        public IncludeRegistry(ShaderStringBuilder builder)
        {
            m_Builder = builder;
        }

        internal ShaderStringBuilder builder => m_Builder;

        public Dictionary<string, IncludeInfo> includes => m_Includes;

        public IEnumerable<string> names { get { return m_Includes.Keys; } }

        public void ProvideInclude(string fileName)
        {
            ProvideIncludeBlock(fileName, "#include \"" + fileName + "\"");
        }

        public void ProvideIncludeBlock(string uniqueIdentifier, string blockCode)
        {
            IncludeInfo existingInfo;
            if (m_Includes.TryGetValue(uniqueIdentifier, out existingInfo))
            {
                existingInfo.nodes.Add(m_Builder.currentNode);
            }
            else
            {
                m_Builder.AppendLine(blockCode);
                m_Includes.Add(uniqueIdentifier, new IncludeInfo { code = blockCode, nodes = new HashSet<AbstractMaterialNode> { builder.currentNode } });
            }
        }
    }
}

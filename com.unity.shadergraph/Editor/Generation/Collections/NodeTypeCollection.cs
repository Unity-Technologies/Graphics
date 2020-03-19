using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    //[GenerationAPI]
    internal class NodeTypeCollection : IEnumerable<Type>
    {
        readonly HashSet<Type> m_Items;

        public NodeTypeCollection()
        {
            m_Items = new HashSet<Type>();
        }

        public void Add(NodeTypeCollection collection)
        {
            foreach(Type t in collection)
            {
                m_Items.Add(t);
            }
        }

        public void Add(Type t)
        {
            m_Items.Add(t);
        }
        public IEnumerator<Type> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(Type t)
        {
            return m_Items.Contains(t);
        }

        public static NodeTypeCollection operator - (NodeTypeCollection a,
                                                     NodeTypeCollection b)
        {
            NodeTypeCollection output = new NodeTypeCollection()
            {
                a
            };
            foreach(var nodeType in b)
            {
                output.m_Items.Remove(nodeType);
            }
            return output;
        }
    }
}

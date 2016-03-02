using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class NodeInfo : ScriptableObject
    {
        public Dictionary<string, NodeBlockInfo> nodeBlocks;
        public VFXEdContext Context {get { return m_Context; } set { m_Context = value; } }
        [SerializeField]
        private VFXEdContext m_Context;
            
        public static NodeInfo Create(VFXEdContext context)
        {
            NodeInfo n =  ScriptableObject.CreateInstance<NodeInfo>();
            n.Context = context;
            return n;
        }

        public NodeInfo()
        {
            nodeBlocks = new Dictionary<string, NodeBlockInfo>();
        }
    }
}

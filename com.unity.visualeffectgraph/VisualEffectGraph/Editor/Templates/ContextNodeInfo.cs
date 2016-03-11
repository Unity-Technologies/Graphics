using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class ContextNodeInfo : NodeInfo
    {
        public string Context {get { return m_Context; } set { m_Context = value; } }
        public Dictionary<string, NodeBlockInfo> nodeBlocks;

        [SerializeField]
        private string m_Context;

        public static ContextNodeInfo Create(string Context)
        {
            ContextNodeInfo n =  CreateInstance<ContextNodeInfo>();
            n.Context = Context;
            return n;
        }

        public ContextNodeInfo() : base()
        {
            nodeBlocks = new Dictionary<string, NodeBlockInfo>();
        }

    }
}

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
        public int systemIndex;

        [SerializeField]
        private string m_Context;

        public static ContextNodeInfo Create(string uniqueName, string Context, int sysIndex)
        {
            ContextNodeInfo n =  CreateInstance<ContextNodeInfo>();
            n.Context = Context;
            n.m_UniqueName = uniqueName;
            n.systemIndex = sysIndex;
            return n;
        }

        public ContextNodeInfo() : base()
        {
            nodeBlocks = new Dictionary<string, NodeBlockInfo>();
        }

    }
}

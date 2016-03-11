using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class DataNodeInfo : NodeInfo
    {
        public bool Exposed;
        public Dictionary<string, DataNodeBlockInfo> nodeBlocks;

        public static DataNodeInfo Create(string uniqueName, bool bExposed)
        {
            DataNodeInfo n =  CreateInstance<DataNodeInfo>();
            n.Exposed = bExposed;
            n.m_UniqueName = uniqueName;
            return n;
        }

        public DataNodeInfo() : base()
        {
           nodeBlocks = new Dictionary<string, DataNodeBlockInfo>();
        }
    }
}

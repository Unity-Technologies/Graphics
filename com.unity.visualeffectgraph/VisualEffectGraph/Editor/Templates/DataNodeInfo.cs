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

        public static DataNodeInfo Create(bool bExposed)
        {
            DataNodeInfo n =  CreateInstance<DataNodeInfo>();
            n.Exposed = bExposed;
            return n;
        }

        public DataNodeInfo() : base()
        {
           nodeBlocks = new Dictionary<string, DataNodeBlockInfo>();
        }
    }
}

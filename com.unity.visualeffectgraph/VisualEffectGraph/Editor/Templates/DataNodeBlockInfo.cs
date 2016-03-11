using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class DataNodeBlockInfo : NodeBlockInfo
    {
        public string ExposedName;

        public static DataNodeBlockInfo Create(string blockname, string exposedname)
        {
            DataNodeBlockInfo ni = CreateInstance<DataNodeBlockInfo>();
            ni.BlockName = blockname;
            ni.ExposedName = exposedname;
            return ni;
        }

        public DataNodeBlockInfo() : base()
        {

        }
    }
}

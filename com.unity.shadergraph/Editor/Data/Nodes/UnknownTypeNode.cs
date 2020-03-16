using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using static UnityEditor.Graphing.SerializationHelper;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class UnknownTypeNode : AbstractMaterialNode
    {
        public JSONSerializedElement serializedData;
        public UnknownTypeNode(JSONSerializedElement serializedNodeData) : base()
        {
            serializedData = serializedNodeData;
        }
    }
}


using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class GraphData0
    {
        public List<SerializationHelper.JSONSerializedElement> m_SerializableNodes;

        public List<SerializationHelper.JSONSerializedElement> m_SerializableEdges;

        public string m_ActiveOutputNodeGuidSerialized;
    }
}

using UnityEditor.ContextLayeredDataStorage;
using UnityEngine;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed partial class GraphStorage : CLDS, ISerializationCallbackReceiver
    {
        public class NodeReader : DataReader
        {
            public NodeReader(Element element) : base(element)
            {
            }
        }

        public class PortReader : DataReader
        {
            public PortReader(Element element) : base(element)
            {
            }
        }

        public class FieldReader : DataReader
        {
            public FieldReader(Element element) : base(element)
            {
            }
        }

    }
}

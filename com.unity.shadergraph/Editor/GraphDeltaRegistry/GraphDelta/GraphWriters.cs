using UnityEditor.ContextLayeredDataStorage;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed partial class GraphStorage : CLDS
    {
        public class NodeWriter : DataWriter
        {
            public NodeWriter(Element element) : base(element)
            {
            }
        }

        public class PortWriter : DataWriter
        {
            public PortWriter(Element element) : base(element)
            {
            }
        }

        public class FieldWriter : DataWriter
        {
            public FieldWriter(Element element) : base(element)
            {
            }
        }
    }
}

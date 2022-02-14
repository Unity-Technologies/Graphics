using UnityEditor.ContextLayeredDataStorage;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed partial class GraphStorage : CLDS
    {
        public class NodeHeader : DataHeader
        {
            public override DataReader GetReader(Element element)
            {
                return new NodeReader(element);
            }

            public override DataWriter GetWriter(Element element)
            {
                return new NodeWriter(element);
            }
        }

        public class PortHeader : DataHeader
        {
            public PortHeader() : base()
            {
                SetMetadata("_isInput", true);
                SetMetadata("_isHorizontal", true);
            }

            public PortHeader(bool isInput, bool isHorizontal) : this()
            {
                SetMetadata("_isInput", isInput);
                SetMetadata("_isHorizontal", isHorizontal);
            }

            public override DataReader GetReader(Element element)
            {
                return new PortReader(element);
            }

            public override DataWriter GetWriter(Element element)
            {
                return new PortWriter(element);
            }

        }

        public class FieldHeader : DataHeader
        {
            public override DataReader GetReader(Element element)
            {
                return new FieldReader(element);
            }

            public override DataWriter GetWriter(Element element)
            {
                return new FieldWriter(element);
            }

        }
    }
}

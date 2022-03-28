using UnityEditor.ContextLayeredDataStorage;
using UnityEngine;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed partial class GraphStorage : CLDS, ISerializationCallbackReceiver
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
            public const string kInput = "_isInput";
            public const string kHorizontal = "_isHorizontal";
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

        public class FieldHeader<T> : FieldHeader
        {
        }

    }
}

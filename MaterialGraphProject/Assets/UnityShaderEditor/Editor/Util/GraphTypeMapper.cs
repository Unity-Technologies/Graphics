using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;

namespace UnityEditor.Graphing.Util
{
    public class GraphTypeMapper : BaseTypeFactory<INode, GraphElement>
    {
        public GraphTypeMapper(Type fallbackType) : base(fallbackType)
        {
        }

        protected override GraphElement InternalCreate(Type valueType)
        {
            return (GraphElement)Activator.CreateInstance(valueType);
        }
    }
}

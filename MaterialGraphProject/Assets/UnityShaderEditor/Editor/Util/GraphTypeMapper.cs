using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.Graphing.Util
{
#if WITH_PRESENTER
    public class GraphTypeMapper : BaseTypeFactory<INode, ScriptableObject>
    {
        public GraphTypeMapper(Type fallbackType) : base(fallbackType)
        {
        }

        protected override ScriptableObject InternalCreate(Type valueType)
        {
            return ScriptableObject.CreateInstance(valueType);
        }
    }
#else
    public class GraphTypeMapper : BaseTypeFactory<INode, GraphElement>
    {
        public GraphTypeMapper(Type fallbackType) : base(fallbackType)
        {
        }

        protected override GraphElement InternalCreate(Type valueType)
        {
            return (GraphElement) Activator.CreateInstance(valueType);
        }
    }
#endif
}

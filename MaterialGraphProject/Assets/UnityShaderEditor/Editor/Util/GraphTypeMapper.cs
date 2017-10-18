using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Util
{
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
}

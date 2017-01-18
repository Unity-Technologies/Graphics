using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Util
{
    public class TypeMapper : BaseTypeMapper<INode, ScriptableObject>
    {
        public TypeMapper(Type fallbackType) : base(fallbackType)
        {
        }

        protected override ScriptableObject InternalCreate(Type valueType)
        {
            return ScriptableObject.CreateInstance(valueType);
        }
    }
}

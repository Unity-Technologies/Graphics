using System;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class SerializedGraphDataSource : AbstractGraphDataSource
    {
        protected SerializedGraphDataSource()
        {}

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(SerializableNode), typeof(NodeDrawData));
        }
    }
}

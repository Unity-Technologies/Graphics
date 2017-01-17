using System;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class SerializedGraphPresenter : AbstractGraphPresenter
    {
        protected SerializedGraphPresenter()
        {}

        protected override void AddTypeMappings(Action<Type, Type> map)
        {
            map(typeof(SerializableNode), typeof(GraphNodePresenter));
        }
    }
}

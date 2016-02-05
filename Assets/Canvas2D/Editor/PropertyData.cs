using System.Reflection;
using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class PropertyData
    {
        public PropertyData(FieldInfo f, object o1, object o2)
        {
            field = f;
            data0 = o1;
            data1 = o2;
            time = 0.0f;
            curve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f); //@todo replace this by google material curve
        }

        public FieldInfo field;
        public object data0;
        public object data1;
        public float time;
        public AnimationCurve curve;
    }
}

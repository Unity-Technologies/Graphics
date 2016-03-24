using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class CurveVectorContainer : ScriptableObject
    {
        public AnimationCurve CurveX;
        public AnimationCurve CurveY;
        public AnimationCurve CurveZ;
    }
}

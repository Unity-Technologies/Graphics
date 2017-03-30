using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorDot : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN right = Vector3.zero;
            public FloatN left = Vector3.zero;
        }

        override public string name { get { return "Dot"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[1]) };
        }
    }
}


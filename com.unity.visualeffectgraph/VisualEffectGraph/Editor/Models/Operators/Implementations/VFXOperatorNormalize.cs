using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorNormalize : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN input = Vector3.one;
        }

        override public string name { get { return "Normalize"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Normalize(inputExpression[0]) };
        }
    }
}

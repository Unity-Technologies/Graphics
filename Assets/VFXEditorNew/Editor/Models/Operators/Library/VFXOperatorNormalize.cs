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
            var invLength = new VFXExpressionDivide(VFXOperatorUtility.OneExpression[1], VFXOperatorUtility.Length(inputExpression[0]));
            var invLengthVector = VFXOperatorUtility.CastFloat(invLength, inputExpression[0].ValueType);
            return new[] { new VFXExpressionMul(inputExpression[0], invLengthVector) };
        }
    }
}



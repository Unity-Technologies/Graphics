using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class Normalize : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The vector to be normalized.")]
            public FloatN x = Vector3.one;
        }

        override public string name { get { return "Normalize"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Normalize(inputExpression[0]) };
        }
    }
}

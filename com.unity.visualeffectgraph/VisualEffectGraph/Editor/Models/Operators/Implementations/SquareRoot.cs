using System;
using UnityEngine;


namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class SquareRoot : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Square Root"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Sqrt(inputExpression[0]) };
        }
    }
}

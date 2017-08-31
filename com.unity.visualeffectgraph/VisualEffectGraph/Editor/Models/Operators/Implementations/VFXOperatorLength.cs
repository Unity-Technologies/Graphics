using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Vector")]
    class VFXOperatorLength : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public FloatN x = Vector3.one;
        }

        override public string name { get { return "Length"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }
}

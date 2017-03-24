using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorLength : VFXOperatorFloatUnified
    {
        public class Properties
        {
            public FloatN input = Vector3.one;
        }

        override public string name { get { return "Length"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }
}


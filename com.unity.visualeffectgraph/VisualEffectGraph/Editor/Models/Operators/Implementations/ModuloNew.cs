using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math", experimental = true)]
    class ModuloNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The numerator operand.")]
            public float a = 1.0f;
            [Tooltip("The denominator operand.")]
            public float b = 1.0f;
        }

        override public string name { get { return "ModuloNew"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Modulo(inputExpression[0], inputExpression[1]) };
        }
    }
}

using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Utility")]
    class VFXOperatorSampleCurve : VFXOperator
    {
        override public string name { get { return "Sample Curve"; } }

        public class InputProperties
        {
            [Tooltip("The curve to sample from.")]
            public AnimationCurve curve = new AnimationCurve();
            [Tooltip("The time along the curve to take a sample from.")]
            public float time = 0.0f;
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (inputExpression.Length != 2)
            {
                return new VFXExpression[] {};
            }

            return new[] { new VFXExpressionSampleCurve(inputExpression[0], inputExpression[1]) };
        }
    }
}

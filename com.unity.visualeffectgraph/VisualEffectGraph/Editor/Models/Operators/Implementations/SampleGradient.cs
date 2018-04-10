using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Utility")]
    class SampleGradient : VFXOperator
    {
        override public string name { get { return "Sample Gradient"; } }

        public class InputProperties
        {
            [Tooltip("The gradient to sample from.")]
            public Gradient gradient = new Gradient();
            [Range(0.0f, 1.0f), Tooltip("The time along the gradient to take a sample from.")]
            public float time = 0.0f;
        }

        public class OutputProperties
        {
            public Vector4 s;
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleGradient(inputExpression[0], inputExpression[1]) };
        }
    }
}

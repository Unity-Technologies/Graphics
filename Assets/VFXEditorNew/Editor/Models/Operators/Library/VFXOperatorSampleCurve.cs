using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorSampleCurve : VFXOperator
    {
        override public string name { get { return "SampleCurve"; } }

        public class InputProperties
        {
            public float time = 0.0f;
            public AnimationCurve curve = new AnimationCurve();
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

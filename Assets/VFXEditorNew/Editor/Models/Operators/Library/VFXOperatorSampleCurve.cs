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

        protected override ModeFlags Flags { get { return ModeFlags.None; } }

        public class Properties
        {
            public float time = 0.0f;
            public AnimationCurve curve = new AnimationCurve();
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleCurve(inputExpression[0], inputExpression[1]) };
        }
    }
}
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorClamp : VFXOperator
    {
        public class Properties
        {
            public float input = 0.0f;
            public float min = 0.0f;
            public float max = 1.0f;
        }

        override public string name { get { return "Clamp"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }

    [VFXInfo]
    class VFXOperatorLerp : VFXOperator
    {
        public class Properties
        {
            public float x = 0.0f;
            public float y = 1.0f;
            public float s = 0.0f;
        }

        override public string name { get { return "Lerp"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
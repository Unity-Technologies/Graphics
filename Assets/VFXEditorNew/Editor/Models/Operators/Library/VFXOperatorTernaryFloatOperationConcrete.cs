using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorClamp : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN input = new FloatN(new[] { 0.0f });
            public FloatN min = new FloatN(new[] { 0.0f });
            public FloatN max = new FloatN(new[] { 1.0f });
        }

        override public string name { get { return "Clamp"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (inputExpression.Length != 3)
            {
                return new VFXExpression[] { };
            }

            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }

    [VFXInfo]
    class VFXOperatorLerp : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN x = new FloatN(new[] { 0.0f });
            public FloatN y = new FloatN(new[] { 1.0f });
            public FloatN s = new FloatN(new[] { 0.5f });
        }

        override public string name { get { return "Lerp"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (inputExpression.Length != 3)
            {
                return new VFXExpression[] { };
            }

            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
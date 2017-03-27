using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorClamp : VFXOperatorFloatUnified
    {
        public class Properties
        {
            public FloatN input = new FloatN(new[] { 0.0f });
            public FloatN min = new FloatN(new[] { 0.0f });
            public FloatN max = new FloatN(new[] { 1.0f });
        }

        override public string name { get { return "Clamp"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}


using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorLerp : VFXOperatorFloatUnified
    {
        public class Properties
        {
            public FloatN x = new FloatN(new[] { 0.0f });
            public FloatN y = new FloatN(new[] { 1.0f });
            public FloatN s = new FloatN(new[] { 0.5f });
        }

        override public string name { get { return "Lerp"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}


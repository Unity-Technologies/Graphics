using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFmod : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN right = new FloatN(new[] { 1.0f });
            public FloatN left = new FloatN(new[] { 1.0f });
        }

        override public string name { get { return "Fmod"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
			return new[] { VFXOperatorUtility.Fmod(inputExpression[0], inputExpression[1]) };
        }
    }
}

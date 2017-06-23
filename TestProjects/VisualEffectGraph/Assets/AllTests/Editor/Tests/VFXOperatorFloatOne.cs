using System;

namespace UnityEditor.VFX.Test
{
    class VFXOperatorFloatOne : VFXOperator
    {
        // HACK FIX
        private static VFXExpression defaultValue = null;
        private VFXExpression GetDefault
        {
            get
            {
                if (defaultValue == null)
                    defaultValue = new VFXValue<float>(1.0f, VFXValue.Mode.Constant);
                return defaultValue;
            }
        }

        override public string name { get { return "Temp_Float_One"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { GetDefault };
        }
    }
}

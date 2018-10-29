#if !UNITY_EDITOR_OSX
using System;

namespace UnityEditor.VFX.Test
{
    class VFXOperatorFloatOne : VFXOperator
    {
        public class OutputProperties
        {
            public float o = 0;
        }

        // HACK FIX
        private static VFXExpression defaultValue = null;
        private VFXExpression GetDefault
        {
            get
            {
                if (defaultValue == null)
                    defaultValue = VFXValue.Constant(1.0f);
                return defaultValue;
            }
        }

        override public string name { get { return "Temp_Float_One"; } }
        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { GetDefault };
        }
    }
}
#endif

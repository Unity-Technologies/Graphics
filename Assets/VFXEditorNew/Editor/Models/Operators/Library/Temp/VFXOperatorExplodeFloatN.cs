using System;
using System.Linq;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorExplodeFloatN : VFXOperator
    {
        public class InputProperties
        {
            public FloatN x = 0.0f;
        }

        override public string name { get { return "Temp_Explode"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();
        }
    }
}

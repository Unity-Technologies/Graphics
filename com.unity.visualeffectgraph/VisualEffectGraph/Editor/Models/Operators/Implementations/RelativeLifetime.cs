using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Attribute")]
    class RelativeLifetime : VFXOperator
    {
        public class OutputProperties
        {
            public float t;
        }

        public override string name
        {
            get
            {
                return "Current Relative Age (Age/Lifetime)";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] { new VFXAttributeExpression(VFXAttribute.Age) / new VFXAttributeExpression(VFXAttribute.Lifetime) };
            return output;
        }
    }
}

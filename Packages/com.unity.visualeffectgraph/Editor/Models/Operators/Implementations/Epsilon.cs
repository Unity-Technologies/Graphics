using System.Collections.Generic;

using UnityEngine.VFX;


namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Epsilon")]
    [VFXInfo(category = "Math/Constants")]
    class Epsilon : VFXOperator
    {
        override public string name { get { return "Epsilon (ε)"; } }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "ε"));
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.EpsilonExpression[VFXValueType.Float] };
        }
    }
}

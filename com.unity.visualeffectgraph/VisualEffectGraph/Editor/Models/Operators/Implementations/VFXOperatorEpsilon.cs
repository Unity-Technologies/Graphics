using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.VFX
{
    [VFXInfo(category = "Constants")]
    class VFXOperatorEpsilon : VFXOperator
    {
        override public string name { get { return "Epsilon (ε)"; } }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "ε"));
            }
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValue.Constant(Mathf.Epsilon) };
        }
    }
}

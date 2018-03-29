using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Constants")]
    class Pi : VFXOperator
    {
        override public string name { get { return "Pi (π)"; } }

        public class OutputProperties
        {
            public float Pi;
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "π"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "2π"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "π/2"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "π/3"));
            }
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] {
                VFXValue.Constant(Mathf.PI),
                VFXValue.Constant(2 * Mathf.PI),
                VFXValue.Constant(Mathf.PI / 2.0f),
                VFXValue.Constant(Mathf.PI / 3.0f)
            };
        }
    }
}

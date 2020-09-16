using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TorusVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the torus used for the volume calculation.")]
            public Torus torus = new Torus();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the torus.")]
            public float volume;
        }

        override public string name { get { return "Volume (Torus)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var minorRadius = inputExpression[2];
            var majorRadius = inputExpression[3];
            return new VFXExpression[] { VFXOperatorUtility.TorusVolume(minorRadius, majorRadius) };
        }
    }
}

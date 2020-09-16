using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class ConeVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the cone used for the volume calculation.")]
            public Cone cone = new Cone();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the cone.")]
            public float volume;
        }

        override public string name { get { return "Volume (Cone)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var radius0 = inputExpression[2];
            var radius1 = inputExpression[3];
            var height = inputExpression[4];
            return new VFXExpression[] { VFXOperatorUtility.ConeVolume(radius0, radius1, height) };
        }
    }
}

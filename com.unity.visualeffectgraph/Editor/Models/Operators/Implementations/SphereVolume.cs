using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class SphereVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the sphere used for the volume calculation.")]
            public TSphere sphere = TSphere.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the sphere.")]
            public float volume;
        }

        override public string name { get { return "Volume (Sphere)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var scale = new VFXExpressionExtractScaleFromMatrix(inputExpression[0]);
            var radius = inputExpression[1];
            return new VFXExpression[] { VFXOperatorUtility.SphereVolume(radius, scale) };
        }
    }
}

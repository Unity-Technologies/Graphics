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
            public Sphere sphere = new Sphere();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the sphere.")]
            public float volume;
        }

        override public string name { get { return "Volume (Sphere)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var sphereRadius = inputExpression[2];
            return new VFXExpression[] { VFXOperatorUtility.SphereVolume(sphereRadius) };
        }
    }
}

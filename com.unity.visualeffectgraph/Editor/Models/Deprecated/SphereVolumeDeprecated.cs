using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //TODOPAUL : Sanitize this
    class SphereVolumeDeprecated : VFXOperator
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

        override public string name { get { return "Volume (Sphere) (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.SphereVolume(inputExpression[1]) };
        }
    }
}

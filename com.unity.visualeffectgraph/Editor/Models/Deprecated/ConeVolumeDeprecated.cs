using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //TODOPAUL : Sanitize this
    class ConeVolumeDeprecated : VFXOperator
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

        override public string name { get { return "Volume (Cone) (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.ConeVolume(inputExpression[1], inputExpression[2], inputExpression[3]) };
        }
    }
}

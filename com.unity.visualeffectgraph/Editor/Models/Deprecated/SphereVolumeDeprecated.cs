using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
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

        public override void Sanitize(int version)
        {
            var newVolume = ScriptableObject.CreateInstance<Operator.SphereVolume>();
            SanitizeHelper.MigrateTSphereFromSphere(newVolume.inputSlots[0], inputSlots[0]);
            VFXSlot.CopyLinksAndValue(newVolume.outputSlots[0], outputSlots[0], true);
            ReplaceModel(newVolume, this);
        }
    }
}

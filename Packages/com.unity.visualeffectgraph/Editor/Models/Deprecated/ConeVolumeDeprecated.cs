using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
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

        public override void Sanitize(int version)
        {
            var newVolume = ScriptableObject.CreateInstance<Operator.ConeVolume>();
            SanitizeHelper.MigrateTConeFromCone(newVolume.inputSlots[0], inputSlots[0]);
            VFXSlot.CopyLinksAndValue(newVolume.outputSlots[0], outputSlots[0], true);
            ReplaceModel(newVolume, this);
        }
    }
}

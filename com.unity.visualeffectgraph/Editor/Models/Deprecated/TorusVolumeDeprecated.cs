using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class TorusVolumeDeprecated : VFXOperator
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

        override public string name { get { return "Volume (Torus) (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.TorusVolume(inputExpression[1], inputExpression[2]) };
        }

        public override void Sanitize(int version)
        {
            var torusVolume = ScriptableObject.CreateInstance<Operator.TorusVolume>();
            SanitizeHelper.MigrateTTorusFromTorus(torusVolume.inputSlots[0], inputSlots[0]);
            VFXSlot.CopyLinksAndValue(torusVolume.outputSlots[0], outputSlots[0], true);
            ReplaceModel(torusVolume, this);
        }
    }
}

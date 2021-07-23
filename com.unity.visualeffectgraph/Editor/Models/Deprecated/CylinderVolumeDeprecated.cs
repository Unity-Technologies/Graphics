using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class CylinderVolumeDeprecated : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the cylinder used for the volume calculation.")]
            public Cylinder cylinder = new Cylinder();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the cylinder.")]
            public float volume;
        }

        override public string name { get { return "Volume (Cylinder) (deprecated)"; } }

        static public VFXExpression CylinderVolume(VFXExpression radius, VFXExpression height)
        {
            //pi * r * r * h
            var pi = VFXValue.Constant(Mathf.PI);
            return (pi * radius * radius * height);
        }

        public override void Sanitize(int version)
        {
            var newVolume = ScriptableObject.CreateInstance<Operator.ConeVolume>();
            SanitizeHelper.MigrateTConeFromCylinder(newVolume.inputSlots[0], inputSlots[0]);
            VFXSlot.CopyLinksAndValue(newVolume.outputSlots[0], outputSlots[0], true);
            ReplaceModel(newVolume, this);
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { CylinderVolume(inputExpression[1], inputExpression[2]) };
        }
    }
}

using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    //[VFXInfo(category = "Math")] DEPRECATED
    class VFXOperatorFitClamped : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.5f);
            [Tooltip("The start of the old range.")]
            public FloatN oldRangeMin = new FloatN(0.0f);
            [Tooltip("The end of the old range.")]
            public FloatN oldRangeMax = new FloatN(1.0f);
            [Tooltip("The start of the new range.")]
            public FloatN newRangeMin = new FloatN(5.0f);
            [Tooltip("The end of the new range.")]
            public FloatN newRangeMax = new FloatN(10.0f);
        }

        override public string name { get { return "DEPRECATED - Fit (Clamped) "; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression clamped = VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]);
            return new[] { VFXOperatorUtility.Fit(clamped, inputExpression[1], inputExpression[2], inputExpression[3], inputExpression[4]) };
        }

        public override void Sanitize()
        {
            Debug.Log("Sanitizing Graph: Automatically replace Phase Attribute Parameter with a Fixed Random Operator");

            var remap = CreateInstance<VFXOperatorRemap>();
            remap.SetSettingValue("Clamp", true);

            // transfer position
            remap.position = position;

            // Transfer links
            VFXSlot.TransferLinks(remap.GetInputSlot(0), GetInputSlot(0), true);
            VFXSlot.TransferLinks(remap.GetInputSlot(1), GetInputSlot(1), true);
            VFXSlot.TransferLinks(remap.GetInputSlot(2), GetInputSlot(2), true);
            VFXSlot.TransferLinks(remap.GetInputSlot(3), GetInputSlot(3), true);
            VFXSlot.TransferLinks(remap.GetInputSlot(4), GetInputSlot(4), true);

            VFXSlot.TransferLinks(remap.GetOutputSlot(0), GetOutputSlot(0), true);

            // Replace operator
            var parent = GetParent();
            Detach();
            remap.Attach(parent);

            base.Sanitize();
        }
    }
}

using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //[VFXInfo(category = "Math")] DEPRECATED
    class FitClamped : VFXOperatorFloatUnifiedWithVariadicOutput
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
            Debug.Log("Sanitizing Graph: Automatically replace FitClamped with Remap");

            var remap = CreateInstance<RemapDeprecated>();
            remap.SetSettingValue("Clamp", true);

            // Transfer links
            for (int i = 0; i < 5; ++i)
                VFXSlot.CopyLinksAndValue(remap.GetInputSlot(i), GetInputSlot(i), true);
            VFXSlot.CopyLinksAndValue(remap.GetOutputSlot(0), GetOutputSlot(0), true);

            ReplaceModel(remap, this);
            remap.Sanitize();
        }
    }
}

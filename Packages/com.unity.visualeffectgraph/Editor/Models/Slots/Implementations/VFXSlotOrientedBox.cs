using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(OrientedBox))]
    class VFXSlotOrientedBox : VFXSlotTransform
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(AABox);
        }

        sealed protected override void ConvertExpressionsFromLink(VFXSlot fromSlot)
        {
            if (fromSlot.property.type == typeof(AABox))
            {
                var slots = fromSlot.GetVFXValueTypeSlots();

                var centerExp = slots.First(s => s.name == nameof(AABox.center)).GetExpression();
                var sizeExp = slots.First(s => s.name == nameof(AABox.size)).GetExpression();

                var trs = new VFXExpressionTRSToMatrix(
                    centerExp,
                    VFXValue.Constant(Vector3.zero),
                    sizeExp);

                UpdateLinkedInExpression(this, trs, null);
            }
            else
            {
                throw new InvalidOperationException("Unexpected manual conversion from slot type : " + fromSlot.property.type);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Sphere))]
    class VFXSlotSphere : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(TSphere);
        }

        sealed protected override void UpdateLinkSlotConversion(VFXSlot fromSlot)
        {
            if (fromSlot.property.type == typeof(TSphere))
            {
                var destSlots = GetVFXValueTypeSlots();
                var fromSlots = fromSlot.GetVFXValueTypeSlots().ToArray();

                var sourceRadius = fromSlots.FirstOrDefault(o => o.name == nameof(TSphere.radius)).GetExpression();
                var sourceScale = fromSlots.FirstOrDefault(o => o.name == nameof(TSphere.transform.scale)).GetExpression();
                var computedRadius = sourceRadius * VFXOperatorUtility.Max3(sourceScale);
                UpdateLinkedInExpression(destSlots.FirstOrDefault(o => o.name == nameof(Sphere.radius)), computedRadius, null /* corresponding slot is virtual */);

                var sourcePosition = fromSlots.FirstOrDefault(o => o.name == nameof(TSphere.transform.position));
                UpdateLinkedInExpression(destSlots.FirstOrDefault(o => o.name == nameof(Sphere.center)), sourcePosition.GetExpression(), sourcePosition);
            }
        }
    }
}

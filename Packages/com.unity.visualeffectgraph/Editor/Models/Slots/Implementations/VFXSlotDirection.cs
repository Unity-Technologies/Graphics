using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
//using Direction = UnityEditor.VFX.Direction;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(DirectionType))]
    class VFXSlotDirection : VFXSlotEncapsulated
    {
        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.forward, mode);
        }

        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(Vector4)
                || type == typeof(Vector3)
                || type == typeof(Vector);
            //Doesn't expose cast from float/uint (scalar) due to the automatic normalization
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (sourceSlot != null)
            {
                if (sourceSlot.GetType() == typeof(VFXSlotDirection))
                    return expression; //avoid multiple normalization
                if (sourceSlot.property.attributes.Is(VFXPropertyAttributes.Type.Normalized))
                    return expression; //avoid multiple normalization from Normalize attribute (rarely used for output slot)
            }

            if (expression.valueType == VFXValueType.Float4)
                expression = VFXOperatorUtility.CastFloat(expression, VFXValueType.Float3);

            return ApplyPatchExpression(expression);
        }

        protected override VFXExpression ApplyPatchExpression(VFXExpression expression)
        {
            return VFXOperatorUtility.Normalize(expression);
        }
    }
}

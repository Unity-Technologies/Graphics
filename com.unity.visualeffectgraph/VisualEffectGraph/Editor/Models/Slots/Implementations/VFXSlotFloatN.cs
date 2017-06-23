using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        protected override bool CanConvertFrom(VFXExpression expression)
        {
            return expression == null || VFXExpression.IsFloatValueType(expression.ValueType);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return type == typeof(float)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4);
        }

        protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            /* if (expression == null)
             {
                 PropagateToChildren(c => c.UnlinkAll());
                 RemoveAllChildren();
             }
             else
             {
                 var nbComponents = VFXExpression.TypeToSize(expression.ValueType);
                 var nbChildren = GetNbChildren();

                 if (nbChildren > nbComponents)
                 {
                     for (int i = nbComponents; i < nbChildren; ++i)
                     {
                         var child = GetChild(GetNbChildren() - 1);
                         child.UnlinkAll();
                         GetChild(GetNbChildren() - 1).Detach();
                     }
                 }

                 if (GetNbChildren() != nbComponents)
                 {

                 }
             }*/

            return expression;
        }

        protected override VFXValue DefaultExpression()
        {
            if (value == null)
                return null;

            var floatN = (FloatN)value;
            return floatN.ToVFXValue(VFXValue.Mode.FoldableVariable);
        }
    }
}

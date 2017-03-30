using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        protected override bool CanConvertFrom(VFXExpression expression)
        {
            return expression == null || VFXExpression.IsFloatValueType(expression.ValueType);
        }

        protected virtual VFXExpression ConvertExpression(VFXExpression expression)
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
            if (m_Value == null)
                return null;

            var floatN = (FloatN)m_Value;
            return (VFXValue)floatN;
        }
    }
}


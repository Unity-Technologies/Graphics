using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using System.Reflection;

namespace UnityEditor.VFX
{
    // Attribute used to normalize a FloatN
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class NormalizeAttribute : PropertyAttribute
    {
    }

    [Serializable]
    class VFXPropertyAttribute
    {
        public static VFXPropertyAttribute[] Create(object[] attributes)
        {
            List<VFXPropertyAttribute> results = new List<VFXPropertyAttribute>();

            foreach (object attribute in attributes)
            {
                RangeAttribute rangeAttribute = attribute as RangeAttribute;
                if (rangeAttribute != null)
                    results.Add(new VFXPropertyAttribute(Type.kRange, rangeAttribute.min, rangeAttribute.max));

                MinAttribute minAttribute = attribute as MinAttribute;
                if (minAttribute != null)
                    results.Add(new VFXPropertyAttribute(Type.kMin, rangeAttribute.min));

                NormalizeAttribute normallizeAttribute = attribute as NormalizeAttribute;
                if (normallizeAttribute != null)
                    results.Add(new VFXPropertyAttribute(Type.kNormalize));

                TooltipAttribute tooltipAttribute = attribute as TooltipAttribute;
                if (tooltipAttribute != null)
                    results.Add(new VFXPropertyAttribute(tooltipAttribute.tooltip));
            }

            return results.ToArray();
        }

        public static string FindTooltip(VFXProperty property)
        {
            if (property.attributes != null)
            {
                foreach (VFXPropertyAttribute attribute in property.attributes)
                {
                    if (attribute.m_Type == Type.kTooltip)
                        return attribute.m_Tooltip;
                }
            }

            return null;
        }

        public VFXExpression Apply(VFXExpression exp)
        {
            switch (m_Type)
            {
                case Type.kRange:
                    return VFXOperatorUtility.UnifyOp(VFXOperatorUtility.Clamp, exp, VFXValue.Constant(m_Min), VFXValue.Constant(m_Max));
                case Type.kMin:
                    return new VFXExpressionMax(exp, VFXOperatorUtility.CastFloat(VFXValue.Constant(m_Min), exp.ValueType));
                case Type.kNormalize:
                    return VFXOperatorUtility.Normalize(exp);
                case Type.kTooltip:
                    return exp;
                default:
                    throw new NotImplementedException();
            }
        }

        private enum Type
        {
            kRange,
            kMin,
            kNormalize,
            kTooltip
        }

        private VFXPropertyAttribute(Type type, float min = -Mathf.Infinity, float max = Mathf.Infinity)
        {
            m_Type = type;
            m_Min = min;
            m_Max = max;
            m_Tooltip = null;
        }

        private VFXPropertyAttribute(string tooltip)
        {
            m_Type = Type.kTooltip;
            m_Min = -Mathf.Infinity;
            m_Max = Mathf.Infinity;
            m_Tooltip = tooltip;
        }

        [SerializeField]
        private Type m_Type;
        [SerializeField]
        private float m_Min;
        [SerializeField]
        private float m_Max;
        [SerializeField]
        private string m_Tooltip;
    }
}

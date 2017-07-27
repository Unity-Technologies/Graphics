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

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class StringProviderAttribute : PropertyAttribute
    {
        public StringProviderAttribute(Type providerType)
        {
            m_ProviderType = providerType;
        }

        public Type m_ProviderType;
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
                    results.Add(new VFXPropertyAttribute(Type.kMin, minAttribute.min));

                NormalizeAttribute normallizeAttribute = attribute as NormalizeAttribute;
                if (normallizeAttribute != null)
                    results.Add(new VFXPropertyAttribute(Type.kNormalize));

                TooltipAttribute tooltipAttribute = attribute as TooltipAttribute;
                if (tooltipAttribute != null)
                    results.Add(new VFXPropertyAttribute(tooltipAttribute.tooltip));

                StringProviderAttribute stringProviderAttribute = attribute as StringProviderAttribute;
                if (stringProviderAttribute != null)
                    results.Add(new VFXPropertyAttribute(stringProviderAttribute.m_ProviderType));
            }

            return results.ToArray();
        }

        public static VFXExpression ApplyToExpressionGraph(VFXPropertyAttribute[] attributes, VFXExpression exp)
        {
            if (attributes != null)
            {
                foreach (VFXPropertyAttribute attribute in attributes)
                {
                    switch (attribute.m_Type)
                    {
                        case Type.kRange:
                            exp = VFXOperatorUtility.UnifyOp(VFXOperatorUtility.Clamp, exp, VFXValue.Constant(attribute.m_Min), VFXValue.Constant(attribute.m_Max));
                            break;
                        case Type.kMin:
                            exp = new VFXExpressionMax(exp, VFXOperatorUtility.CastFloat(VFXValue.Constant(attribute.m_Min), exp.ValueType));
                            break;
                        case Type.kNormalize:
                            exp = VFXOperatorUtility.Normalize(exp);
                            break;
                        case Type.kTooltip:
                            break;
                        case Type.kStringProvider:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            return exp;
        }

        public static void ApplyToGUI(VFXPropertyAttribute[] attributes, ref string label, ref string tooltip)
        {
            if (attributes != null)
            {
                foreach (VFXPropertyAttribute attribute in attributes)
                {
                    switch (attribute.m_Type)
                    {
                        case Type.kRange:
                            break;
                        case Type.kMin:
                            label += " (Min: " + attribute.m_Min + ")";
                            break;
                        case Type.kNormalize:
                            label += " (Normalized)";
                            break;
                        case Type.kTooltip:
                            tooltip = attribute.m_Tooltip;
                            break;
                        case Type.kStringProvider:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public static Vector2 FindRange(VFXPropertyAttribute[] attributes)
        {
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.m_Type == Type.kRange)
                        return new Vector2(attribute.m_Min, attribute.m_Max);
                }
            }

            return Vector2.zero;
        }

        public static Func<string[]> FindStringProvider(VFXPropertyAttribute[] attributes)
        {
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.m_Type == Type.kStringProvider)
                    {
                        var instance = Activator.CreateInstance(attribute.m_stringProvider);
                        if (instance is IStringProvider)
                        {
                            var stringProvider = instance as IStringProvider;
                            return () => stringProvider.GetAvailableString();
                        }
                    }
                }
            }
            return null;
        }

        private enum Type
        {
            kRange,
            kMin,
            kNormalize,
            kTooltip,
            kStringProvider
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

        private VFXPropertyAttribute(System.Type stringProvider)
        {
            m_Type = Type.kStringProvider;
            m_stringProvider = stringProvider;
            m_Min = -Mathf.Infinity;
            m_Max = Mathf.Infinity;
            m_Tooltip = null;
        }

        [SerializeField]
        private Type m_Type;
        [SerializeField]
        private float m_Min;
        [SerializeField]
        private float m_Max;
        [SerializeField]
        private string m_Tooltip;
        [SerializeField]
        private SerializableType m_stringProviderSerializedType;
        private System.Type m_stringProvider
        {
            get
            {
                return m_stringProviderSerializedType;
            }
            set
            {
                m_stringProviderSerializedType = value;
            }
        }
    }
}

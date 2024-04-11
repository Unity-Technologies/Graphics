using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    sealed class EnumAttribute : PropertyAttribute
    {
        public EnumAttribute(string[] values)
        {
            this.values = values;
        }

        public readonly string[] values;
    }

    // Attribute used to normalize a Vector or float
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    sealed class NormalizeAttribute : PropertyAttribute
    {
    }

    // Attribute used to display a float in degrees in the UI
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    sealed class AngleAttribute : PropertyAttribute
    {
    }

    // Attribute to setup logarithmic sliders
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    sealed class LogarithmicAttribute : PropertyAttribute
    {
        public LogarithmicAttribute(float @base, bool snap)
        {
            logBase = @base;
            snapToPower = snap;
        }

        public float logBase { get; } = 10;
        public bool snapToPower { get; } = false;
    }

    // Attribute used to constrain a property to a Regex query
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    sealed class RegexAttribute : PropertyAttribute
    {
        public RegexAttribute(string _pattern, int _maxLength = int.MaxValue) { pattern = _pattern; maxLength = _maxLength; }

        public string pattern { get; set; }
        public int maxLength { get; set; }
    }

    struct VFXPropertyAttributes
    {
        [Flags]
        public enum Type
        {
            Range = GraphAttribute | (1 << 0),
            Min = GraphAttribute | (1 << 1),
            Normalized = GraphAttribute | (1 << 2),
            Tooltip = 1 << 3,
            Angle = 1 << 4,
            Color = 1 << 5,
            Regex = 1 << 6,
            Delayed = 1 << 7,
            BitField = 1 << 8,
            Enum = GraphAttribute | 1 << 9,
            MinMax = GraphAttribute | 1 << 10,
            Logarithmic = GraphAttribute | 1 << 11,

            // Tells whether this attribute modifies the expression graph
            GraphAttribute = 1 << 31,
        }

        private static readonly Dictionary<System.Type, Type> s_RegisteredAttributes = new Dictionary<System.Type, Type>()
        {
            { typeof(RangeAttribute),       Type.Range },
            { typeof(MinAttribute),         Type.Min },
            { typeof(NormalizeAttribute),   Type.Normalized },
            { typeof(TooltipAttribute),     Type.Tooltip },
            { typeof(AngleAttribute),       Type.Angle },
            { typeof(ShowAsColorAttribute), Type.Color },
            { typeof(RegexAttribute),       Type.Regex },
            { typeof(DelayedAttribute),     Type.Delayed },
            { typeof(BitFieldAttribute),    Type.BitField },
            { typeof(EnumAttribute),        Type.Enum },
            { typeof(MinMaxAttribute),      Type.MinMax},
            { typeof(LogarithmicAttribute), Type.Logarithmic},
        };

        public VFXPropertyAttributes(params object[] attributes) : this()
        {
            if (attributes != null && attributes.Length != 0)
            {
                if (attributes.Any(a => !(a is Attribute)))
                    throw new ArgumentException("Only C# attributes are allowed to be passed to this method");

                m_AllAttributes = attributes.Where(o => s_RegisteredAttributes.ContainsKey(o.GetType())).Cast<Attribute>().ToArray();
                m_GraphAttributes = m_AllAttributes.Where(o => (s_RegisteredAttributes[o.GetType()] & Type.GraphAttribute) != 0).ToArray();

                foreach (var attribute in m_AllAttributes)
                {
                    Type attributeType = s_RegisteredAttributes[attribute.GetType()];
                    // Check multi inclusion of the same attribute
                    if (Is(attributeType))
                        throw new ArgumentException($"The same property attribute type ({attribute.GetType()}) was added twice");
                    m_Flag |= attributeType;
                }
            }
            else
                m_AllAttributes = Array.Empty<Attribute>(); // Just to discriminate between uninitialized and no properties
        }

        public bool IsEqual(VFXPropertyAttributes other)
        {
            if (m_Flag != other.m_Flag)
                return false;

            if (m_AllAttributes == null)
                return other.m_AllAttributes == null;

            if (other.m_AllAttributes == null)
                return false;

            if (m_AllAttributes.Length != other.m_AllAttributes.Length)
                return false;

            for (int i = 0; i < m_AllAttributes.Length; ++i)
                if (!m_AllAttributes[i].Equals(other.m_AllAttributes[i]))
                    return false;

            return true;
        }

        public VFXExpression ApplyToExpressionGraph(VFXExpression exp)
        {
            if (m_GraphAttributes == null)
                return exp;

            foreach (PropertyAttribute attribute in m_GraphAttributes)
            {
                if (attribute is RangeAttribute)
                {
                    var rangeAttribute = (RangeAttribute)attribute;
                    switch (exp.valueType)
                    {
                        case VFXValueType.Int32:
                            exp = VFXOperatorUtility.Clamp(exp, VFXValue.Constant((int)rangeAttribute.min), VFXValue.Constant((int)rangeAttribute.max), false);
                            break;
                        case VFXValueType.Uint32:
                            exp = VFXOperatorUtility.Clamp(exp, VFXValue.Constant((uint)rangeAttribute.min), VFXValue.Constant((uint)rangeAttribute.max), false);
                            break;
                        case VFXValueType.Float:
                        case VFXValueType.Float2:
                        case VFXValueType.Float3:
                        case VFXValueType.Float4:
                            exp = VFXOperatorUtility.Clamp(exp, VFXValue.Constant(rangeAttribute.min), VFXValue.Constant(rangeAttribute.max));
                            break;
                        default:
                            throw new NotImplementedException(string.Format("Cannot use RangeAttribute on value of type: {0}", exp.valueType));
                    }
                }
                else if (attribute is MinAttribute)
                {
                    var minAttribute = (MinAttribute)attribute;
                    switch (exp.valueType)
                    {
                        case VFXValueType.Int32:
                            exp = new VFXExpressionMax(exp, VFXValue.Constant((int)minAttribute.min));
                            break;
                        case VFXValueType.Uint32:
                            exp = new VFXExpressionMax(exp, VFXValue.Constant((uint)minAttribute.min));
                            break;
                        case VFXValueType.Float:
                        case VFXValueType.Float2:
                        case VFXValueType.Float3:
                        case VFXValueType.Float4:
                            exp = new VFXExpressionMax(exp, VFXOperatorUtility.CastFloat(VFXValue.Constant(minAttribute.min), exp.valueType));
                            break;
                        default:
                            throw new NotImplementedException(string.Format("Cannot use MinAttribute on value of type: {0}", exp.valueType));
                    }
                }
                else if (attribute is NormalizeAttribute)
                {
                    exp = VFXOperatorUtility.Normalize(exp);
                }
                else if (attribute is EnumAttribute enumAttribute)
                {
                    exp = new VFXExpressionMin(exp, VFXValue.Constant((uint)enumAttribute.values.Length - 1));
                }
                else if (attribute is MinMaxAttribute minMaxAttribute)
                {
                    exp = VFXOperatorUtility.Clamp(exp, VFXValue.Constant(minMaxAttribute.min), VFXValue.Constant(minMaxAttribute.max));
                }
                else if (attribute is LogarithmicAttribute logarithmicAttribute)
                {
                    if (logarithmicAttribute.snapToPower)
                        exp = VFXOperatorUtility.SnapToClosestPowerOfBase(exp, VFXValue.Constant(logarithmicAttribute.logBase));
                }
                else
                    throw new NotImplementedException("Unrecognized expression attribute: " + attribute);
            }

            return exp;
        }

        public void ApplyToGUI(ref string label, ref string tooltip)
        {
            string tooltipAddon = "";
            if (m_AllAttributes != null)
                foreach (var attribute in m_AllAttributes)
                {
                    if (attribute is MinAttribute)
                        tooltipAddon += string.Format(CultureInfo.InvariantCulture, " (Min: {0})", ((MinAttribute)attribute).min);
                    else if (attribute is NormalizeAttribute)
                        tooltipAddon += " (Normalized)";
                    else if (attribute is TooltipAttribute)
                        tooltip = ((TooltipAttribute)attribute).tooltip;
                    else if (attribute is AngleAttribute)
                        tooltipAddon += " (Angle)";
                }

            if (string.IsNullOrEmpty(tooltip))
                tooltip = label;

            tooltip = tooltip + tooltipAddon;
        }

        public Vector2 FindRange()
        {
            if (Is(Type.Range))
            {
                var attribute = m_AllAttributes.OfType<RangeAttribute>().First();
                return new Vector2(attribute.min, attribute.max);
            }
            else if (Is(Type.Min))
            {
                var attribute = m_AllAttributes.OfType<MinAttribute>().First();
                return new Vector2(attribute.min, Mathf.Infinity);
            }
            else if (Is(Type.MinMax))
            {
                var attribute = m_AllAttributes.OfType<MinMaxAttribute>()
                    .First();
                return new Vector2(attribute.min, attribute.max);
            }

            return Vector2.zero;
        }

        public float FindLogarithmicBase()
        {
            if (Is(Type.Logarithmic))
            {
                var attribute = m_AllAttributes.OfType<LogarithmicAttribute>().Single();
                return attribute.logBase;
            }

            return -1f;
        }
        public bool FindSnapToPower()
        {
            if (Is(Type.Logarithmic))
            {
                var attribute = m_AllAttributes.OfType<LogarithmicAttribute>().Single();
                return attribute.snapToPower;
            }

            return false;
        }

        public string[] FindEnum()
        {
            if (Is(Type.Enum))
            {
                return m_AllAttributes.OfType<EnumAttribute>().First().values;
            }
            return null;
        }

        public string ApplyRegex(object obj)
        {
            if (Is(Type.Regex))
            {
                var attribute = m_AllAttributes.OfType<RegexAttribute>().First();
                string str = (string)obj;
                str = Regex.Replace(str, attribute.pattern, "");
                return str.Substring(0, Math.Min(str.Length, attribute.maxLength));
            }

            return null;
        }

        public bool Is(VFXPropertyAttributes.Type type)
        {
            return (m_Flag & type) == type;
        }

        public bool IsInitialized => m_AllAttributes != null;

        public IReadOnlyCollection<Attribute> attributes => m_AllAttributes != null ? m_AllAttributes : new Attribute[0];

        private Attribute[] m_GraphAttributes;
        private Attribute[] m_AllAttributes;
        private Type m_Flag;
    }
}

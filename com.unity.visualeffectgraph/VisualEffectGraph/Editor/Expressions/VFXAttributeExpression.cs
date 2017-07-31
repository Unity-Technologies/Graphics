using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Flags]
    enum VFXAttributeMode
    {
        None        = 0,
        Read        = 1 << 0,
        Write       = 1 << 1,
        ReadWrite   = Read | Write,
    }

    enum VFXAttributeLocation
    {
        Current = 0,
        Source = 1,
    }

    struct VFXAttribute
    {
        public static readonly VFXAttribute Seed               = new VFXAttribute("seed", VFXValueType.kUint);
        public static readonly VFXAttribute Position           = new VFXAttribute("position", VFXValueType.kFloat3);
        public static readonly VFXAttribute Velocity           = new VFXAttribute("velocity", VFXValueType.kFloat3);
        public static readonly VFXAttribute Color              = new VFXAttribute("color", VFXValueType.kFloat3);
        public static readonly VFXAttribute Alpha              = new VFXAttribute("alpha", VFXValueType.kFloat);
        public static readonly VFXAttribute Phase              = new VFXAttribute("phase", VFXValueType.kFloat);
        public static readonly VFXAttribute Size               = new VFXAttribute("size", VFXValueType.kFloat2);
        public static readonly VFXAttribute Lifetime           = new VFXAttribute("lifetime", VFXValueType.kFloat);
        public static readonly VFXAttribute Age                = new VFXAttribute("age", VFXValueType.kFloat);
        public static readonly VFXAttribute Angle              = new VFXAttribute("angle", VFXValueType.kFloat);
        public static readonly VFXAttribute AngularVelocity    = new VFXAttribute("angularVelocity", VFXValueType.kFloat);
        public static readonly VFXAttribute TexIndex           = new VFXAttribute("texIndex", VFXValueType.kFloat);
        public static readonly VFXAttribute Pivot              = new VFXAttribute("pivot", VFXValueType.kFloat3);
        public static readonly VFXAttribute ParticleId         = new VFXAttribute("particleId", VFXValueType.kUint);
        public static readonly VFXAttribute Front              = new VFXAttribute("front", VFXValueType.kFloat3);
        public static readonly VFXAttribute Side               = new VFXAttribute("side", VFXValueType.kFloat3);
        public static readonly VFXAttribute Up                 = new VFXAttribute("up", VFXValueType.kFloat3);

        public static readonly VFXAttribute[] AllAttribute = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXAttribute>(typeof(VFXAttribute), System.Reflection.BindingFlags.Public);
        public static readonly string[] All = AllAttribute.Select(e => e.name).ToArray();

        public VFXAttribute(string name, VFXValueType type, VFXAttributeLocation location = VFXAttributeLocation.Current)
        {
            this.name = name;
            this.type = type;
            this.location = location;
        }

        public static VFXAttribute Find(string attributeName, VFXAttributeLocation location)
        {
            if (!AllAttribute.Any(e => e.name == attributeName))
            {
                throw new Exception(string.Format("Unable to find attribute expression : {0}", attributeName));
            }

            var attribute = AllAttribute.First(e => e.name == attributeName);
            attribute.location = location;
            return attribute;
        }

        public string name;
        public VFXValueType type;
        public VFXAttributeLocation location;
    }

    struct VFXAttributeInfo
    {
        public VFXAttributeInfo(string name, VFXValueType type, VFXAttributeLocation location, VFXAttributeMode mode)
        {
            attrib.name = name;
            attrib.type = type;
            attrib.location = location;
            this.mode = mode;
        }

        public VFXAttributeInfo(VFXAttribute attrib, VFXAttributeMode mode)
        {
            this.attrib = attrib;
            this.mode = mode;
        }

        public VFXAttribute attrib;
        public VFXAttributeMode mode;
    }

    sealed class VFXAttributeExpression : VFXExpression
    {
        public VFXAttributeExpression(VFXAttribute attribute) : base(Flags.PerElement)
        {
            m_Attribute = attribute;
        }

        public override VFXExpressionOp Operation
        {
            get
            {
                return VFXExpressionOp.kVFXNoneOp;
            }
        }

        public override VFXValueType ValueType
        {
            get
            {
                return m_Attribute.type;
            }
        }

        public string attributeName
        {
            get
            {
                return m_Attribute.name;
            }
        }

        public VFXAttributeLocation attributeLocation
        {
            get
            {
                return m_Attribute.location;
            }
        }

        public VFXAttribute attribute { get { return m_Attribute; } }
        private VFXAttribute m_Attribute;


        public override bool Equals(object obj)
        {
            if (!(obj is VFXAttributeExpression))
                return false;

            var other = (VFXAttributeExpression)obj;
            return ValueType == other.ValueType && attributeName == other.attributeName;
        }

        public override int GetHashCode()
        {
            return attributeName.GetHashCode();
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        public override string GetCodeString(string[] parents)
        {
            return attributeName;
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            yield return new VFXAttributeInfo(attribute, VFXAttributeMode.Read);
        }
    }
}

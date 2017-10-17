using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [Flags]
    enum VFXAttributeMode
    {
        None        = 0,
        ReadCurrent = 1 << 0,
        WriteCurrent = 1 << 1,
        ReadWriteCurrent = ReadCurrent | WriteCurrent,
        ReadSource  = 1 << 2,
    }

    struct VFXAttribute
    {
        public static readonly VFXAttribute Seed                = new VFXAttribute("seed", VFXValueType.kUint);
        public static readonly VFXAttribute Position            = new VFXAttribute("position", VFXValueType.kFloat3);
        public static readonly VFXAttribute Velocity            = new VFXAttribute("velocity", VFXValueType.kFloat3);
        public static readonly VFXAttribute Color               = new VFXAttribute("color", VFXValue.Constant(Vector3.one));
        public static readonly VFXAttribute Alpha               = new VFXAttribute("alpha", VFXValue.Constant(1.0f));
        public static readonly VFXAttribute Phase               = new VFXAttribute("phase", VFXValueType.kFloat);
        public static readonly VFXAttribute Size                = new VFXAttribute("size", VFXValue.Constant(new Vector2(0.1f, 0.1f)));
        public static readonly VFXAttribute Lifetime            = new VFXAttribute("lifetime", VFXValueType.kFloat);
        public static readonly VFXAttribute Age                 = new VFXAttribute("age", VFXValueType.kFloat);
        public static readonly VFXAttribute Angle               = new VFXAttribute("angle", VFXValueType.kFloat);
        public static readonly VFXAttribute AngularVelocity     = new VFXAttribute("angularVelocity", VFXValueType.kFloat);
        public static readonly VFXAttribute TexIndex            = new VFXAttribute("texIndex", VFXValueType.kFloat);
        public static readonly VFXAttribute Pivot               = new VFXAttribute("pivot", VFXValueType.kFloat3);
        public static readonly VFXAttribute ParticleId          = new VFXAttribute("particleId", VFXValueType.kUint);
        public static readonly VFXAttribute Front               = new VFXAttribute("front", VFXValue.Constant(Vector3.forward));
        public static readonly VFXAttribute Side                = new VFXAttribute("side", VFXValue.Constant(Vector3.right));
        public static readonly VFXAttribute Up                  = new VFXAttribute("up", VFXValue.Constant(Vector3.up));
        public static readonly VFXAttribute Alive               = new VFXAttribute("alive", VFXValue.Constant(true));

        public static readonly VFXAttribute[] AllAttribute = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXAttribute>(typeof(VFXAttribute));
        public static readonly string[] All = AllAttribute.Select(e => e.name).ToArray();

        static private VFXValue GetValueFromType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kBool: return VFXValue.Constant<bool>();
                case VFXValueType.kUint: return VFXValue.Constant<uint>();
                case VFXValueType.kInt: return VFXValue.Constant<int>();
                case VFXValueType.kFloat: return VFXValue.Constant<float>();
                case VFXValueType.kFloat2: return VFXValue.Constant<Vector2>();
                case VFXValueType.kFloat3: return VFXValue.Constant<Vector3>();
                case VFXValueType.kFloat4: return VFXValue.Constant<Vector4>();
                default: throw new InvalidOperationException(string.Format("Unexpected attribute type: {0}", type));
            }
        }

        public VFXAttribute(string name, VFXValueType type)
        {
            this.name = name;
            this.value = GetValueFromType(type);
        }

        public VFXAttribute(string name, VFXValue value)
        {
            this.name = name;
            this.value = value;
        }

        public static VFXAttribute Find(string attributeName)
        {
            if (!AllAttribute.Any(e => e.name == attributeName))
            {
                throw new Exception(string.Format("Unable to find attribute expression : {0}", attributeName));
            }

            var attribute = AllAttribute.First(e => e.name == attributeName);
            return attribute;
        }

        public string name;
        public VFXValue value;
        public VFXValueType type
        {
            get
            {
                return value.valueType;
            }
        }
    }

    struct VFXAttributeInfo
    {
        public VFXAttributeInfo(VFXAttribute attrib, VFXAttributeMode mode)
        {
            this.attrib = attrib;
            this.mode = mode;
        }

        public VFXAttribute attrib;
        public VFXAttributeMode mode;
    }

    enum VFXAttributeLocation
    {
        Current = 0,
        Source = 1,
    }

    sealed class VFXAttributeExpression : VFXExpression
    {
        public VFXAttributeExpression(VFXAttribute attribute, VFXAttributeLocation location = VFXAttributeLocation.Current) : base(Flags.PerElement)
        {
            m_attribute = attribute;
            m_attributeLocation = location;
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXNoneOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return m_attribute.type;
            }
        }

        public string attributeName
        {
            get
            {
                return m_attribute.name;
            }
        }

        public VFXAttributeLocation attributeLocation
        {
            get
            {
                return m_attributeLocation;
            }
        }

        public VFXAttribute attribute { get { return m_attribute; } }
        private VFXAttribute m_attribute;
        private VFXAttributeLocation m_attributeLocation;

        public override bool Equals(object obj)
        {
            if (!(obj is VFXAttributeExpression))
                return false;

            var other = (VFXAttributeExpression)obj;
            return valueType == other.valueType && attributeName == other.attributeName && attributeLocation == other.attributeLocation;
        }

        public override int GetHashCode()
        {
            return (attributeName.GetHashCode() * 397) ^ attributeLocation.GetHashCode();
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        public override string GetCodeString(string[] parents)
        {
            return attributeLocation == VFXAttributeLocation.Current ? attributeName : attributeName + "_source";
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            yield return new VFXAttributeInfo(attribute, m_attributeLocation == VFXAttributeLocation.Source ? VFXAttributeMode.ReadSource : VFXAttributeMode.ReadCurrent);
        }
    }
}

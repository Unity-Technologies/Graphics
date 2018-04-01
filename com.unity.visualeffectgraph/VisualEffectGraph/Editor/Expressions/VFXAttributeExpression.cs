using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Flags]
    enum VFXAttributeMode
    {
        None        = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write,
        ReadSource  = 1 << 2,
    }

    struct VFXAttribute
    {
        public static readonly float kDefaultSize = 0.1f;

        public static readonly VFXAttribute Seed                = new VFXAttribute("seed", VFXValueType.Uint32);
        public static readonly VFXAttribute OldPosition         = new VFXAttribute("oldPosition", VFXValueType.Float3);
        public static readonly VFXAttribute Position            = new VFXAttribute("position", VFXValueType.Float3);
        public static readonly VFXAttribute Velocity            = new VFXAttribute("velocity", VFXValueType.Float3);
        public static readonly VFXAttribute Color               = new VFXAttribute("color", VFXValue.Constant(Vector3.one));
        public static readonly VFXAttribute Alpha               = new VFXAttribute("alpha", VFXValue.Constant(1.0f));
        public static readonly VFXAttribute SizeX               = new VFXAttribute("sizeX", VFXValue.Constant(kDefaultSize));
        public static readonly VFXAttribute SizeY               = new VFXAttribute("sizeY", VFXValue.Constant(kDefaultSize));
        public static readonly VFXAttribute SizeZ               = new VFXAttribute("sizeZ", VFXValue.Constant(kDefaultSize));
        public static readonly VFXAttribute Lifetime            = new VFXAttribute("lifetime", VFXValueType.Float);
        public static readonly VFXAttribute Age                 = new VFXAttribute("age", VFXValueType.Float);
        public static readonly VFXAttribute AngleX              = new VFXAttribute("angleX", VFXValueType.Float);
        public static readonly VFXAttribute AngleY              = new VFXAttribute("angleY", VFXValueType.Float);
        public static readonly VFXAttribute AngleZ              = new VFXAttribute("angleZ", VFXValueType.Float);
        public static readonly VFXAttribute AngularVelocity     = new VFXAttribute("angularVelocity", VFXValueType.Float);
        public static readonly VFXAttribute TexIndex            = new VFXAttribute("texIndex", VFXValueType.Float);
        public static readonly VFXAttribute Pivot               = new VFXAttribute("pivot", VFXValueType.Float3);
        public static readonly VFXAttribute ParticleId          = new VFXAttribute("particleId", VFXValueType.Uint32);
        public static readonly VFXAttribute AxisX               = new VFXAttribute("axisX", VFXValue.Constant(Vector3.right));
        public static readonly VFXAttribute AxisY               = new VFXAttribute("axisY", VFXValue.Constant(Vector3.up));
        public static readonly VFXAttribute AxisZ               = new VFXAttribute("axisZ", VFXValue.Constant(Vector3.forward));
        public static readonly VFXAttribute Alive               = new VFXAttribute("alive", VFXValue.Constant(true));
        public static readonly VFXAttribute Mass                = new VFXAttribute("mass", VFXValue.Constant(1.0f));
        public static readonly VFXAttribute TargetPosition      = new VFXAttribute("targetPosition", VFXValueType.Float3);
        public static readonly VFXAttribute[] AllAttributeReadOnly = new VFXAttribute[] { Seed, ParticleId };
        public static readonly string[] AllReadOnly = AllAttributeReadOnly.Select(e => e.name).ToArray();

        public static readonly VFXAttribute[] AllAttribute = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXAttribute>(typeof(VFXAttribute));
        public static readonly string[] All = AllAttribute.Select(e => e.name).ToArray();
        public static readonly string[] AllWritable = All.Except(AllReadOnly).ToArray();

        static private VFXValue GetValueFromType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Boolean: return VFXValue.Constant<bool>();
                case VFXValueType.Uint32: return VFXValue.Constant<uint>();
                case VFXValueType.Int32: return VFXValue.Constant<int>();
                case VFXValueType.Float: return VFXValue.Constant<float>();
                case VFXValueType.Float2: return VFXValue.Constant<Vector2>();
                case VFXValueType.Float3: return VFXValue.Constant<Vector3>();
                case VFXValueType.Float4: return VFXValue.Constant<Vector4>();
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
        Initial = 2
    }

    sealed class VFXAttributeExpression : VFXExpression
    {
        public VFXAttributeExpression(VFXAttribute attribute, VFXAttributeLocation location = VFXAttributeLocation.Current) : base(Flags.PerElement)
        {
            m_attribute = attribute;
            m_attributeLocation = location;
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.None;
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
            return valueType == other.valueType && attributeLocation == other.attributeLocation && attributeName == other.attributeName;
        }

        protected override int GetInnerHashCode()
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
            yield return new VFXAttributeInfo(attribute, m_attributeLocation == VFXAttributeLocation.Source ? VFXAttributeMode.ReadSource : VFXAttributeMode.Read);
        }
    }
}

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
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write,
        ReadSource = 1 << 2,
    }

    partial struct VFXAttribute : IEquatable<VFXAttribute>
    {
        public VFXAttribute(string name, VFXValueType type, string description, VFXVariadic variadic = VFXVariadic.False, SpaceableType space = SpaceableType.None)
            : this(name, GetValueFromType(type), description, variadic, space)
        {
        }

        public VFXAttribute(string name, VFXValue value, string description, VFXVariadic variadic = VFXVariadic.False, SpaceableType space = SpaceableType.None)
        {
            this.name = name;
            this.value = value;
            this.variadic = variadic;
            this.space = space;
            this.description = description;
            if (space != SpaceableType.None && variadic != VFXVariadic.False)
            {
                throw new InvalidOperationException("Can't mix spaceable and variadic attributes : " + name);
            }

            category = "No Category";
        }

        public string GetNameInCode(VFXAttributeLocation location)
        {
            var structName = location == VFXAttributeLocation.Source ? "sourceAttributes" : "attributes";
            return $"{structName}.{name}";
        }

        public string name;
        public VFXValue value;
        public VFXVariadic variadic;
        public SpaceableType space;
        public string description;
        public string category { get; set; }

        public VFXValueType type => value.valueType;

        public void Rename(string newName)
        {
            name = newName;
        }

        private static VFXValue GetValueFromType(VFXValueType type)
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
                default: throw new InvalidOperationException($"Unexpected attribute type: {type}");
            }
        }

        public bool Equals(VFXAttribute other)
        {
            return string.Compare(name, other.name, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
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

    enum VFXVariadic
    {
        False = 0,
        True = 1,
        BelongsToVariadic = 2
    }

    enum VariadicChannelOptions
    {
        X = 0,
        Y = 1,
        Z = 2,
        XY = 3,
        XZ = 4,
        YZ = 5,
        XYZ = 6
    };

#pragma warning disable 0659
    sealed class VFXAttributeExpression : VFXExpression
    {
        public VFXAttributeExpression(VFXAttribute attribute, VFXAttributeLocation location = VFXAttributeLocation.Current) : base(Flags.PerElement)
        {
            m_attribute = attribute;
            m_attributeLocation = location;
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.None;

        public override VFXValueType valueType => m_attribute.type;

        public string attributeName => m_attribute.name;

        public VFXAttributeLocation attributeLocation => m_attributeLocation;

        public VFXAttribute attribute => m_attribute;
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

        protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        public override string GetCodeString(string[] parents)
        {
            return attribute.GetNameInCode(attributeLocation);
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            yield return new VFXAttributeInfo(attribute, m_attributeLocation == VFXAttributeLocation.Source ? VFXAttributeMode.ReadSource : VFXAttributeMode.Read);
        }
    }


    sealed class VFXReadEventAttributeExpression : VFXExpression
    {
        private VFXAttribute m_attribute;
        private UInt32 m_elementOffset;

        public VFXReadEventAttributeExpression(VFXAttribute attribute, UInt32 elementOffset) : base(Flags.PerSpawn | Flags.InvalidOnGPU)
        {
            m_attribute = attribute;
            m_elementOffset = elementOffset;
        }

        protected override int GetInnerHashCode()
        {
            return m_attribute.name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXReadEventAttributeExpression))
                return false;

            var other = (VFXReadEventAttributeExpression)obj;
            return valueType == other.valueType && attributeName == other.attributeName && m_elementOffset == other.elementOffset;
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            yield return new VFXAttributeInfo(m_attribute, VFXAttributeMode.Read);
        }

        private UInt32 elementOffset => m_elementOffset;
        private string attributeName => m_attribute.name;
        public override VFXValueType valueType => m_attribute.type;
        public override VFXExpressionOperation operation => VFXExpressionOperation.ReadEventAttribute;
        protected override int[] additionnalOperands => new int[] { (int)m_elementOffset, (int)m_attribute.type };
    }

#pragma warning restore 0659
}

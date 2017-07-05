using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Serializable]
    struct VFXProperty
    {
        private Type m_type;
        public Type type
        {
            get
            {
                if (m_type == null)
                {
                    m_type = Type.GetType(typeName);
                }
                return m_type;
            }
        }

        public string name;
        [SerializeField]
        private string typeName;

        [SerializeField]
        private VFXPropertyAttribute[] attributes;

        public VFXProperty(Type type, string name)
        {
            this.name = name;
            typeName = type.AssemblyQualifiedName;
            m_type = null;
            attributes = null;
        }

        public VFXProperty(FieldInfo info)
        {
            this.name = info.Name;
            typeName = info.FieldType.AssemblyQualifiedName;
            m_type = null;
            attributes = VFXPropertyAttribute.Create(info.GetCustomAttributes(true));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXProperty))
                return false;

            VFXProperty other = (VFXProperty)obj;
            return name == other.name && typeName == other.typeName;
        }

        public IEnumerable<VFXProperty> SubProperties()
        {
            if (IsExpandable())
            {
                FieldInfo[] infos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                return infos.Where(info => info.FieldType != typeof(CoordinateSpace)) // TODO filter out Coordinate space is tmp. Should be changed
                    .Select(info => new VFXProperty(info));
            }
            else
            {
                return Enumerable.Empty<VFXProperty>();
            }
        }

        public bool IsExpandable()
        {
            return !type.IsPrimitive && !typeof(UnityEngine.Object).IsAssignableFrom(type) && type != typeof(AnimationCurve);
        }

        public VFXExpression ApplyAttributes(VFXExpression exp)
        {
            if (attributes != null)
            {
                foreach (VFXPropertyAttribute attribute in attributes)
                    exp = attribute.Apply(exp);
            }

            return exp;
        }
    }
}

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
        public Type type
        {
            get
            {
                return m_serializedType;
            }
            private set
            {
                m_serializedType = value;
            }
        }

        public string name;
        [SerializeField]
        private SerializableType m_serializedType;

        [SerializeField]
        public VFXPropertyAttribute[] attributes;

        public VFXProperty(Type type, string name)
        {
            m_serializedType = type;
            this.name = name;
            attributes = null;
        }

        public VFXProperty(FieldInfo info)
        {
            name = info.Name;
            m_serializedType = info.FieldType;
            attributes = VFXPropertyAttribute.Create(info.GetCustomAttributes(true));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXProperty))
                return false;

            VFXProperty other = (VFXProperty)obj;
            return name == other.name && type == other.type;
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
    }
}

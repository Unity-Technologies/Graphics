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

        public VFXProperty(Type type, string name)
        {
            this.name = name;
            typeName = type.AssemblyQualifiedName;
            m_type = null;
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
                    .Select(info => new VFXProperty(info.FieldType, info.Name));
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

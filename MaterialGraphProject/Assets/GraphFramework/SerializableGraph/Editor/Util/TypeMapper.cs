using System;
using System.Collections.Generic;

namespace UnityEditor.Graphing.Util
{
    public class TypeMapper
    {
        private readonly Dictionary<Type, Type> m_Mappings = new Dictionary<Type, Type>();
        private readonly Type m_Default;

        public TypeMapper() : this(null) {}

        public TypeMapper(Type defaultType)
        {
            m_Default = defaultType;
        }

        public void Clear()
        {
            m_Mappings.Clear();
        }

        public void AddMapping(Type from, Type to)
        {
            m_Mappings[from] = to;
        }

        public Type MapType(Type type)
        {
            Type found = null;
            while (type != null)
            {
                if (m_Mappings.TryGetValue(type, out found))
                    break;
                type = type.BaseType;
            }
            return found ?? m_Default;
        }
    }
}

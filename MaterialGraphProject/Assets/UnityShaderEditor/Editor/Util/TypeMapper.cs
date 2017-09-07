using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Graphing.Util
{
    public class TypeMapper<TFrom, TTo> : IEnumerable<TypeMapping>
    {
        readonly Type m_FallbackType;
        readonly Dictionary<Type, Type> m_Mappings = new Dictionary<Type, Type>();

        public TypeMapper(Type fallbackType = null)
        {
            if (fallbackType != null && !(fallbackType.IsSubclassOf(typeof(TFrom)) || fallbackType.GetInterfaces().Contains(typeof(TFrom))))
                throw new ArgumentException(string.Format("{0} does not implement or derive from {1}.", fallbackType.Name, typeof(TFrom).Name), "fallbackType");
            m_FallbackType = fallbackType;
        }

        public void Add(TypeMapping mapping)
        {
            Add(mapping.fromType, mapping.toType);
        }

        public void Add(Type fromType, Type toType)
        {
            if (!fromType.IsSubclassOf(typeof(TFrom)) && !fromType.GetInterfaces().Contains(typeof(TFrom)))
            {
                throw new ArgumentException(string.Format("{0} does not implement or derive from {1}.", fromType.Name, typeof(TFrom).Name), "fromType");
            }

            if (!toType.IsSubclassOf(typeof(TTo)))
            {
                throw new ArgumentException(string.Format("{0} does not derive from {1}.", toType.Name, typeof(TTo).Name), "toType");
            }

            m_Mappings[fromType] = toType;
        }

        public Type MapType(Type fromType)
        {
            Type toType = null;

            while (toType == null && fromType != null && fromType != typeof(TFrom))
            {
                if (!m_Mappings.TryGetValue(fromType, out toType))
                    fromType = fromType.BaseType;
            }

            return toType ?? m_FallbackType;
        }

        public IEnumerator<TypeMapping> GetEnumerator()
        {
            return m_Mappings.Select(kvp => new TypeMapping(kvp.Key, kvp.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Graphing.Util
{
	public class TypeMapper<TFrom, TTo, TFallback>
        where TFallback : TTo
    {
        private readonly Dictionary<Type, Type> m_Mappings = new Dictionary<Type, Type>();

        public void AddMapping(TypeMapping mapping)
        {
            AddMapping(mapping.fromType, mapping.toType);
        }

        public void AddMapping(Type fromType, Type toType)
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

            return toType ?? typeof(TFallback);
        }
    }
}

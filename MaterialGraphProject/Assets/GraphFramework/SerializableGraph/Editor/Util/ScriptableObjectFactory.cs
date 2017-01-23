using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Graphing.Util
{
    public class ScriptableObjectFactory<TFrom, TTo, TFallback>
        where TTo : ScriptableObject
        where TFallback : TTo
    {
        private readonly TypeMapper<TFrom, TTo, TFallback> m_TypeMapper = new TypeMapper<TFrom, TTo, TFallback>();

        public ScriptableObjectFactory(IEnumerable<TypeMapping> typeMappings)
        {
            foreach (var typeMapping in typeMappings)
                m_TypeMapper.AddMapping(typeMapping);
        }

        public TTo Create(TFrom from)
        {
            var toType = m_TypeMapper.MapType(from.GetType());
            return ScriptableObject.CreateInstance(toType) as TTo;
        }
    }
}

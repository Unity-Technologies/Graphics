using System;
using Newtonsoft.Json.Serialization;

namespace Importers
{
    class ConstantValueProvider : IValueProvider
    {
        readonly object m_Value;

        public ConstantValueProvider(object value)
        {
            m_Value = value;
        }

        public void SetValue(object target, object value)
        {
            throw new InvalidOperationException("The JsonProperty using ConstantValueProvider must have Writable = false.");
        }

        public object GetValue(object target)
        {
            return m_Value;
        }
    }
}

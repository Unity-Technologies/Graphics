using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // A "Header" object is an abstraction layer that interprets domain/context-free
    // information from a ShaderObject (in this case a function) and translates it into
    // domain specific information.
    abstract class StrongHeader<T> where T : IShaderObject
    {
        internal bool isValid { get; private set; }

        protected HintRegistry<T> HintRegistry => GetHintRegistry();

        Dictionary<string, object> m_values;
        List<string> m_msgs;

        internal IEnumerable<string> Messages => m_msgs;

        protected bool Has(string key) => m_values.ContainsKey(key);

        protected V Get<V>(string key)
        {
            m_values.TryGetValue(key, out object value);
            return value is V ? (V)value : default;
        }

        abstract protected HintRegistry<T> GetHintRegistry();

        // Runs once per Process call, which should be run whenever the ShaderOject and subsequent model are updated.
        // Get/Has should be used here to populate the Header with meaningful information.
        abstract protected void OnProcess(T obj, IProvider provider);

        internal void Process(T obj, IProvider provider)
        {
            isValid = obj != null && obj.IsValid && provider != null && provider.IsValid;

            if (!isValid)
                return;

            HintRegistry.ProcessObject(obj, provider, out m_values, out m_msgs);
            OnProcess(obj, provider);
        }
    }
}

using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public class ShaderPropertyCollection : IEnumerable<AbstractShaderProperty>
    {
        readonly List<AbstractShaderProperty> m_Properties;

        public ShaderPropertyCollection()
        {
            m_Properties = new List<AbstractShaderProperty>();
        }

        public void Add(AbstractShaderProperty prop)
        {
            m_Properties.Add(prop);
        }

        public void Add(ShaderPropertyCollection collection)
        {
            foreach (AbstractShaderProperty prop in collection)
            {
                m_Properties.Add(prop);
            }
        }

        public IEnumerator<AbstractShaderProperty> GetEnumerator()
        {
            return m_Properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

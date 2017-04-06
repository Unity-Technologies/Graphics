using System.Reflection;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public abstract class SkySettings : ScriptableObject
    {
        protected class Unhashed : System.Attribute {}

        public float                    rotation = 0.0f;
        public float                    exposure = 0.0f;
        public float                    multiplier = 1.0f;
        public SkyResolution            resolution = SkyResolution.SkyResolution256;
        public EnvironementUpdateMode   updateMode = EnvironementUpdateMode.OnChanged;
        public float                    updatePeriod = 0.0f;
        public Cubemap                  lightingOverride = null;

        private FieldInfo[] m_Properties;

        protected void OnEnable()
        {
            // Enumerate properties in order to compute the hash more quickly later on.
            m_Properties = GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
        }

        public int GetHash()
        {
            unchecked
            {
                int hash = 13;
                foreach (var p in m_Properties)
                {
                    bool unhashedAttribute = p.GetCustomAttributes(typeof(Unhashed), true).Length != 0;
                    object obj = p.GetValue(this);
                    if (obj != null && !unhashedAttribute) // Sometimes it can be a null reference.
                        hash = hash * 23 + obj.GetHashCode();
                }
                return hash;
            }
        }

        public abstract SkyRenderer GetRenderer();
    }
}

using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class FloatPropertyChunk : PropertyChunk
    {
        private readonly float m_DefaultValue;
        public FloatPropertyChunk(string propertyName, string propertyDescription, float defaultValue, HideState hideState)
            : base(propertyName, propertyDescription, hideState)
        {
            m_DefaultValue = defaultValue;
        }

        public float defaultValue
        {
            get { return m_DefaultValue; }
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            result.Append(propertyName);
            result.Append("(\"");
            result.Append(propertyDescription);
            result.Append("\", Float) = ");
            result.Append(defaultValue);
            return result.ToString();
        }
    }
}

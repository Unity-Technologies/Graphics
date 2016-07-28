using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class VectorPropertyChunk : PropertyChunk
    {
        private readonly Vector4 m_DefaultVector;

        public VectorPropertyChunk(string propertyName, string propertyDescription, Vector4 defaultVector, HideState hideState)
            : base(propertyName, propertyDescription, hideState)
        {
            m_DefaultVector = defaultVector;
        }

        public Vector4 defaultValue
        {
            get { return m_DefaultVector; }
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            result.Append(propertyName);
            result.Append("(\"");
            result.Append(propertyDescription);
            result.Append("\", Vector) = (");
            result.Append(defaultValue.x);
            result.Append(",");
            result.Append(defaultValue.y);
            result.Append(",");
            result.Append(defaultValue.z);
            result.Append(",");
            result.Append(defaultValue.w);
            result.Append(")");
            return result.ToString();
        }
    }
}

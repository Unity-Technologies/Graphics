using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class ColorPropertyChunk : PropertyChunk
    {
        private Color m_DefaultColor;

        public ColorPropertyChunk(string propertyName, string propertyDescription, Color defaultColor, HideState hideState)
            : base(propertyName, propertyDescription, hideState)
        {
            m_DefaultColor = defaultColor;
        }

        public Color defaultColor
        {
            get { return m_DefaultColor; }
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            result.Append(propertyName);
            result.Append("(\"");
            result.Append(propertyDescription);
            result.Append("\", Color) = (");
            result.Append(defaultColor.r);
            result.Append(",");
            result.Append(defaultColor.g);
            result.Append(",");
            result.Append(defaultColor.b);
            result.Append(",");
            result.Append(defaultColor.a);
            result.Append(")");
            return result.ToString();
        }
    }
}

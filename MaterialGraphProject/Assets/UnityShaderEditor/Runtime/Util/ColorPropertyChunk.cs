using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class ColorPropertyChunk : PropertyChunk
    {
        public enum ColorType
        {
            Default,
            HDR
        }

        private Color m_DefaultColor;
        private ColorType m_colorType;

        public ColorPropertyChunk(string propertyName, string propertyDescription, Color defaultColor, ColorType colorType, HideState hideState)
            : base(propertyName, propertyDescription, hideState)
        {
            m_colorType = colorType;
            m_DefaultColor = defaultColor;
        }

        public Color defaultColor
        {
            get { return m_DefaultColor; }
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            if (m_colorType == ColorType.HDR)
                result.Append("[HDR]");
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

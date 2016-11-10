using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class PropertyGenerator
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public TexturePropertyChunk.ModifiableState modifiable;
        }

        private readonly List<PropertyChunk> m_Properties = new List<PropertyChunk>();

        public void AddShaderProperty(PropertyChunk chunk)
        {
            if (m_Properties.Any(x => x.propertyName == chunk.propertyName))
                return;
            m_Properties.Add(chunk);
        }

        public string GetShaderString(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var prop in m_Properties)
            {
                for (var i = 0; i < baseIndentLevel; i++)
                    sb.Append("\t");
                sb.AppendLine(prop.GetPropertyString());
            }
            return sb.ToString();
        }

        public List<TextureInfo> GetConfiguredTexutres()
        {
            var result = new List<TextureInfo>();

            foreach (var prop in m_Properties.OfType<TexturePropertyChunk>())
            {
                if (prop.propertyName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.propertyName,
                        textureId = prop.defaultTexture != null ? prop.defaultTexture.GetInstanceID() : 0,
                        modifiable = prop.modifiableState
                    };
                    result.Add(textureInfo);
                }
            }
            return result;
        }
    }
}

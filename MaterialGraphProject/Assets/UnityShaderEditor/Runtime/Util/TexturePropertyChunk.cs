using System;
using System.Text;

namespace UnityEngine.MaterialGraph
{
    public class TexturePropertyChunk : PropertyChunk
    {
        public enum ModifiableState
        {
            Modifiable,
            NonModifiable
        }

        private readonly Texture m_DefaultTexture;
        private readonly TextureType m_DefaultTextureType;
        private readonly ModifiableState m_Modifiable;

        public TexturePropertyChunk(string propertyName, string propertyDescription, Texture defaultTexture, TextureType defaultTextureType, HideState hidden, ModifiableState modifiableState)
            : base(propertyName, propertyDescription, hidden)
        {
            m_DefaultTexture = defaultTexture;
            m_DefaultTextureType = defaultTextureType;
            m_Modifiable = modifiableState;
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            if (hideState == HideState.Hidden)
                result.Append("[HideInInspector] ");
            if (m_Modifiable == ModifiableState.NonModifiable)
                result.Append("[NonModifiableTextureData] ");

            result.Append(propertyName);
            result.Append("(\"");
            result.Append(propertyDescription);
            result.Append("\", 2D) = \"");
            result.Append(Enum.GetName(typeof(TextureType), m_DefaultTextureType).ToLower());
            result.Append("\" {}");
            return result.ToString();
        }

        public Texture defaultTexture
        {
            get
            {
                return m_DefaultTexture;
            }
        }
        public ModifiableState modifiableState
        {
            get
            {
                return m_Modifiable;
            }
        }
    }
}

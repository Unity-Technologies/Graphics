using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class TextureProperty : ShaderProperty
    {
        [SerializeField] private Texture2D m_DefaultTexture;

        [SerializeField] private TextureType m_DefaultTextureType;

        public Texture2D defaultTexture
        {
            get { return m_DefaultTexture; }
            set { m_DefaultTexture = value; }
        }

        public TextureType defaultTextureType
        {
            get { return m_DefaultTextureType; }
            set { m_DefaultTextureType = value; }
        }

        public override object value
        {
            get { return m_DefaultTexture; }
            set { m_DefaultTexture = value as Texture2D; }
        }

        public override void PropertyOnGUI(out bool nameChanged, out bool valuesChanged)
        {
            base.PropertyOnGUI(out nameChanged, out valuesChanged);
            EditorGUI.BeginChangeCheck();
            m_DefaultTexture = EditorGUILayout.ObjectField("Texture", m_DefaultTexture, typeof(Texture2D), false) as Texture2D;
            valuesChanged |= EditorGUI.EndChangeCheck();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Texture2D; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new TexturePropertyChunk(name, m_PropertyDescription, m_DefaultTexture,
                    m_DefaultTextureType, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("sampler2D " + name + ";", true);
            visitor.AddShaderChunk("float4 " + name + "_ST;", true);
        }

        public override string GenerateDefaultValue()
        {
            return name;
        }
    }
}

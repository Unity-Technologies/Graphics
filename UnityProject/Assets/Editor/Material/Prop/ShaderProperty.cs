using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	public enum PropertyType
	{
		Color,
		Texture2D,
		Float,
		Vector4
	}

	public abstract class ShaderProperty : ScriptableObject, IGenerateProperties
	{
		[SerializeField]
		protected string m_PropertyDescription;

		public virtual object value { get; set; }

		public virtual void PropertyOnGUI (out bool nameChanged, out bool valuesChanged)
		{
			EditorGUI.BeginChangeCheck ();
			name = EditorGUILayout.TextField("Name", name);
			nameChanged = EditorGUI.EndChangeCheck ();
			
			EditorGUI.BeginChangeCheck ();
			m_PropertyDescription = EditorGUILayout.TextField("Desc", m_PropertyDescription);
			valuesChanged = EditorGUI.EndChangeCheck ();
		}

		public abstract PropertyType propertyType { get; }
		public abstract void GeneratePropertyBlock (PropertyGenerator visitor, GenerationMode generationMode);
		public abstract void GeneratePropertyUsages (ShaderGenerator visitor, GenerationMode generationMode);
		public abstract string GenerateDefaultValue ();
	}

	public class TextureProperty : ShaderProperty
	{
		[SerializeField]
		private Texture2D m_DefaultTexture;

		[SerializeField]
		private TextureType m_DefaultTextureType;

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

		public override void PropertyOnGUI (out bool nameChanged, out bool valuesChanged)
		{
			base.PropertyOnGUI (out nameChanged, out valuesChanged);
			EditorGUI.BeginChangeCheck ();
			m_DefaultTexture = EditorGUILayout.ObjectField ("Texture", m_DefaultTexture, typeof(Texture2D), false) as Texture2D;
			valuesChanged |= EditorGUI.EndChangeCheck ();
		}

		public override PropertyType propertyType { get { return PropertyType.Texture2D; } }

		public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
		{
			visitor.AddShaderProperty(new TexturePropertyChunk(name, m_PropertyDescription, m_DefaultTexture, m_DefaultTextureType, false));
		}

		public override void GeneratePropertyUsages (ShaderGenerator visitor, GenerationMode generationMode)
		{
			visitor.AddShaderChunk("sampler2D " + name + ";", true);
			visitor.AddShaderChunk("float4 " + name + "_ST;", true); 
		}

		public override string GenerateDefaultValue ()
		{
			return name;
		}
	}

	public class ColorProperty : ShaderProperty
	{
		[SerializeField]
		private Color m_DefaultColor;

		public override object value
		{
			get { return m_DefaultColor; }
			set { m_DefaultColor = (Color)value; }
		}

		public Color defaultColor
		{
			get { return m_DefaultColor; }
			set { m_DefaultColor = value; }
		}

		public override void PropertyOnGUI (out bool nameChanged, out bool valuesChanged)
		{
			base.PropertyOnGUI (out nameChanged, out valuesChanged);
			EditorGUI.BeginChangeCheck ();
			m_DefaultColor = EditorGUILayout.ColorField("Color", m_DefaultColor);
			valuesChanged |= EditorGUI.EndChangeCheck ();
		}

		public override PropertyType propertyType { get { return PropertyType.Color; } }

		public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
		{
			visitor.AddShaderProperty(new ColorPropertyChunk(name, m_PropertyDescription, m_DefaultColor, false));
		}

		public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
		{
			visitor.AddShaderChunk("float4 " + name + ";", true); 
		}

		public override string GenerateDefaultValue ()
		{
			return "half4 (" + m_DefaultColor.r + "," + m_DefaultColor.g + "," + m_DefaultColor.b + "," + m_DefaultColor.a + ")";
		}
	}

	public class VectorProperty : ShaderProperty
	{
		[SerializeField]
		private Vector4 m_DefaultVector;

		public override object value
		{
			get { return m_DefaultVector; }
			set { m_DefaultVector = (Vector4)value; }
		}

		public Color defaultVector
		{
			get { return m_DefaultVector; }
			set { m_DefaultVector = value; }
		}

		public override void PropertyOnGUI (out bool nameChanged, out bool valuesChanged)
		{
			base.PropertyOnGUI (out nameChanged, out valuesChanged);
			EditorGUI.BeginChangeCheck ();
			m_DefaultVector = EditorGUILayout.Vector4Field("Vector", m_DefaultVector);
			valuesChanged |= EditorGUI.EndChangeCheck ();
		}

		public override PropertyType propertyType { get { return PropertyType.Vector4; } }

		public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
		{
			visitor.AddShaderProperty(new VectorPropertyChunk(name, m_PropertyDescription, m_DefaultVector, false));
		}

		public override void GeneratePropertyUsages (ShaderGenerator visitor, GenerationMode generationMode)
		{
			visitor.AddShaderChunk("float4 " + name + ";", true); 
		}

		public override string GenerateDefaultValue ()
		{
			return "half4 (" + m_DefaultVector.x + "," + m_DefaultVector.y + "," + m_DefaultVector.z + "," + m_DefaultVector.w + ")";
		}
	}
}

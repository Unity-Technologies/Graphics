using UnityEngine;

namespace UnityEditor.MaterialGraph
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

        public virtual void PropertyOnGUI(out bool nameChanged, out bool valuesChanged)
        {
            EditorGUI.BeginChangeCheck();
            name = EditorGUILayout.TextField("Name", name);
            nameChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            m_PropertyDescription = EditorGUILayout.TextField("Desc", m_PropertyDescription);
            valuesChanged = EditorGUI.EndChangeCheck();
        }

        public abstract PropertyType propertyType { get; }
        public abstract void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode);
        public abstract void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode);
        public abstract string GenerateDefaultValue();
    }
}

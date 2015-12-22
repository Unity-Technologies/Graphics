using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public enum PreviewState
    {
        Full = 0,
        Selected = 1,
        Off = 2
    }

    public class MaterialProperties : ScriptableObject, IGenerateGraphProperties
    {
        private Vector2 m_ScrollPos;

        [SerializeField]
        List<ShaderProperty> m_ShaderProperties = new List<ShaderProperty>();
        
        [SerializeField]
        private bool m_Expanded;
        
        public void DoGUI(List<Node> nodes)
        {
            m_Expanded = MaterialGraphStyles.Header("Properties", m_Expanded);

            if (!m_Expanded)
                return;

            var propsToRemove = new List<ShaderProperty>();
            foreach (var property in m_ShaderProperties)
            {
                // property changed
                bool nameChanged;
                bool valuesChanged;
                property.PropertyOnGUI(out nameChanged, out valuesChanged);
                if (nameChanged || valuesChanged)
                {
                    foreach (var node in nodes.Where(x => x is PropertyNode).Cast<PropertyNode>())
                        node.RefreshBoundProperty(property, nameChanged);
                }

                if (GUILayout.Button("Remove"))
                    propsToRemove.Add(property);
                EditorGUILayout.Separator();
            }

            foreach (var prop in propsToRemove)
            {
                foreach (var node in nodes.Where(x => x is PropertyNode).Cast<PropertyNode>())
                    node.UnbindProperty(prop);

                m_ShaderProperties.Remove(prop);
            }

            if (GUILayout.Button("Add"))
                AddProperty();
        }

        private void AddProperty()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Texture"), false, CreateProperty<TextureProperty>);
            menu.AddItem(new GUIContent("Color"), false, CreateProperty<ColorProperty>);
            menu.AddItem(new GUIContent("Vector"), false, CreateProperty<VectorProperty>);
            menu.ShowAsContext();
        }

        private void CreateProperty<T>() where T: ShaderProperty
        {
            var createdProperty = CreateInstance<T>();
            createdProperty.name = typeof(T).Name;
            createdProperty.hideFlags = HideFlags.HideInHierarchy;
            m_ShaderProperties.Add(createdProperty);
            AssetDatabase.AddObjectToAsset(createdProperty, this);
        }

        public IEnumerable<ShaderProperty> GetPropertiesForPropertyType(PropertyType propertyType)
        {
            return m_ShaderProperties.Where(x => x.propertyType == propertyType);
        }

        public void GenerateSharedProperties(PropertyGenerator shaderProperties, ShaderGenerator propertyUsages, GenerationMode generationMode)
        {
            foreach (var property in m_ShaderProperties)
            {
                property.GeneratePropertyBlock(shaderProperties, generationMode);
                property.GeneratePropertyUsages(propertyUsages, generationMode, ConcreteSlotValueType.Vector4);
            }
        }
    }
}

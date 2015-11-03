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
        public int previewState;

        //static GUIContent[] previewOptions;

        public event EventHandler OnChangePreviewState;

        /*static MaterialProperties ()
        {
            previewOptions = Enum.GetNames (typeof (PreviewState)).Select (x => new GUIContent (x)).ToArray ();
        }*/

        public void OnEnable()
        {
            if (OnChangePreviewState != null)
                OnChangePreviewState((PreviewState)previewState, null);
        }

        public void DoGUI(List<Node> nodes)
        {
            //m_ScrollPos1 = BeginArea ("Options", m_ScrollPos1);

            /*GUILayout.BeginHorizontal ();
            GUILayout.Label ("Preview", EditorStyles.largeLabel);
            EditorGUI.BeginChangeCheck ();
            previewState = EditorGUILayout.CycleButton (previewState, previewOptions, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck () && OnChangePreviewState != null)
                OnChangePreviewState ((PreviewState)previewState, null);
            GUILayout.EndHorizontal ();*/

            //EndArea ();

            m_ScrollPos = BeginArea("Exposed Properties", m_ScrollPos);

            if (GUILayout.Button("Add Property"))
                AddProperty();

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
                    {
                        node.RefreshBoundProperty(property, nameChanged);
                    }
                }
                if (GUILayout.Button("Remove"))
                {
                    propsToRemove.Add(property);
                }
                EditorGUILayout.Separator();
            }
            EndArea();

            foreach (var prop in propsToRemove)
            {
                foreach (var node in nodes.Where(x => x is PropertyNode).Cast<PropertyNode>())
                {
                    node.UnbindProperty(prop);
                }

                m_ShaderProperties.Remove(prop);
            }
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

        static Vector2 BeginArea(string title, Vector2 scrollPos)
        {
            DoHeader(title);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            return scrollPos;
        }

        static void EndArea()
        {
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        static void DoHeader(string title)
        {
            GUILayout.BeginHorizontal(/*EditorStyles.inspectorBig*/);
            GUILayout.BeginVertical();
            GUILayout.Label(title, EditorStyles.largeLabel);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
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

using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    [CreateAssetMenu(fileName = "EnvironmentLibrary", menuName = "LookDev/EnvironmentLibrary", order = 2000)]
    public class EnvironmentLibrary : ScriptableObject
    {
        [field: SerializeField]
        List<Environment> environments { get; set; } = new List<Environment>();

        public int Count => environments.Count;
        public Environment this[int index] => environments[index];

        public Environment Add()
        {
            Environment environment = ScriptableObject.CreateInstance<Environment>();
            Undo.RegisterCreatedObjectUndo(environment, "Add Environment");
            
            environments.Add(environment);

            // Store this new environment as a subasset so we can reference it safely afterwards.
            AssetDatabase.AddObjectToAsset(environment, this);
            
            // Force save / refresh. Important to do this last because SaveAssets can cause effect to become null!
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return environment;
        }

        public void Remove(int index)
        {
            Environment environment = environments[index];
            Undo.RecordObject(this, "Remove Environment");
            environments.RemoveAt(index);
            Undo.DestroyObjectImmediate(environment);

            // Force save / refresh
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }

    [CustomEditor(typeof(EnvironmentLibrary))]
    class EnvironmentLibraryEditor : Editor
    {
        class List<T, K> : VisualElement
            where T : VisualElement, IBendable<K>, new()
            where K : class
        {
            const string k_uss = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/Inspectors.uss";
            const string k_ListClass = "List";
            const string k_FirstContainerClass = "First";
            const string k_ContainerClass = "ListContainer";
            const string k_SelectedClass = "Selected";
            const string k_FooterClass = "Footer";

            class LineSelecter : Manipulator
            {
                List<T, K> m_Root;
                Container m_Container;

                public LineSelecter(List<T, K> root, Container container)
                {
                    m_Root = root;
                    m_Container = container;
                }

                protected override void RegisterCallbacksOnTarget()
                    => target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);

                protected override void UnregisterCallbacksFromTarget()
                    => target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);

                void OnMouseDown(MouseDownEvent e)
                    => m_Root.Select(m_Container);
            }
            class Container : VisualElement
            {
                T m_Content;

                public Container(List<T, K> root, T content)
                {
                    var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_uss);
                    if (sheet != null)
                        styleSheets.Add(sheet);

                    AddToClassList(k_ContainerClass);
                    this.AddManipulator(new LineSelecter(root, this));
                    
                    m_Content = content;
                    Add(m_Content);
                }
                
                public void Select() => AddToClassList(k_SelectedClass);

                public void Unselect()
                {
                    if (ClassListContains(k_SelectedClass))
                        RemoveFromClassList(k_SelectedClass);
                }

                public void Bind(K data) => m_Content.Bind(data);
            }

            VisualElement m_List;
            Container m_Selected;

            public List(Func<K> addCallback, Action<int> removeCallback)
            {
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_uss);
                if (sheet != null)
                    styleSheets.Add(sheet);
                
                Add(m_List = new VisualElement() { name = "Content" });

                VisualElement footer = new VisualElement();
                footer.AddToClassList(k_FooterClass);
                Add(footer);

                Button removeButton = new Button(() =>
                {
                    if (m_Selected == null)
                        return;
                    int index = m_List.IndexOf(m_Selected);
                    bool removeFirst = index == 0;
                    m_List.Remove(m_Selected);
                    m_Selected = null;
                    removeCallback?.Invoke(index);
                    if(removeFirst && m_List.childCount > 0)
                        m_List.ElementAt(0).AddToClassList(k_FirstContainerClass);
                })
                {
                    text = "-"
                };
                footer.Add(removeButton);

                Button addButton = new Button(() =>
                {
                    AddItem(new T());
                    Select(m_List.childCount - 1);
                    K newData = addCallback?.Invoke();
                    if (newData != null)
                        m_Selected.Bind(newData);
                })
                {
                    text = "+"
                };
                footer.Add(addButton);
            }

            void Select(Container container)
            {
                m_Selected?.Unselect();
                container?.Select();
                m_Selected = container;
            }
            public void Select(int containerIndex)
                => Select(m_List.ElementAt(containerIndex) as Container);

            public void AddItem(T item)
            {
                bool addFirst = m_List.childCount == 0;
                m_List.Add(new Container(this, item));
                if(addFirst)
                    m_List.ElementAt(0).AddToClassList(k_FirstContainerClass);
            }
        }

        VisualElement root;

        public sealed override VisualElement CreateInspectorGUI()
        {
            var library = target as EnvironmentLibrary;
            root = new VisualElement();

            var environments = new List<EnvironmentElement, Environment>(library.Add, library.Remove);
            for (int index = 0; index < library.Count; ++index)
                environments.AddItem(new EnvironmentElement(library[index]));
            root.Add(environments);

            return root;
        }

        // Don't use ImGUI
        public sealed override void OnInspectorGUI() { }
    }
}

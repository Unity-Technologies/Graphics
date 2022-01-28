using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace UnityEngine.Rendering
{
    public abstract class RenderingDebuggerPanel : ScriptableObject
    {
        protected static readonly string kHiddenClassName = "hidden";

        public abstract string panelName { get; }

        public abstract VisualElement CreatePanel();

        public abstract bool AreAnySettingsActive { get; }
        public abstract bool IsPostProcessingAllowed { get; }
        public abstract bool IsLightingActive { get; }

        public abstract bool TryGetScreenClearColor(ref Color color);
        private List<VisualElement> m_InstantiatedPanels = new List<VisualElement>();

        protected VisualElement CreateVisualElement(VisualTreeAsset panelAsset)
        {
            // Create the content of the tab
            var panel = panelAsset.Instantiate();
            m_InstantiatedPanels.Add(panel);
            return panel;
        }

        // Utility methods
        protected void RegisterCallback<T>(VisualElement panel, string fieldElementName, T currentValue, EventCallback<ChangeEvent<T>> onFieldValueChanged)
        {
            if (onFieldValueChanged == null)
                throw new ArgumentNullException(nameof(onFieldValueChanged));

            var fieldElement = panel.Q(fieldElementName);
            if (fieldElement == null)
                throw new InvalidOperationException($"Element {fieldElementName} not found");

            fieldElement.RegisterCallback(onFieldValueChanged);
            using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(default, currentValue))
            {
                onFieldValueChanged(evt);
            }
        }

        protected void SetElementHidden(string elementName, bool hidden)
        {
            foreach (var panel in m_InstantiatedPanels)
            {
                var element = panel.Q(elementName);
                if (element == null)
                    throw new InvalidOperationException($"Element {elementName} not found");

                if (hidden)
                    element.AddToClassList(kHiddenClassName);
                else
                    element.RemoveFromClassList(kHiddenClassName);
            }
        }

        public void Reset()
        {
            m_InstantiatedPanels.Clear();
        }

        public void BindTo(VisualElement targetElement)
        {
#if UNITY_EDITOR
            targetElement.Bind(new SerializedObject(this));
#else
            // TODO
#endif
        }

        public static List<Type> GetPanelTypes()
        {
#if UNITY_EDITOR
            return TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>().ToList();
#else
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> types = new ();
            foreach (var assembly in assemblies)
            {
                Type[] allAssemblyTypes;
                try
                {
                    allAssemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    allAssemblyTypes = e.Types;
                }

                var myTypes = allAssemblyTypes.Where(t =>!t.IsAbstract && typeof(RenderingDebuggerPanel).IsAssignableFrom(t));
                types.AddRange(myTypes);
            }

            return types;
#endif
        }
    }
}

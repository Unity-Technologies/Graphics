#if UNITY_EDITOR
using UnityEditor; // TODO fix this
#endif
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

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

        private List<VisualElement> m_InstantiatedPanels = new List<VisualElement>();

        protected VisualElement CreateVisualElement(string uiDocument)
        {
#if UNITY_EDITOR
            // TODO fix - needs editor assembly, use Resources.Load instead?
            var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiDocument);
            if (panelVisualTreeAsset == null)
                return null;

            // Create the content of the tab
            var panel = panelVisualTreeAsset.Instantiate();
            m_InstantiatedPanels.Add(panel);
            return panel;
#else
            throw new NotImplementedException();
#endif
        }

        // Utility methods

        protected void RegisterCallback<T>(VisualElement panel, string fieldElementName, EventCallback<ChangeEvent<T>> onFieldValueChanged)
        {
            var fieldElement = panel.Q(fieldElementName);
            if (fieldElement == null)
                throw new InvalidOperationException($"Element {fieldElementName} not found");
            fieldElement.RegisterCallback(onFieldValueChanged);
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
    }
}

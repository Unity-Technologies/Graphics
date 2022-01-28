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
    }
}

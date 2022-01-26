#if UNITY_EDITOR
using UnityEditor; // TODO fix this
#endif
using System;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    public abstract class RenderingDebuggerPanel : ScriptableObject
    {
        protected static readonly string kHiddenClassName = "hidden";

        public abstract string panelName { get; }
        public abstract VisualElement CreatePanel();

        private VisualElement m_PanelElement;
        public VisualElement panelElement
        {
            get
            {
                if (m_PanelElement == null)
                    m_PanelElement = CreatePanel();

#if UNITY_EDITOR
                // TODO fix - needs editor assembly, use Resources.Load instead?
                var panelStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/Styles/RenderingDebuggerPanelStyle.uss");
                m_PanelElement.styleSheets.Add(panelStyle);
#endif

                return m_PanelElement;
            }
        }

        protected VisualElement CreateVisualElement(string uiDocument)
        {
#if UNITY_EDITOR
            // TODO fix - needs editor assembly, use Resources.Load instead?
            var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiDocument);
            if (panelVisualTreeAsset == null)
                return null;

            // Create the content of the tab
            return panelVisualTreeAsset.Instantiate();
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

        protected void SetElementHidden(VisualElement element, bool hidden)
        {
            if (hidden)
                element.AddToClassList(kHiddenClassName);
            else
                element.RemoveFromClassList(kHiddenClassName);
        }
    }
}

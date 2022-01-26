#if UNITY_EDITOR
using UnityEditor; // TODO fix this
#endif
using System;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    public abstract class RenderingDebuggerPanel : ScriptableObject
    {
        public abstract string panelName { get; }
        public abstract VisualElement CreatePanel();

        private VisualElement m_PanelElement;
        public VisualElement panelElement
        {
            get
            {
                if (m_PanelElement == null)
                    m_PanelElement = CreatePanel();
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
    }
}

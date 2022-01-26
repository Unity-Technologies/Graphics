using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    public abstract class RenderingDebuggerPanel : ScriptableObject
    {
        public abstract string panelName { get; }
        public VisualElement panelElement
        {
            get
            {
                if (m_RootElement == null)
                    m_RootElement = CreatePanel();
                return m_RootElement;
            }
        }

        private VisualElement m_RootElement;

        protected VisualElement CreateVisualElement(string uiDocument)
        {
            var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiDocument);
            if (panelVisualTreeAsset == null)
                return null;

            // Create the content of the tab
            VisualElement visualElement = new VisualElement();

            // TODO use Instantiate
            panelVisualTreeAsset.CloneTree(visualElement);

            return visualElement;
        }

        public abstract VisualElement CreatePanel();
    }
}

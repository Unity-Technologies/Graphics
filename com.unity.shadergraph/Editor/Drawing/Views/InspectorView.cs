using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing
{
    // TODO: Temporary Inspector
    // TODO: Replace this with Sai's work
    class InspectorView : VisualElement
    {
        GraphData m_GraphData;
        PreviewManager m_PreviewManager;
        VisualElement m_Element;

        // Track enabled states of foldouts
        Dictionary<ITargetImplementation, bool> m_ImplementationFoldouts;

        public InspectorView(GraphData graphData, PreviewManager previewManager)
        {
            name = "inspectorView";
            m_GraphData = graphData;
            m_PreviewManager = previewManager;
            m_ImplementationFoldouts = new Dictionary<ITargetImplementation, bool>();

            // Styles
            style.width = 270;
            style.height = 400;
            style.position = Position.Absolute;
            style.right = 0;
            style.top = 0;
            style.backgroundColor = new Color(.17f, .17f, .17f, 1);
            style.flexDirection = FlexDirection.Column;

            Rebuild();
        }

        void Rebuild()
        {
            m_Element = new VisualElement();

            m_Element.Add(m_GraphData.GetSettings(() =>
                {
                    OnChange();
                }));
            
            Add(m_Element);
        }

        void OnChange()
        {
            m_GraphData.UpdateActiveBlocks();
            m_PreviewManager.UpdateMasterPreview(ModificationScope.Topological);
            Remove(m_Element);
            Rebuild();
        }
    }
}

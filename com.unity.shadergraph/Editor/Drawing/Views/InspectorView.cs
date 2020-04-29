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

        public InspectorView(GraphData graphData, PreviewManager previewManager)
        {
            name = "inspectorView";
            m_GraphData = graphData;
            m_PreviewManager = previewManager;

            // Styles
            style.width = 400;
            style.height = 800;
            style.position = Position.Absolute;
            style.right = 0;
            style.top = 0;
            style.backgroundColor = new Color(.17f, .17f, .17f, 1);
            style.flexDirection = FlexDirection.Column;

            Rebuild();

            // TODO: Temporary hack to repaint inspector on Undo
            // TODO: Make sure this is handled properly by InspectorView then removed
            graphData.onUndoPerformed += Rebuild;
        }

        void Rebuild()
        {
            if(m_Element != null)
            {
                Remove(m_Element);
            }
            
            m_Element = new VisualElement();
            m_Element.Add(m_GraphData.GetSettings(() => { OnChange(); }, (s) => { RegisterUndo(s); } ));
            Add(m_Element);
        }

        void OnChange()
        {
            var activeBlocks = m_GraphData.GetActiveBlocksForAllActiveTargets();
            if(ShaderGraphPreferences.autoAddRemoveBlocks)
            {
                m_GraphData.AddRemoveBlocksFromActiveList(activeBlocks);
            }
            
            m_GraphData.UpdateActiveBlocks(activeBlocks);
            m_PreviewManager.UpdateMasterPreview(ModificationScope.Topological);
            Rebuild();
        }

        void RegisterUndo(string identifier)
        {
            m_GraphData.owner.RegisterCompleteObjectUndo(identifier);
        }
    }
}

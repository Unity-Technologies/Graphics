using System;
using Debug = UnityEngine.Debug;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        // TODO: Move somewhere useful (StaticPortNodeFieldsInspector)
        private const string COLOR_HINT = ".UseColor";
        private const string SLIDER_HINT = ".UseSlider";

        NodePreviewPart m_NodePreviewPart;
        public NodePreviewPart NodePreview => m_NodePreviewPart;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            var shouldShowPreview = m_GraphDataNodeModel.existsInGraphData;

            if (shouldShowPreview)
                m_NodePreviewPart = new NodePreviewPart("node-preview", GraphElementModel, this, ussClassName);

            PartList.AppendPart(m_NodePreviewPart);

            if (Model is not GraphDataNodeModel graphDataNodeModel) return;
            PartList.InsertPartAfter(portContainerPartName, new StaticPortNodeFieldsInspector("sg-static-fields", graphDataNodeModel, this, ussClassName, inspectorOnly: false));
        }

        GraphDataNodeModel m_GraphDataNodeModel => NodeModel as GraphDataNodeModel;

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();

            // TODO: (Sai) Re-enable in Sprint 2
            // Currently commented out as we don't require preview expansion/collapse
            //evt.menu.AppendAction("Preview/Expand", action =>
            //{
            //    GraphView.Dispatch(new ChangePreviewExpandedCommand(true, new [] {m_GraphDataNodeModel}));
            //});
            //
            //evt.menu.AppendAction("Preview/Collapse", action =>
            //{
            //    GraphView.Dispatch(new ChangePreviewExpandedCommand(false, new [] {m_GraphDataNodeModel}));
            //});

            evt.menu.AppendAction("Copy Shader", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
            });

            evt.menu.AppendAction("Show Generated Shader", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
                // TODO (Brett) Get the shader code from the PreviewManager once it is implemented.
                // https://jira.unity3d.com/browse/GSG-780
            });

            evt.menu.AppendAction("Show Preview Code", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
                // TODO (Brett) Get the shader code from the PreviewManager once it is implemented.
                // https://jira.unity3d.com/browse/GSG-780
            });

            evt.menu.AppendAction("Disconnect All", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
            });

            base.BuildContextualMenu(evt);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEditor.MaterialGraph;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    [GUISkinStyle("window")]
    public class MaterialGraphNode : GraphElement
    {
        VisualContainer m_SlotContainer;
        VisualContainer m_ControlsContainer;
        VisualContainer m_PreviewContainer;
        private List<NodePreviewData> m_currentPreviewData;

        public MaterialGraphNode()
        {
            content = new GUIContent("");

            m_SlotContainer = new VisualContainer
            {
                name = "slots", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };

            m_ControlsContainer = new VisualContainer
            {
                name = "controls", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };

            m_PreviewContainer = new VisualContainer
            {
                name = "preview", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };

            m_currentPreviewData = new List<NodePreviewData>();
        }

        public override void DoRepaint(PaintContext painter)
        {
            base.DoRepaint(painter);
            if (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected)
            {
                painter.DrawRectangleOutline(transform, position, Color.yellow);
            }
        }

        private void AddSlots(MaterialNodeData nodeData)
        {
            m_SlotContainer.ClearChildren();

            if (!nodeData.elements.OfType<NodeAnchorData>().Any())
                return;

            var inputs = new VisualContainer
            {
                name = "input", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            m_SlotContainer.AddChild(inputs);

            // put a spacer here?
            //m_SlotContainer.AddChild(new f);

            var outputs = new VisualContainer
            {
                name = "output", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            m_SlotContainer.AddChild(outputs);

            content = new GUIContent(nodeData.name);
            foreach (var anchor in nodeData.elements.OfType<NodeAnchorData>())
            {
                if (anchor.direction == Direction.Input)
                    inputs.AddChild(new NodeAnchor(anchor));
                else
                    outputs.AddChild(new NodeAnchor(anchor));
            }

            AddChild(m_SlotContainer);
        }

        private void AddControls(MaterialNodeData nodeData)
        {
            m_ControlsContainer.ClearChildren();

            if (!nodeData.elements.OfType<NodeControlData>().Any())
                return;

            foreach (var controlData in nodeData.elements.OfType<NodeControlData>())
            {
                var imContainer = new IMGUIContainer()
                {
                    name = "element",
                    OnGUIHandler = controlData.OnGUIHandler,
                    pickingMode = PickingMode.Position
                };
                m_ControlsContainer.AddChild(imContainer);
            }

            AddChild(m_ControlsContainer);
        }

        private void AddPreview(MaterialNodeData nodeData)
        {
            

            if (!nodeData.elements.OfType<NodePreviewData>().Any())
                return;
            
            var previews = nodeData.elements.OfType<NodePreviewData>().ToList();
            var isSamePreviews = m_currentPreviewData.Count == previews.Count;

            if (isSamePreviews)
            {
                for (int i = 0; i < previews.Count; i++)
                {
                    if (!ReferenceEquals(previews[i], m_currentPreviewData[i]))
                    {
                        isSamePreviews = false;
                        break;
                    }
                }
            }

            if (isSamePreviews)
            {
                for (int i = 0; i < previews.Count; i++)
                {
                    var preview = previews[i];
                    var thePreview = m_PreviewContainer.GetChildAtIndex(i) as Image;
                    // TODO: Consider null exception
                    // TODO: Need to share the texture
                    // right now it's allocating all the time. 
                    thePreview.image = preview.Render(new Vector2(200, 200));
                }
            }
            else
            {
                m_PreviewContainer.ClearChildren();
                m_currentPreviewData.Clear();

                foreach (var preview in previews)
                {
                    var image = preview.Render(new Vector2(200, 200));
                    var thePreview = new Image
                    {
                        image = image,
                        name = "image"
                    };
                    m_PreviewContainer.AddChild(thePreview);
                    m_currentPreviewData.Add(preview);
                }
            }
            
            AddChild(m_PreviewContainer);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            ClearChildren();

            m_ControlsContainer.ClearChildren();
            // m_PreviewContainer.ClearChildren();

            var nodeData = dataProvider as MaterialNodeData;

            if (nodeData == null)
                return;

            AddSlots(nodeData);
            AddControls(nodeData);
            AddPreview(nodeData);

            /*positionType = PositionType.Absolute;
            positionLeft = nodeData.node.drawState.position.x;
            positionTop = nodeData.node.drawState.position.y;*/
        }
    }
}

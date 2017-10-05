using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialNodeView : Node
    {
        VisualElement m_ControlsContainer;
        List<GraphControlPresenter> m_CurrentControls;
        VisualElement m_PreviewToggle;
        VisualElement m_ResizeHandle;
        Image m_PreviewImage;
        bool m_IsScheduled;

        public MaterialNodeView()
        {
            CreateContainers();

            AddToClassList("MaterialNode");
        }

        void CreateContainers()
        {
            m_ControlsContainer = new VisualElement
            {
                name = "controls"
            };
            leftContainer.Add(m_ControlsContainer);
            m_CurrentControls = new List<GraphControlPresenter>();

            m_PreviewToggle = new VisualElement { name = "toggle", text = "" };
            m_PreviewToggle.AddManipulator(new Clickable(OnPreviewToggle));
            leftContainer.Add(m_PreviewToggle);

            m_PreviewImage = new Image
            {
                name = "preview",
                pickingMode = PickingMode.Ignore,
                image = Texture2D.whiteTexture
            };

            leftContainer.Add(m_PreviewImage);


            m_ResizeHandle = new VisualElement() { name = "resize", text = "" };
            m_ResizeHandle.AddManipulator(new Draggable(OnResize));
            Add(m_ResizeHandle);
        }

        void OnResize(Vector2 deltaSize)
        {
            style.width = layout.width + deltaSize.x;
            style.height = layout.height + deltaSize.y;
        }

        void OnPreviewToggle()
        {
            var node = GetPresenter<MaterialNodePresenter>().node;
            node.previewExpanded = !node.previewExpanded;
            m_PreviewToggle.text = node.previewExpanded ? "▲" : "▼";
        }

        void UpdatePreviewTexture(Texture previewTexture)
        {
            if (previewTexture == null)
            {
                m_PreviewImage.visible = false;
                m_PreviewImage.RemoveFromClassList("visible");
                m_PreviewImage.AddToClassList("hidden");
                m_PreviewImage.image = Texture2D.whiteTexture;
            }
            else
            {
                m_PreviewImage.visible = true;
                m_PreviewImage.AddToClassList("visible");
                m_PreviewImage.RemoveFromClassList("hidden");
                m_PreviewImage.image = previewTexture;
            }
            Dirty(ChangeType.Repaint);

        }

        void UpdateControls(MaterialNodePresenter nodeData)
        {
            if (nodeData.controls.SequenceEqual(m_CurrentControls) && nodeData.expanded)
                return;

            m_ControlsContainer.Clear();
            m_CurrentControls.Clear();
            Dirty(ChangeType.Layout);

            if (!nodeData.expanded)
                return;

            foreach (var controlData in nodeData.controls)
            {
                m_ControlsContainer.Add(new IMGUIContainer(controlData.OnGUIHandler)
                {
                    name = "element"
                });
                m_CurrentControls.Add(controlData);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            var nodePresenter = GetPresenter<MaterialNodePresenter>();
            if (nodePresenter != null)
                nodePresenter.position = newPos;
            base.SetPosition(newPos);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var nodePresenter = GetPresenter<MaterialNodePresenter>();

            if (nodePresenter == null)
            {
                m_ControlsContainer.Clear();
                m_CurrentControls.Clear();
                UpdatePreviewTexture(null);
                return;
            }

            m_PreviewToggle.text = nodePresenter.node.previewExpanded ? "▲" : "▼";
            if (nodePresenter.node.hasPreview)
                m_PreviewToggle.RemoveFromClassList("inactive");
            else
                m_PreviewToggle.AddToClassList("inactive");

            UpdateControls(nodePresenter);

            UpdatePreviewTexture(nodePresenter.node.previewExpanded ? nodePresenter.previewTexture : null);
        }
    }
}

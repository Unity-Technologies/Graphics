using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialNodeView : Node
    {
        VisualElement m_ControlsContainer;
        List<VisualElement> m_ControlViews;
        Guid m_NodeGuid;
        VisualElement m_PreviewToggle;
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
            m_ControlViews = new List<VisualElement>();

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

        void UpdateControls(MaterialNodePresenter nodePresenter)
        {
            if (!nodePresenter.node.guid.Equals(m_NodeGuid))
            {
                m_ControlViews.Clear();
                foreach (var propertyInfo in nodePresenter.node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
                        m_ControlViews.Add(attribute.InstantiateControl(nodePresenter.node, propertyInfo));
                }
            }

            if (!nodePresenter.expanded)
            {
                m_ControlsContainer.Clear();
            }
            else if (m_ControlsContainer.childCount != m_ControlViews.Count)
            {
                m_ControlsContainer.Clear();
                foreach (var view in m_ControlViews)
                    m_ControlsContainer.Add(view);
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
                m_ControlViews.Clear();
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

            m_NodeGuid = nodePresenter.node.guid;
        }
    }
}

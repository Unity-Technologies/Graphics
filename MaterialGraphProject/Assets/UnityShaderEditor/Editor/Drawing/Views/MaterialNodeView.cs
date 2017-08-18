using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialNodeView : Node
    {
        VisualElement m_ControlsContainer;
        List<GraphControlPresenter> m_CurrentControls;
        Image m_PreviewImage;
        NodePreviewPresenter m_CurrentPreview;
        bool m_IsScheduled;

        public MaterialNodeView()
        {
            CreateContainers();

            AddToClassList("MaterialNode");

            onEnter += SchedulePolling;
            onLeave += UnschedulePolling;
        }

        void CreateContainers()
        {
            m_ControlsContainer = new VisualElement
            {
                name = "controls"
            };
            leftContainer.Add(m_ControlsContainer);
            m_CurrentControls = new List<GraphControlPresenter>();

            m_PreviewImage = new Image
            {
                name = "preview",
                pickingMode = PickingMode.Ignore,
                image = Texture2D.whiteTexture
            };
            m_PreviewImage.AddToClassList("inactive");
            leftContainer.Add(m_PreviewImage);
        }

        void SchedulePolling()
        {
            if (panel != null)
            {
                if (!m_IsScheduled)
                {
                    this.Schedule(InvalidateUIIfNeedsTime).StartingIn(0).Every(16);
                    m_IsScheduled = true;
                }
            }
            else
            {
                m_IsScheduled = false;
            }
        }

        void UnschedulePolling()
        {
            if (m_IsScheduled && panel != null)
            {
                this.Unschedule(InvalidateUIIfNeedsTime);
            }
            m_IsScheduled = false;
        }

        void InvalidateUIIfNeedsTime(TimerState timerState)
        {
            var node = GetPresenter<MaterialNodePresenter>();
            if (node.requiresTime)
            {
                node.OnModified(ModificationScope.Node);
                UpdatePreviewTexture(m_CurrentPreview);
            }
        }

        void UpdatePreviewTexture(NodePreviewPresenter preview)
        {
            var texture = preview != null ? preview.Render(new Vector2(256, 256)) : null;
            if (texture == null)
            {
                m_PreviewImage.AddToClassList("inactive");
                m_PreviewImage.image = Texture2D.whiteTexture;
            }
            else
            {
                m_PreviewImage.RemoveFromClassList("inactive");
                m_PreviewImage.image = texture;
            }
            Dirty(ChangeType.Repaint);
        }

        void UpdateControls(MaterialNodePresenter nodeData)
        {
            if (nodeData.controls.SequenceEqual(m_CurrentControls) && nodeData.expanded)
                return;

            m_ControlsContainer.Clear();
            m_CurrentControls.Clear();

            if (!nodeData.expanded)
                return;

            foreach (var controlData in nodeData.controls)
            {
                m_ControlsContainer.Add(new IMGUIContainer(controlData.OnGUIHandler)
                {
                    name = "element",
                    executionContext = controlData.GetInstanceID(),
                });
                m_CurrentControls.Add(controlData);
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var node = GetPresenter<MaterialNodePresenter>();

            if (node == null)
            {
                m_ControlsContainer.Clear();
                m_CurrentControls.Clear();
                m_PreviewImage.AddToClassList("inactive");
                m_CurrentPreview = null;
                UpdatePreviewTexture(m_CurrentPreview);
                return;
            }

            UpdateControls(node);

            m_CurrentPreview = node.preview;
            UpdatePreviewTexture(m_CurrentPreview);

            if (node.expanded)
                m_PreviewImage.RemoveFromClassList("hidden");
            else
                m_PreviewImage.AddToClassList("hidden");
        }
    }
}

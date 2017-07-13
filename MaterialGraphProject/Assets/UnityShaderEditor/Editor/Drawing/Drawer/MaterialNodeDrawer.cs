using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing.Util;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialNodeDrawer : NodeDrawer
    {
        Image m_PreviewImage;
        NodePreviewPresenter m_currentPreviewData;
        bool m_IsScheduled;

        public MaterialNodeDrawer()
        {
            CreateContainers();

            AddToClassList("MaterialNode");

            onEnter += SchedulePolling;
            onLeave += UnschedulePolling;
        }

        private void CreateContainers()
        {
            m_PreviewImage = new Image
            {
                name = "preview", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
                image = Texture2D.whiteTexture
            };
            m_PreviewImage.AddToClassList("inactive");
            leftContainer.AddChild(m_PreviewImage);
        }

        private void SchedulePolling()
        {
            Debug.LogFormat("SchedulePolling");
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

        private void UnschedulePolling()
        {
            Debug.LogFormat("UnschedulePolling");
            if (m_IsScheduled && panel != null)
            {
                this.Unschedule(InvalidateUIIfNeedsTime);
            }
            m_IsScheduled = false;
        }

        private void InvalidateUIIfNeedsTime(TimerState timerState)
        {
            var data = GetPresenter<MaterialNodePresenter>();
            var childrenNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childrenNodes, data.node);
            if (childrenNodes.OfType<IRequiresTime>().Any())
            {
                data.OnModified(ModificationScope.Node);
                var texture = m_currentPreviewData != null ? m_currentPreviewData.Render(new Vector2(256, 256)) : null;
                m_PreviewImage.image = texture ?? Texture2D.whiteTexture;
                m_PreviewImage.RemoveFromClassList("inactive");
            }
            ListPool<INode>.Release(childrenNodes);
        }

        private void AddPreview(MaterialNodePresenter nodeData)
        {
            if (!nodeData.elements.OfType<NodePreviewPresenter>().Any())
                return;

            var preview = nodeData.elements.OfType<NodePreviewPresenter>().FirstOrDefault();
            var texture = preview != null ? preview.Render(new Vector2(256, 256)) : null;

            if (texture == null)
            {
                m_PreviewImage.AddToClassList("inactive");
                m_PreviewImage.image = Texture2D.whiteTexture;
            }
            else
            {
                m_PreviewImage.RemoveFromClassList("inactive");
                m_PreviewImage.image = preview.Render(new Vector2(256, 256));
            }

            m_currentPreviewData = preview;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = GetPresenter<MaterialNodePresenter>();
            if (nodeData == null)
            {
                m_PreviewImage.AddToClassList("inactive");
                m_currentPreviewData = null;
                return;
            }

            AddPreview(nodeData);

            if (nodeData.expanded)
            {
                //m_PreviewImage.paintFlags &= ~PaintFlags.Invisible;
                m_PreviewImage.RemoveFromClassList("hidden");
            }
            else
            {
                //m_PreviewImage.paintFlags |= PaintFlags.Invisible;
                m_PreviewImage.AddToClassList("hidden");
            }
        }
    }
}

using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

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
            leftContainer.Add(m_PreviewImage);
        }

        private void SchedulePolling()
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

        private void UnschedulePolling()
        {
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
                UpdatePreviewTexture(m_currentPreviewData);
                Dirty(ChangeType.Repaint);
            }
            ListPool<INode>.Release(childrenNodes);
        }

        void UpdatePreviewTexture(NodePreviewPresenter previewPresenter)
        {
            var texture = previewPresenter != null ? previewPresenter.Render(new Vector2(256, 256)) : null;
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
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = GetPresenter<MaterialNodePresenter>();
            if (nodeData == null)
            {
                m_PreviewImage.AddToClassList("inactive");
                m_currentPreviewData = null;
                UpdatePreviewTexture(m_currentPreviewData);
                return;
            }

            m_currentPreviewData = nodeData.elements.OfType<NodePreviewPresenter>().FirstOrDefault();
            UpdatePreviewTexture(m_currentPreviewData);

            if (nodeData.expanded)
                m_PreviewImage.RemoveFromClassList("hidden");
            else
                m_PreviewImage.AddToClassList("hidden");
        }

    }
}

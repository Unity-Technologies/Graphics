using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.RMGUI;
using UnityEditor.Graphing.Util;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialNodeDrawer : NodeDrawer
    {
        VisualContainer m_PreviewContainer;
        private List<NodePreviewPresenter> m_currentPreviewData;
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
            m_PreviewContainer = new VisualContainer
            {
                name = "preview", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            leftContainer.AddChild(m_PreviewContainer);

            m_currentPreviewData = new List<NodePreviewPresenter>();
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
            }
            ListPool<INode>.Release(childrenNodes);
        }

        private void AddPreview(MaterialNodePresenter nodeData)
        {
            if (!nodeData.elements.OfType<NodePreviewPresenter>().Any())
                return;

            var previews = nodeData.elements.OfType<NodePreviewPresenter>().ToList();

            if (!previews.ItemsReferenceEquals(m_currentPreviewData))
            {
                m_PreviewContainer.ClearChildren();
                m_currentPreviewData = previews;

                foreach (var preview in previews)
                {
                    var thePreview = new NodePreviewDrawer
                    {
                        data = preview,
                        name = "image"
                    };
                    m_PreviewContainer.AddChild(thePreview);
                }
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = GetPresenter<MaterialNodePresenter>();
            if (nodeData == null)
            {
                m_PreviewContainer.ClearChildren();
                m_currentPreviewData.Clear();
                return;
            }

            AddPreview(nodeData);

            if (nodeData.expanded)
            {
                m_PreviewContainer.paintFlags &= ~PaintFlags.Invisible;
                m_PreviewContainer.RemoveFromClassList("hidden");
            }
            else
            {
                m_PreviewContainer.paintFlags |= PaintFlags.Invisible;
                m_PreviewContainer.AddToClassList("hidden");
            }
        }
    }
}

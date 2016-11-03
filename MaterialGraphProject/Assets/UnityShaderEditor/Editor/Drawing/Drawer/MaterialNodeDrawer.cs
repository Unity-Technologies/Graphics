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
        private List<NodePreviewDrawData> m_currentPreviewData;
        bool m_IsScheduled;

        public MaterialNodeDrawer()
        {
            CreateContainers();

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

            m_currentPreviewData = new List<NodePreviewDrawData>();
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
            var data = GetData<MaterialNodeDrawData>();
            var childrenNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childrenNodes, data.node);
            if (childrenNodes.OfType<IRequiresTime>().Any())
            {
                data.OnModified(ModificationScope.Node);
            }
            ListPool<INode>.Release(childrenNodes);
        }

        private void AddPreview(MaterialNodeDrawData nodeData)
        {
            if (!nodeData.elements.OfType<NodePreviewDrawData>().Any())
                return;

            var previews = nodeData.elements.OfType<NodePreviewDrawData>().ToList();

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

            AddChild(m_PreviewContainer);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = dataProvider as MaterialNodeDrawData;
            if (nodeData == null)
            {
                CreateContainers();
                return;
            }

            AddPreview(nodeData);
        }
    }
}

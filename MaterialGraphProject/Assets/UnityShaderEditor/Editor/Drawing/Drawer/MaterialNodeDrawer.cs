using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.RMGUI;

namespace UnityEditor.MaterialGraph.Drawing
{
    [GUISkinStyle("window")]
    public class MaterialNodeDrawer : NodeDrawer
    {
        VisualContainer m_PreviewContainer;
        private List<NodePreviewDrawData> m_currentPreviewData;
        bool m_IsScheduled;

        public MaterialNodeDrawer()
        {
            m_PreviewContainer = new VisualContainer
            {
                name = "preview", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };

            m_currentPreviewData = new List<NodePreviewDrawData>();

            onEnter += SchedulePolling;
            onLeave += UnschedulePolling;
        }

        private void SchedulePolling()
        {
            if (panel != null)
            {
                if (!m_IsScheduled)
                {
                    this.Schedule(InvalidateUIIfNeedsTime).StartingIn(0).Every(30);
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
            var childrenNodes = ListPool<INode>.Get();
            var data = GetData<MaterialNodeDrawData>();
            NodeUtils.DepthFirstCollectNodesFromNode(childrenNodes, data.node);
            if (childrenNodes.OfType<IRequiresTime>().Any())
                data.MarkDirtyHack();
            ListPool<INode>.Release(childrenNodes);
        }

        private void AddPreview(MaterialNodeDrawData nodeData)
        {
            if (!nodeData.elements.OfType<NodePreviewDrawData>().Any())
                return;

            var previews = nodeData.elements.OfType<NodePreviewDrawData>().ToList();
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

            var nodeData = dataProvider as MaterialNodeDrawData;
            if (nodeData == null)
                return;

            AddPreview(nodeData);
        }
    }
}

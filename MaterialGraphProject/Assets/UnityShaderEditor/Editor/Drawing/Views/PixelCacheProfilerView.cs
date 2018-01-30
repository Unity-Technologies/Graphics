using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PixelCacheProfilerView : VisualElement
    {
        readonly VisualElement m_Target;
        Label m_TotalLabel;
        Label m_DirtyLabel;
        Label m_TotalNodeContentsLabel;
        Label m_DirtyNodeContentsLabel;
        Label m_TotalPreviewsLabel;
        Label m_DirtyPreviewsLabel;
        Label m_TotalInlinesLabel;
        Label m_DirtyInlinesLabel;

        public PixelCacheProfilerView(VisualElement target)
        {
            m_Target = target;

            var tpl = Resources.Load<VisualTreeAsset>("UXML/PixelCacheProfiler");
            tpl.CloneTree(this, null);

            m_TotalLabel = this.Q<Label>("totalLabel");
            m_DirtyLabel = this.Q<Label>("dirtyLabel");
            m_TotalNodeContentsLabel = this.Q<Label>("totalNodeContentsLabel");
            m_DirtyNodeContentsLabel = this.Q<Label>("dirtyNodeContentsLabel");
            m_TotalPreviewsLabel = this.Q<Label>("totalPreviewsLabel");
            m_DirtyPreviewsLabel = this.Q<Label>("dirtyPreviewsLabel");
            m_TotalInlinesLabel = this.Q<Label>("totalInlinesLabel");
            m_DirtyInlinesLabel = this.Q<Label>("dirtyInlinesLabel");
        }

        public void Profile()
        {
            var caches = m_Target.Query().Where(ve => ve.clippingOptions == ClippingOptions.ClipAndCacheContents).Build().ToList();
            var dirtyCaches = caches.Where(ve => ve.IsDirty(ChangeType.Repaint)).ToList();
            m_TotalLabel.text = caches.Count.ToString();
            m_DirtyLabel.text = dirtyCaches.Count.ToString();

            var nodeContentsCaches = caches.Where(ve => ve.name == "node-border").ToList();
            var dirtyNodeContentsCaches = nodeContentsCaches.Where(ve => ve.IsDirty(ChangeType.Repaint)).ToList();
            m_TotalNodeContentsLabel.text = nodeContentsCaches.Count.ToString();
            m_DirtyNodeContentsLabel.text = dirtyNodeContentsCaches.Count.ToString();

            var previewCaches = caches.Where(ve => ve.name == "previewContainer").ToList();
            var dirtyPreviewCaches = previewCaches.Where(ve => ve.IsDirty(ChangeType.Repaint)).ToList();
            m_TotalPreviewsLabel.text = previewCaches.Count.ToString();
            m_DirtyPreviewsLabel.text = dirtyPreviewCaches.Count.ToString();

            var inlineCaches = caches.Where(ve => ve.name == "portInputContainer").ToList();
            var dirtyInlineCaches = inlineCaches.Where(ve => ve.IsDirty(ChangeType.Repaint)).ToList();
            m_TotalInlinesLabel.text = inlineCaches.Count.ToString();
            m_DirtyInlinesLabel.text = dirtyInlineCaches.Count.ToString();
        }
    }
}

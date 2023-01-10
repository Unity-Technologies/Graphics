using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for panels.
    /// </summary>
    [CoreRPHelpURL("Rendering-Debugger")]
    public class DebugUIHandlerPanel : MonoBehaviour
    {
        /// <summary>Name of the panel.</summary>
        public Text nameLabel;
        /// <summary>Scroll rect of the panel.</summary>
        public ScrollRect scrollRect;
        /// <summary>Viewport of the panel.</summary>
        public RectTransform viewport;
        /// <summary>Associated canvas.</summary>
        public DebugUIHandlerCanvas Canvas;

        RectTransform m_ScrollTransform;
        RectTransform m_ContentTransform;
        RectTransform m_MaskTransform;
        DebugUIHandlerWidget m_ScrollTarget;

        internal protected DebugUI.Panel m_Panel;

        void OnEnable()
        {
            m_ScrollTransform = scrollRect.GetComponent<RectTransform>();
            m_ContentTransform = GetComponent<DebugUIHandlerContainer>().contentHolder;
            m_MaskTransform = GetComponentInChildren<Mask>(true).rectTransform;
        }

        internal void SetPanel(DebugUI.Panel panel)
        {
            m_Panel = panel;
            nameLabel.text = panel.displayName;
        }

        internal DebugUI.Panel GetPanel()
        {
            return m_Panel;
        }

        /// <summary>
        /// Select next panel on the canvas.
        /// </summary>
        public void SelectNextItem()
        {
            Canvas.SelectNextPanel();
        }

        /// <summary>
        /// Select previous panel on the canvas.
        /// </summary>
        public void SelectPreviousItem()
        {
            Canvas.SelectPreviousPanel();
        }

        /// <summary>
        /// Scrollbar value clicked via mouse/touch.
        /// </summary>
        public void OnScrollbarClicked()
        {
            DebugManager.instance.SetScrollTarget(null); // Release scroll target
        }

        internal void SetScrollTarget(DebugUIHandlerWidget target)
        {
            m_ScrollTarget = target;
        }

        // TODO: Jumps around with foldouts and the likes, fix me
        internal void UpdateScroll()
        {
            if (m_ScrollTarget == null)
                return;

            var targetTransform = m_ScrollTarget.GetComponent<RectTransform>();

            float itemY = GetYPosInScroll(targetTransform);
            float targetY = GetYPosInScroll(m_MaskTransform);
            float normalizedDiffY = (targetY - itemY) / (m_ContentTransform.rect.size.y - m_ScrollTransform.rect.size.y);
            float normalizedPosY = scrollRect.verticalNormalizedPosition - normalizedDiffY;
            normalizedPosY = Mathf.Clamp01(normalizedPosY);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, normalizedPosY, Time.deltaTime * 10f);
        }

        float GetYPosInScroll(RectTransform target)
        {
            var pivotOffset = new Vector3(
                (0.5f - target.pivot.x) * target.rect.size.x,
                (0.5f - target.pivot.y) * target.rect.size.y,
                0f
            );
            var localPos = target.localPosition + pivotOffset;
            var worldPos = target.parent.TransformPoint(localPos);
            return m_ScrollTransform.TransformPoint(worldPos).y;
        }

        internal DebugUIHandlerWidget GetFirstItem()
        {
            return GetComponent<DebugUIHandlerContainer>()
                .GetFirstItem();
        }

        /// <summary>
        /// Function to reset DebugManager, provided for UI.
        /// </summary>
        public void ResetDebugManager()
        {
            DebugManager.instance.Reset();
        }
    }
}

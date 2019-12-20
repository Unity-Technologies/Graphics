using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>Foldout in the DebugMenu</summary>
    [ExecuteAlways]
    public class UIFoldout : Toggle
    {
        /// <summary>Contents inside the toggle</summary>
        public GameObject content;
        /// <summary>Arror in state opened</summary>
        public GameObject arrowOpened;
        /// <summary>Arror in state closed</summary>
        public GameObject arrowClosed;

        /// <summary>Start of this GameObject lifecicle</summary>
        protected override void Start()
        {
            base.Start();
            onValueChanged.AddListener(SetState);
            SetState(isOn);
        }

#pragma warning disable 108,114
        void OnValidate()
        {
            SetState(isOn, false);
        }

#pragma warning restore 108,114

        /// <summary>Change the state of this foldout</summary>
        /// <param name="state">The new State</param>
        public void SetState(bool state)
        {
            SetState(state, true);
        }

        /// <summary>Change the state of this foldout</summary>
        /// <param name="state">The new State</param>
        /// <param name="rebuildLayout">If True, the layout will be rebuild</param>
        public void SetState(bool state, bool rebuildLayout)
        {
            if (arrowOpened == null || arrowClosed == null || content == null)
                return;

            if (arrowOpened.activeSelf != state)
                arrowOpened.SetActive(state);

            if (arrowClosed.activeSelf == state)
                arrowClosed.SetActive(!state);

            if (content.activeSelf != state)
                content.SetActive(state);

            if (rebuildLayout)
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }
}

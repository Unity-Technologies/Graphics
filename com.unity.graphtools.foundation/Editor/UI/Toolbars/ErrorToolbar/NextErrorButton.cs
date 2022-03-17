#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar element to navigate to the next error in the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class NextErrorButton : ErrorToolbarButton
    {
        public const string id = "GTF/Error/Next";

        /// <summary>
        /// Initializes a new instance of the <see cref="NextErrorButton"/> class.
        /// </summary>
        public NextErrorButton()
        {
            name = "NextError";
            tooltip = L10n.Tr("Next Error");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/ErrorToolbar/NextError.png");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var state = GraphView?.GraphViewModel.GraphViewState;
            if (state != null)
                FrameAndSelectError(state.ErrorIndex + 1);
        }
    }
}
#endif

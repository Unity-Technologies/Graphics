#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar element to navigate to the previous error in the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class PreviousErrorButton : ErrorToolbarButton
    {
        public const string id = "GTF/Error/Previous";

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousErrorButton"/> class.
        /// </summary>
        public PreviousErrorButton()
        {
            name = "PreviousError";
            tooltip = L10n.Tr("Previous Error");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/ErrorToolbar/PreviousError.png");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var state = GraphView?.GraphViewModel.GraphViewState;
            if (state != null)
                FrameAndSelectError(state.ErrorIndex - 1);
        }
    }
}
#endif

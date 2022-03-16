#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class MathBookSaveAllButton : SaveAllButton
    {
        public new const string id = "MathBook/Main/Save All";

        public MathBookSaveAllButton()
        {
            tooltip = "Save All and Reload Assets";
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            base.OnClick();
            AssetDatabase.Refresh();
        }
    }
}
#endif

#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class MathBookSaveButton : SaveButton
    {
        public new const string id = "MathBook/Main/Save";

        public MathBookSaveButton()
        {
            tooltip = "Save and Reload Assets";
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

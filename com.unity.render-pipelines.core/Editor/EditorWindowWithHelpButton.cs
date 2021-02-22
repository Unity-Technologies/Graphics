using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>Editor window that adds a button to browse the help url, specify <see cref="HelpURLAttribute"/> when defining your inherited class</summary>
    public class EditorWindowWithHelpButton : EditorWindow
    {
        /// <summary>Shows a button with help icon and opens the url defined by <see cref="HelpURLAttribute"/></summary>
        /// <param name="r">The rect to show the button</param>
        protected virtual void ShowButton(Rect r)
        {
            if (GUI.Button(r, CoreEditorStyles.iconHelp, CoreEditorStyles.iconHelpStyle))
                Help.ShowHelpForObject(this);
        }
    }
}

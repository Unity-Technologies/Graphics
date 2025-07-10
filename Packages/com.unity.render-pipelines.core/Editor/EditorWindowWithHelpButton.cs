using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>Editor window that adds a button to browse the help url, specify <see cref="HelpURLAttribute"/> when defining your inherited class</summary>
    public class EditorWindowWithHelpButton : EditorWindow
    {
        static Lazy<GUIContent> m_IconHelpGUIContent;
        GUIContent iconHelpGUIContent => m_IconHelpGUIContent.Value;

        static EditorWindowWithHelpButton()
        {
            m_IconHelpGUIContent = new Lazy<GUIContent>(() => new GUIContent(CoreEditorStyles.iconHelp));
        }

        /// <summary>Shows a button with help icon and opens the url defined by <see cref="HelpURLAttribute"/></summary>
        /// <param name="r">The rect to show the button</param>
        [Obsolete("This method will be removed soon. Please override OnHelpButtonClicked instead. #from(2023.1)")]
        protected virtual void ShowButton(Rect r)
        {
            if (GUI.Button(r, iconHelpGUIContent, CoreEditorStyles.iconHelpStyle))
                OnHelpButtonClicked();
        }

        /// <summary>What hapens when the help button is clicked onto. Default implementation use the <see cref="HelpURLAttribute"/> onto the window class.</summary>
        protected virtual void OnHelpButtonClicked()
        {
            Help.ShowHelpForObject(this);
        }
    }
}

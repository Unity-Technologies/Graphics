using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Create a toggleable header for material UI, must be used within a scope.
    /// </summary>
    /// <example>
    /// <code>
    /// void OnGUI()
    /// {
    ///     using (var header = new MaterialHeaderScope(text, ExpandBit, editor))
    ///     {
    ///         if (header.expanded)
    ///             EditorGUILayout.LabelField("Hello World !");
    ///     }
    /// }
    /// </code>
    /// </example>
    public struct MaterialHeaderScope : IDisposable
    {
        /// <summary>Indicates whether the header is expanded or not. Is true if the header is expanded, false otherwise.</summary>
        public readonly bool expanded;
        bool spaceAtEnd;
#if !UNITY_2020_1_OR_NEWER
        int oldIndentLevel;
#endif

        /// <summary>
        /// Creates a material header scope to display the foldout in the material UI.
        /// </summary>
        /// <param name="title">GUI Content of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        /// <param name="defaultExpandedState">The default state if the header is not present</param>
        /// <param name="documentationURL">[optional] Documentation page</param>
        public MaterialHeaderScope(GUIContent title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, bool subHeader = false, uint defaultExpandedState = uint.MaxValue, string documentationURL = "")
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            bool beforeExpanded = materialEditor.IsAreaExpanded(bitExpanded, defaultExpandedState);

#if !UNITY_2020_1_OR_NEWER
            oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = subHeader ? 1 : 0; //fix for preset in 2019.3 (preset are one more indentation depth in material)
#endif

            this.spaceAtEnd = spaceAtEnd;
            if (!subHeader)
                CoreEditorUtils.DrawSplitter();
            GUILayout.BeginVertical();

            bool saveChangeState = GUI.changed;
            expanded = subHeader
                ? CoreEditorUtils.DrawSubHeaderFoldout(title, beforeExpanded, isBoxed: false)
                : CoreEditorUtils.DrawHeaderFoldout(title, beforeExpanded, documentationURL: documentationURL);
            if (expanded ^ beforeExpanded)
            {
                materialEditor.SetIsAreaExpanded((uint)bitExpanded, expanded);
                saveChangeState = true;
            }
            GUI.changed = saveChangeState;
        }

        /// <summary>
        /// Creates a material header scope to display the foldout in the material UI.
        /// </summary>
        /// <param name="title">Title of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        public MaterialHeaderScope(string title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, bool subHeader = false)
            : this(EditorGUIUtility.TrTextContent(title, string.Empty), bitExpanded, materialEditor, spaceAtEnd, subHeader)
        {
        }

        /// <summary>Disposes of the material scope header and cleans up any resources it used.</summary>
        void IDisposable.Dispose()
        {
            if (expanded && spaceAtEnd && (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
                EditorGUILayout.Space();

#if !UNITY_2020_1_OR_NEWER
            EditorGUI.indentLevel = oldIndentLevel;
#endif
            GUILayout.EndVertical();
        }
    }
}

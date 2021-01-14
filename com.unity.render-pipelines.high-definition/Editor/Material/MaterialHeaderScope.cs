using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Create a toggleable header for material UI, must be used within a scope.
    /// <example>Example:
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
    /// </summary>
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
        /// <param name="title">Title of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="colorDot">Specify a color to display a dot, like in the layered UI.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        public MaterialHeaderScope(string title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, Color colorDot = default(Color), bool subHeader = false)
        {
            bool beforeExpended = materialEditor.GetExpandedAreas(bitExpanded);

#if !UNITY_2020_1_OR_NEWER
            oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = subHeader ? 1 : 0; //fix for preset in 2019.3 (preset are one more indentation depth in material)
#endif

            this.spaceAtEnd = spaceAtEnd;
            if (!subHeader)
                CoreEditorUtils.DrawSplitter();
            GUILayout.BeginVertical();

            bool saveChangeState = GUI.changed;
            if (colorDot != default(Color))
                title = "   " + title;
            expanded = subHeader
                ? CoreEditorUtils.DrawSubHeaderFoldout(title, beforeExpended)
                : CoreEditorUtils.DrawHeaderFoldout(title, beforeExpended);
            if (colorDot != default(Color))
            {
                Rect dotRect = GUILayoutUtility.GetLastRect();
                dotRect.width = 5;
                dotRect.height = 5;
                dotRect.y += 7;
                dotRect.x += 17;
                EditorGUI.DrawRect(dotRect, colorDot);
            }
            if (expanded ^ beforeExpended)
            {
                materialEditor.SetExpandedAreas((uint)bitExpanded, expanded);
                saveChangeState = true;
            }
            GUI.changed = saveChangeState;

            if (expanded)
                ++EditorGUI.indentLevel;
        }

        /// <summary>Disposes of the material scope header and cleans up any resources it used.</summary>
        void IDisposable.Dispose()
        {
            if (expanded)
            {
                if (spaceAtEnd && (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
                    EditorGUILayout.Space();
                --EditorGUI.indentLevel;
            }

#if !UNITY_2020_1_OR_NEWER
            EditorGUI.indentLevel = oldIndentLevel;
#endif
            GUILayout.EndVertical();
        }
    }
}

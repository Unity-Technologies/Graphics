using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Rendering;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.HDPipeline
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
    internal struct MaterialHeaderScope : IDisposable
    {
        public readonly bool expanded;
        private bool spaceAtEnd;

        public MaterialHeaderScope(string title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, Color colorDot = default(Color), bool subHeader = false)
        {
            bool beforeExpended = materialEditor.GetExpandedAreas(bitExpanded);

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
                Color previousColor = GUI.contentColor;
                GUI.contentColor = colorDot;
                Rect headerRect = GUILayoutUtility.GetLastRect();
                headerRect.xMin += 16f;
                EditorGUI.LabelField(headerRect, "■");
                GUI.contentColor = previousColor;
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

        void IDisposable.Dispose()
        {
            if (expanded)
            {
                if (spaceAtEnd && (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
                    EditorGUILayout.Space();
                --EditorGUI.indentLevel;
            }
            GUILayout.EndVertical();
        }
    }
}

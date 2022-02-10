using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    [CustomEditor(typeof(SearcherExampleComponent))]
    class SearcherExampleComponentEditor : Editor
    {
        Rect m_ButtonRect;

        public override void OnInspectorGUI()
        {
            var root = new SearcherItem("Root");
            var children = new List<SearcherItem>();
            for (var i = 0; i < 10; ++i)
                children.Add(new SearcherItem("B-" + i));

            var child = new SearcherItem("Child", "", children);
            root.AddChild(child);

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Mouse Position");
                if (EditorGUILayout.DropdownButton(new GUIContent("Button"), FocusType.Passive, GUI.skin.button))
                {
                    var editorWindow = EditorWindow.focusedWindow;
                    var localMousePosition = Event.current.mousePosition;
                    var worldMousePosition = editorWindow.rootVisualElement.LocalToWorld(localMousePosition);

                    SearcherWindow.Show(
                        editorWindow,
                        new List<SearcherItem> { root },
                        "Mouse Position",
                        item =>
                        {
                            Debug.Log("Searcher item selected: " + (item?.Name ?? "<none>"));
                            return true;
                        },
                        worldMousePosition);
                }
                EditorGUILayout.EndHorizontal();
            }

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Button Center");

                var selected = EditorGUILayout.DropdownButton(new GUIContent("Button"), FocusType.Passive, GUI.skin.button);
                if (Event.current.type == EventType.Repaint)
                    m_ButtonRect = GUILayoutUtility.GetLastRect();

                if (selected)
                {
                    var editorWindow = EditorWindow.focusedWindow;
                    var localButtonCenter = m_ButtonRect.center;
                    var worldButtonCenter = editorWindow.rootVisualElement.LocalToWorld(localButtonCenter);

                    SearcherWindow.Show(
                        editorWindow,
                        new List<SearcherItem> { root },
                        "Button Center",
                        item =>
                        {
                            Debug.Log("Searcher item selected: " + (item?.Name ?? "<none>"));
                            return true;
                        },
                        worldButtonCenter);
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}

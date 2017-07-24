using System;
using System.Linq;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    /*class CustomCodeControlPresenter : GraphControlPresenter
    {
        public class CodeEditorPopup : PopupWindowContent
        {
            private CustomCodeNode m_customCodeNode = null;
            private Vector2 m_scroll;

            private bool m_isOpen = false;
            public bool IsOpen
            {
                get { return m_isOpen; }
            }

            public CodeEditorPopup(CustomCodeNode customCodeNode)
            {
                m_customCodeNode = customCodeNode;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(300, 300);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Label("Custom Code Editor", EditorStyles.boldLabel);
                m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
                m_customCodeNode.Code = EditorGUILayout.TextArea(m_customCodeNode.Code, GUILayout.Height(GetWindowSize().y - 30));
                EditorGUILayout.EndScrollView();
            }

            public override void OnOpen()
            {
                Debug.Log("Popup opened: " + this);
                m_isOpen = true;
            }

            public override void OnClose()
            {
                Debug.Log("Popup closed: " + this);
                if (m_isOpen)
                {
                    m_customCodeNode.UpdateInputAndOuputSlots();
                }
                m_isOpen = false;
            }
        }

        private CodeEditorPopup codeEditorPopup = null;
        Rect buttonRect;
        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as UnityEngine.MaterialGraph.CustomCodeNode;
            if (tNode == null)
                return;

            if (codeEditorPopup == null)
            {
                codeEditorPopup = new CodeEditorPopup(tNode);
                if (string.IsNullOrEmpty(tNode.Code))
                {
                    tNode.Code = "//Write your function below.\r\nvoid test(float a, float b, out float c)\r\n{\r\n\tc = a + b;\r\n}\r\n";
                }
            }

            string buttonText = codeEditorPopup.IsOpen ? "Close Code Editor" : "Open Code Editor";
            if (GUILayout.Button(buttonText))
            {
                PopupWindow.Show(buttonRect, codeEditorPopup);
            }
            if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class CustomCodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<CustomCodeControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }*/
}

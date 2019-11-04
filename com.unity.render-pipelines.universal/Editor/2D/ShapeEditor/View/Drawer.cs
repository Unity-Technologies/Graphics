using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal class DefaultStyles
    {
        public readonly GUIStyle pointNormalStyle;
        public readonly GUIStyle pointHoveredStyle;
        public readonly GUIStyle pointSelectedStyle;
        public readonly GUIStyle pointPreviewStyle;
        public readonly GUIStyle pointRemovePreviewStyle;
        public readonly GUIStyle tangentNormalStyle;
        public readonly GUIStyle tangentHoveredStyle;
        public readonly GUIStyle selectionRectStyle;

        public DefaultStyles()
        {
            var pointNormal = Resources.Load<Texture2D>("Path/pointNormal");
            var pointHovered = Resources.Load<Texture2D>("Path/pointHovered");
            var pointSelected = Resources.Load<Texture2D>("Path/pointSelected");
            var pointPreview = Resources.Load<Texture2D>("Path/pointPreview");
            var pointRemovePreview = Resources.Load<Texture2D>("Path/pointRemovePreview");
            var tangentNormal = Resources.Load<Texture2D>("Path/tangentNormal");

            pointNormalStyle = CreateStyle(Resources.Load<Texture2D>("Path/pointNormal"), Vector2.one * 12f);
            pointHoveredStyle = CreateStyle(Resources.Load<Texture2D>("Path/pointHovered"), Vector2.one * 12f);
            pointSelectedStyle = CreateStyle(Resources.Load<Texture2D>("Path/pointSelected"), Vector2.one * 12f);
            pointPreviewStyle = CreateStyle(Resources.Load<Texture2D>("Path/pointPreview"), Vector2.one * 12f);
            pointRemovePreviewStyle = CreateStyle(Resources.Load<Texture2D>("Path/pointRemovePreview"), Vector2.one * 12f);
            tangentNormalStyle = CreateStyle(Resources.Load<Texture2D>("Path/tangentNormal"), Vector2.one * 8f);
            tangentHoveredStyle = CreateStyle(Resources.Load<Texture2D>("Path/pointHovered"), Vector2.one * 10f);

            selectionRectStyle = GUI.skin.FindStyle("selectionRect");
        }

        private GUIStyle CreateStyle(Texture2D texture, Vector2 size)
        {
            var guiStyle = new GUIStyle();
            guiStyle.normal.background = texture;
            guiStyle.fixedWidth = size.x;
            guiStyle.fixedHeight = size.y;

            return guiStyle;
        }
    }

    internal class Drawer : IDrawer
    {
        private IGUIState m_GUIState = new GUIState();
        private DefaultStyles m_Styles;
        protected DefaultStyles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new DefaultStyles();

                return m_Styles;
            }
        }

        public void DrawSelectionRect(Rect rect)
        {
            Handles.BeginGUI();
            styles.selectionRectStyle.Draw(rect, GUIContent.none, false, false, false, false);
            Handles.EndGUI();
        }

        public void DrawCreatePointPreview(Vector3 position)
        {
            DrawGUIStyleCap(0, position, Quaternion.identity, m_GUIState.GetHandleSize(position), styles.pointPreviewStyle);
        }

        public void DrawRemovePointPreview(Vector3 position)
        {
            DrawGUIStyleCap(0, position, Quaternion.identity, m_GUIState.GetHandleSize(position), styles.pointRemovePreviewStyle);
        }

        public void DrawPoint(Vector3 position)
        {
            DrawGUIStyleCap(0, position, Quaternion.identity, m_GUIState.GetHandleSize(position), styles.pointNormalStyle);
        }

        public void DrawPointHovered(Vector3 position)
        {
            DrawGUIStyleCap(0, position, Quaternion.identity, m_GUIState.GetHandleSize(position), styles.pointHoveredStyle);
        }

        public void DrawPointSelected(Vector3 position)
        {
            DrawGUIStyleCap(0, position, Quaternion.identity, m_GUIState.GetHandleSize(position), styles.pointSelectedStyle);
        }

        public void DrawLine(Vector3 p1, Vector3 p2, float width, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(width, new Vector3[] { p1, p2 });
        }

        public void DrawDottedLine(Vector3 p1, Vector3 p2, float width, Color color)
        {
            Handles.color = color;
            Handles.DrawDottedLine(p1, p2, width);
        }

        public void DrawBezier(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float width, Color color)
        {
            Handles.color = color;
            Handles.DrawBezier(p1, p4, p2, p3, color, null, width);
        }

        public void DrawTangent(Vector3 position, Vector3 tangent)
        {
            DrawLine(position, tangent, 3f, Color.yellow);
            DrawGUIStyleCap(0, tangent, Quaternion.identity, m_GUIState.GetHandleSize(tangent), styles.tangentNormalStyle);
        }

        
        private void DrawGUIStyleCap(int controlID, Vector3 position, Quaternion rotation, float size, GUIStyle guiStyle)
        {
            if (Camera.current && Vector3.Dot(position - Camera.current.transform.position, Camera.current.transform.forward) < 0f)
                return;

            Handles.BeginGUI();
            guiStyle.Draw(GetGUIStyleRect(guiStyle, position), GUIContent.none, controlID);
            Handles.EndGUI();
        }

        private Rect GetGUIStyleRect(GUIStyle style, Vector3 position)
        {
            Vector2 vector = HandleUtility.WorldToGUIPoint(position);

            float fixedWidth = style.fixedWidth;
            float fixedHeight = style.fixedHeight;

            return new Rect(vector.x - fixedWidth / 2f, vector.y - fixedHeight / 2f, fixedWidth, fixedHeight);
        }
    }
}

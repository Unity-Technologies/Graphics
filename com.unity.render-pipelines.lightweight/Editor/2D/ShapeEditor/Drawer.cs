using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GUIFramework;

namespace Unity.Path2D
{
    public class DefaultStyles
    {
        public readonly GUIStyle pointNormalStyle;
        public readonly GUIStyle pointHoveredStyle;
        public readonly GUIStyle pointSelectedStyle;
        public readonly GUIStyle pointPreviewStyle;
        public readonly GUIStyle pointRemovePreviewStyle;
        public readonly GUIStyle selectionRectStyle;

        public DefaultStyles()
        {
            var pointNormal = Resources.Load<Texture2D>("pointNormal");
            var pointHovered = Resources.Load<Texture2D>("pointHovered");
            var pointSelected = Resources.Load<Texture2D>("pointSelected");
            var pointPreview = Resources.Load<Texture2D>("pointPreview");
            var pointRemovePreview = Resources.Load<Texture2D>("pointRemovePreview");

            pointNormalStyle = CreateStyle(pointNormal, Vector2.one * 12f);
            pointHoveredStyle = CreateStyle(pointHovered, Vector2.one * 12f);
            pointSelectedStyle = CreateStyle(pointSelected, Vector2.one * 12f);
            pointPreviewStyle = CreateStyle(pointPreview, Vector2.one * 12f);
            pointRemovePreviewStyle = CreateStyle(pointRemovePreview, Vector2.one * 12f);

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

    public class Drawer
    {
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
            ShapeEditorUtility.DrawGUIStyleCap(0, position, Quaternion.identity, ShapeEditorUtility.GetHandleSize(position), styles.pointPreviewStyle);
        }

        public void DrawRemovePointPreview(Vector3 position)
        {
            ShapeEditorUtility.DrawGUIStyleCap(0, position, Quaternion.identity, ShapeEditorUtility.GetHandleSize(position), styles.pointRemovePreviewStyle);
        }

        public void DrawPoint(Vector3 position)
        {
            ShapeEditorUtility.DrawGUIStyleCap(0, position, Quaternion.identity, ShapeEditorUtility.GetHandleSize(position), styles.pointNormalStyle);
        }

        public void DrawPointHovered(Vector3 position)
        {
            ShapeEditorUtility.DrawGUIStyleCap(0, position, Quaternion.identity, ShapeEditorUtility.GetHandleSize(position), styles.pointHoveredStyle);
        }

        public void DrawPointSelected(Vector3 position)
        {
            ShapeEditorUtility.DrawGUIStyleCap(0, position, Quaternion.identity, ShapeEditorUtility.GetHandleSize(position), styles.pointSelectedStyle);
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
    }
}

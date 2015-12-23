using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraphStyles
    {
        private static MaterialGraphStyles s_Styles;

        private const float kHeadingSpace = 22.0f;
        private readonly GUIStyle m_Header = "ShurikenModuleTitle";
        
        private MaterialGraphStyles()
        {
            m_Header.font = (new GUIStyle("Label")).font;
            m_Header.border = new RectOffset(15, 7, 4, 4);
            m_Header.fixedHeight = kHeadingSpace;
            m_Header.contentOffset = new Vector2(20f, -2f);
        }

        private static MaterialGraphStyles styles
        {
            get { return s_Styles ?? (s_Styles = new MaterialGraphStyles()); }
        }
        
        public static bool DoDrawDefaultInspector(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.Update();

            // Loop through properties and create one field (including children) for each top level property.
            SerializedProperty property = obj.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                EditorGUI.BeginDisabledGroup("m_Script" == property.propertyPath);
                EditorGUILayout.PropertyField(property, true);
                EditorGUI.EndDisabledGroup();
                expanded = false;
            }

            obj.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        public static bool Header(string title, bool display)
        {
            GUILayout.Box(title, styles.m_Header);

            var rect = GUILayoutUtility.GetLastRect();

            Rect toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (Event.current.type == EventType.Repaint)
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }
            return display;
        }
    }
}

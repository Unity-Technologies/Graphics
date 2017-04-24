using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public class DebugItemDrawer
    {
        protected DebugMenuItem m_MenuItem = null;

        public DebugItemDrawer()
        {
        }

        public void SetDebugItem(DebugMenuItem item)
        {
            m_MenuItem = item;
        }

        public virtual void ClampValues(Func<object> getter, Action<object> setter) {}
        public virtual void BuildGUI() {}

#if UNITY_EDITOR
        void DrawBoolItem()
        {
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.Toggle(m_MenuItem.name, (bool)m_MenuItem.GetValue());
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
            }
        }

        void DrawFloatItem()
        {
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.FloatField(m_MenuItem.name, (float)m_MenuItem.GetValue());
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
            }
        }

        public virtual void OnEditorGUI()
        {
            if (m_MenuItem.GetItemType() == typeof(bool))
            {
                DrawBoolItem();
            }
            else if (m_MenuItem.GetItemType() == typeof(float))
            {
                DrawFloatItem();
            }
        }
#endif
}

    public class DebugItemDrawFloatMinMax
        : DebugItemDrawer
    {
        float m_Min = 0.0f;
        float m_Max = 1.0f;
        public DebugItemDrawFloatMinMax(float min, float max)
        {
            m_Min = min;
            m_Max = max;
        }

        public override void ClampValues(Func<object> getter, Action<object> setter)
        {
            if (m_MenuItem == null)
                return;

            if(m_MenuItem.GetItemType() == typeof(float))
            {
                setter(Mathf.Clamp((float)getter(), m_Min, m_Max));
            }
        }

#if UNITY_EDITOR
        public override void OnEditorGUI()
        {
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(m_MenuItem.name, (float)m_MenuItem.GetValue(), m_Min, m_Max);
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
            }
        }
#endif
    }
}
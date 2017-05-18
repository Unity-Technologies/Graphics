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

        // Label for simple GUI items
        protected GUIContent m_Label;
        protected List<GUIContent> m_EnumStrings = null;
        protected List<int> m_EnumValues = null;

        public DebugItemDrawer()
        {
        }

        public void SetDebugItem(DebugMenuItem item)
        {
            m_MenuItem = item;

            m_Label = new GUIContent(m_MenuItem.name);
            Type itemType = m_MenuItem.GetItemType();
            if(itemType.BaseType == typeof(System.Enum))
            {
                Array arr = Enum.GetValues(itemType);
                m_EnumStrings = new List<GUIContent>(arr.Length);
                m_EnumValues = new List<int>(arr.Length);

                foreach(var value in arr)
                {
                    m_EnumStrings.Add(new GUIContent(value.ToString()));
                    m_EnumValues.Add((int)value);
                }
            }
        }

        public virtual void ClampValues(Func<object> getter, Action<object> setter) {}
        public virtual DebugMenuItemUI BuildGUI(GameObject parent, DebugMenuItem menuItem)
        {
            DebugMenuItemUI newItemUI = null;
            if (menuItem.GetItemType() == typeof(bool))
            {
                newItemUI = new DebugMenuBoolItemUI(parent, menuItem, m_Label.text);
            }
            else if (menuItem.GetItemType() == typeof(int))
            {
                newItemUI = new DebugMenuIntItemUI(parent, menuItem, m_Label.text);
            }
            else if (menuItem.GetItemType() == typeof(uint))
            {
                newItemUI = new DebugMenuUIntItemUI(parent, menuItem, m_Label.text);
            }
            else if (menuItem.GetItemType() == typeof(float))
            {
                newItemUI = new DebugMenuFloatItemUI(parent, menuItem, m_Label.text);
            }
            else if (menuItem.GetItemType() == typeof(Color))
            {
                newItemUI = new DebugMenuColorItemUI(parent, menuItem, m_Label.text);
            }
            else if (m_MenuItem.GetItemType().BaseType == typeof(System.Enum))
            {
                newItemUI = new DebugMenuEnumItemUI(parent, menuItem, m_Label.text, m_EnumStrings.ToArray(), m_EnumValues.ToArray());
            }

            return newItemUI;
        }

#if UNITY_EDITOR
        bool DrawBoolItem()
        {
            bool value = (bool)m_MenuItem.GetValue();
            if (m_MenuItem.readOnly)
            {
                EditorGUILayout.LabelField(m_Label, new GUIContent(value.ToString()));
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.Toggle(m_Label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    m_MenuItem.SetValue(value);
                    return true;
                }
            }

            return false;
        }

        bool DrawIntItem()
        {
            int value = (int)m_MenuItem.GetValue();
            if (m_MenuItem.readOnly)
            {
                EditorGUILayout.LabelField(m_Label, new GUIContent(value.ToString()));
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.IntField(m_Label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    m_MenuItem.SetValue(value);
                    return true;
                }
            }

            return false;
        }

        bool DrawUIntItem()
        {
            int value = (int)(uint)m_MenuItem.GetValue();
            if (m_MenuItem.readOnly)
            {
                EditorGUILayout.LabelField(m_Label, new GUIContent(value.ToString()));
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.IntField(m_Label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    value = System.Math.Max(0, value);
                    m_MenuItem.SetValue((uint)value);
                    return true;
                }
            }

            return false;
        }

        bool DrawFloatItem()
        {
            float value = (float)m_MenuItem.GetValue();
            if(m_MenuItem.readOnly)
            {
                EditorGUILayout.LabelField(m_Label, new GUIContent(value.ToString()));
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.FloatField(m_Label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    m_MenuItem.SetValue(value);
                    return true;
                }
            }

            return false;
        }

        bool DrawColorItem()
        {
            EditorGUI.BeginChangeCheck();
            Color value = EditorGUILayout.ColorField(m_Label, (Color)m_MenuItem.GetValue());
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawEnumItem()
        {
            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.IntPopup(m_Label, (int)m_MenuItem.GetValue(), m_EnumStrings.ToArray(), m_EnumValues.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
            }

            return false;
        }

        public virtual bool OnEditorGUI()
        {
            if (m_MenuItem.GetItemType() == typeof(bool))
            {
                return DrawBoolItem();
            }
            else if (m_MenuItem.GetItemType() == typeof(int))
            {
                return DrawIntItem();
            }
            else if(m_MenuItem.GetItemType() == typeof(uint))
            {
                return DrawUIntItem();
            }
            else if (m_MenuItem.GetItemType() == typeof(float))
            {
                return DrawFloatItem();
            }
            else if (m_MenuItem.GetItemType() == typeof(Color))
            {
                return DrawColorItem();
            }
            else if (m_MenuItem.GetItemType().BaseType == typeof(System.Enum))
            {
                return DrawEnumItem();
            }

            return false;
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
        public override bool OnEditorGUI()
        {
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(m_MenuItem.name, (float)m_MenuItem.GetValue(), m_Min, m_Max);
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
            }

            return false;
        }
#endif
    }

    public class DebugItemDrawerIntEnum
        : DebugItemDrawer
    {
        GUIContent[]    m_EnumStrings = null;
        int[]           m_EnumValues = null;

        public DebugItemDrawerIntEnum(GUIContent[] enumStrings, int[] enumValues)
        {
            m_EnumStrings = enumStrings;
            m_EnumValues = enumValues;
        }

        public override DebugMenuItemUI BuildGUI(GameObject parent, DebugMenuItem menuItem)
        {
            return new DebugMenuEnumItemUI(parent, menuItem, m_Label.text, m_EnumStrings, m_EnumValues);
        }

#if UNITY_EDITOR
        public override bool OnEditorGUI()
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            int value = UnityEditor.EditorGUILayout.IntPopup(m_Label, (int)m_MenuItem.GetValue(), m_EnumStrings, m_EnumValues);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
            }

            return false;
        }
#endif
    }

}

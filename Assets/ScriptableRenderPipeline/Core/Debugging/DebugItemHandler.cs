using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public abstract class DebugItemHandler
    {
        protected DebugMenuItem m_MenuItem = null;

        public void SetDebugMenuItem(DebugMenuItem item)
        {
            m_MenuItem = item;
        }

        // Method user needs to override for specific value clamping.
        public virtual void ClampValues(Func<object> getter, Action<object> setter) {}
        // Method that will create UI items for runtime debug menu.
        public abstract DebugMenuItemUI BuildGUI(GameObject parent);
        // Method users need to override for editor specific UI
        public abstract bool OnEditorGUI();
        // Method users need to override for specific serialization of custom types.
        public abstract DebugMenuItemState CreateDebugMenuItemState();
    }

    // This is the default debug item handler that handles all basic types.
    public class DefaultDebugItemHandler
        : DebugItemHandler
    {
        bool m_IsInitialized = false;

        // Label for simple GUI items
        protected GUIContent m_Label;

        // Values and strings for Enum items
        protected List<GUIContent>  m_EnumStrings = null;
        protected List<int>         m_EnumValues = null;

        public void Initialize()
        {
            if (m_IsInitialized)
                return;

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

        public override DebugMenuItemState CreateDebugMenuItemState()
        {
            DebugMenuItemState newItemState = null;
            if (m_MenuItem.GetItemType() == typeof(bool))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateBool>();
            }
            else if (m_MenuItem.GetItemType() == typeof(int))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateInt>();
            }
            else if (m_MenuItem.GetItemType() == typeof(uint))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateUInt>();
            }
            else if (m_MenuItem.GetItemType() == typeof(float))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateFloat>();
            }
            else if (m_MenuItem.GetItemType() == typeof(Color))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateColor>();
            }
            else if (m_MenuItem.GetItemType().BaseType == typeof(System.Enum))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateInt>(); // Need to be serialized as int. For some reason serialization of the Enum directly just fails...
            }

            return newItemState;
        }

        public override DebugMenuItemUI BuildGUI(GameObject parent)
        {
            Initialize();

            DebugMenuItemUI newItemUI = null;
            if (m_MenuItem.GetItemType() == typeof(bool))
            {
                newItemUI = new DebugMenuBoolItemUI(parent, m_MenuItem, m_Label.text);
            }
            else if (m_MenuItem.GetItemType() == typeof(int))
            {
                newItemUI = new DebugMenuIntItemUI(parent, m_MenuItem, m_Label.text);
            }
            else if (m_MenuItem.GetItemType() == typeof(uint))
            {
                newItemUI = new DebugMenuUIntItemUI(parent, m_MenuItem, m_Label.text);
            }
            else if (m_MenuItem.GetItemType() == typeof(float))
            {
                newItemUI = new DebugMenuFloatItemUI(parent, m_MenuItem, m_Label.text);
            }
            else if (m_MenuItem.GetItemType() == typeof(Color))
            {
                newItemUI = new DebugMenuColorItemUI(parent, m_MenuItem, m_Label.text);
            }
            else if (m_MenuItem.GetItemType().BaseType == typeof(System.Enum))
            {
                newItemUI = new DebugMenuEnumItemUI(parent, m_MenuItem, m_Label.text, m_EnumStrings.ToArray(), m_EnumValues.ToArray());
            }

            return newItemUI;
        }

#if UNITY_EDITOR
        bool DrawBoolItem()
        {
            bool value = (bool)m_MenuItem.GetValue();

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawIntItem()
        {
            int value = (int)m_MenuItem.GetValue();
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntField(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawUIntItem()
        {
            int value = (int)(uint)m_MenuItem.GetValue();
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntField(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = System.Math.Max(0, value);
                m_MenuItem.SetValue((uint)value);
                return true;
            }

            return false;
        }

        bool DrawFloatItem()
        {
            float value = (float)m_MenuItem.GetValue();
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.FloatField(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                m_MenuItem.SetValue(value);
                return true;
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

        public override bool OnEditorGUI()
        {
            Initialize();

            if (m_MenuItem.readOnly)
            {
                EditorGUILayout.LabelField(m_Label, new GUIContent(m_MenuItem.GetValue().ToString()));
                return false;
            }

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

    public class DebugItemHandlerFloatMinMax
        : DefaultDebugItemHandler
    {
        float m_Min = 0.0f;
        float m_Max = 1.0f;
        public DebugItemHandlerFloatMinMax(float min, float max)
        {
            m_Min = min;
            m_Max = max;
        }

        public override void ClampValues(Func<object> getter, Action<object> setter)
        {
            setter(Mathf.Clamp((float)getter(), m_Min, m_Max));
        }

#if UNITY_EDITOR
        public override bool OnEditorGUI()
        {
            Initialize();

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

    public class DebugItemHandlerIntEnum
        : DefaultDebugItemHandler
    {
        GUIContent[]    m_IntEnumStrings = null;
        int[]           m_IntEnumValues = null;

        public DebugItemHandlerIntEnum(GUIContent[] enumStrings, int[] enumValues)
        {
            m_IntEnumStrings = enumStrings;
            m_IntEnumValues = enumValues;
        }

        public override DebugMenuItemUI BuildGUI(GameObject parent)
        {
            Initialize();

            return new DebugMenuEnumItemUI(parent, m_MenuItem, m_Label.text, m_IntEnumStrings, m_IntEnumValues);
        }

#if UNITY_EDITOR
        public override bool OnEditorGUI()
        {
            Initialize();

            UnityEditor.EditorGUI.BeginChangeCheck();
            int value = UnityEditor.EditorGUILayout.IntPopup(m_Label, (int)m_MenuItem.GetValue(), m_IntEnumStrings, m_IntEnumValues);
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

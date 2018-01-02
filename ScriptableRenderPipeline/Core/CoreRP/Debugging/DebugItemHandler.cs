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
        protected DebugItem m_DebugItem = null;

        public void SetDebugItem(DebugItem item)
        {
            m_DebugItem = item;
        }

        // Method user needs to override for specific value validation.
        public virtual void ValidateValues(Func<object> getter, Action<object> setter) {}
        // Method that will create UI items for runtime debug menu.
        public abstract DebugItemUI BuildGUI(GameObject parent);

#if UNITY_EDITOR
        // Method users need to override for editor specific UI
        public abstract bool OnEditorGUIImpl();
        public void OnEditorGUI()
        {
            if (m_DebugItem.runtimeOnly)
                return;

            if(OnEditorGUIImpl())
            {
                DebugMenuUI.changed = true;
            }
        }
#endif

        // Method users need to override for specific serialization of custom types.
        public abstract DebugItemState CreateDebugItemState();
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

            m_Label = new GUIContent(m_DebugItem.name);
            Type itemType = m_DebugItem.GetItemType();
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

        public override DebugItemState CreateDebugItemState()
        {
            DebugItemState newItemState = null;
            if (m_DebugItem.GetItemType() == typeof(bool))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateBool>();
            }
            else if (m_DebugItem.GetItemType() == typeof(int))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateInt>();
            }
            else if (m_DebugItem.GetItemType() == typeof(uint))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateUInt>();
            }
            else if (m_DebugItem.GetItemType() == typeof(float))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateFloat>();
            }
            else if (m_DebugItem.GetItemType() == typeof(Color))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateColor>();
            }
            else if (m_DebugItem.GetItemType().BaseType == typeof(System.Enum))
            {
                newItemState = ScriptableObject.CreateInstance<DebugItemStateInt>(); // Need to be serialized as int. For some reason serialization of the Enum directly just fails...
            }

            return newItemState;
        }

        public override DebugItemUI BuildGUI(GameObject parent)
        {
            Initialize();

            DebugItemUI newItemUI = null;
            if (m_DebugItem.GetItemType() == typeof(bool))
            {
                newItemUI = new DebugBoolItemUI(parent, m_DebugItem, m_Label.text);
            }
            else if (m_DebugItem.GetItemType() == typeof(int))
            {
                newItemUI = new DebugIntItemUI(parent, m_DebugItem, m_Label.text);
            }
            else if (m_DebugItem.GetItemType() == typeof(uint))
            {
                newItemUI = new DebugUIntItemUI(parent, m_DebugItem, m_Label.text);
            }
            else if (m_DebugItem.GetItemType() == typeof(float))
            {
                newItemUI = new DebugFloatItemUI(parent, m_DebugItem, m_Label.text);
            }
            else if (m_DebugItem.GetItemType() == typeof(Color))
            {
                newItemUI = new DebugColorItemUI(parent, m_DebugItem, m_Label.text);
            }
            else if (m_DebugItem.GetItemType().BaseType == typeof(System.Enum))
            {
                newItemUI = new DebugEnumItemUI(parent, m_DebugItem, m_Label.text, m_EnumStrings.ToArray(), m_EnumValues.ToArray());
            }

            return newItemUI;
        }

#if UNITY_EDITOR
        bool DrawBoolItem()
        {
            bool value = (bool)m_DebugItem.GetValue();

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawIntItem()
        {
            int value = (int)m_DebugItem.GetValue();
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntField(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawUIntItem()
        {
            int value = (int)(uint)m_DebugItem.GetValue();
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntField(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                value = System.Math.Max(0, value);
                m_DebugItem.SetValue((uint)value);
                return true;
            }

            return false;
        }

        bool DrawFloatItem()
        {
            float value = (float)m_DebugItem.GetValue();
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.FloatField(m_Label, value);
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawColorItem()
        {
            EditorGUI.BeginChangeCheck();
            Color value = EditorGUILayout.ColorField(m_Label, (Color)m_DebugItem.GetValue());
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }

        bool DrawEnumItem()
        {
            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.IntPopup(m_Label, (int)m_DebugItem.GetValue(), m_EnumStrings.ToArray(), m_EnumValues.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }

        public override bool OnEditorGUIImpl()
        {
            Initialize();

            if (m_DebugItem.readOnly)
            {
                EditorGUILayout.LabelField(m_Label, new GUIContent(m_DebugItem.GetValue().ToString()));
                return false;
            }

            if (m_DebugItem.GetItemType() == typeof(bool))
            {
                return DrawBoolItem();
            }
            else if (m_DebugItem.GetItemType() == typeof(int))
            {
                return DrawIntItem();
            }
            else if(m_DebugItem.GetItemType() == typeof(uint))
            {
                return DrawUIntItem();
            }
            else if (m_DebugItem.GetItemType() == typeof(float))
            {
                return DrawFloatItem();
            }
            else if (m_DebugItem.GetItemType() == typeof(Color))
            {
                return DrawColorItem();
            }
            else if (m_DebugItem.GetItemType().BaseType == typeof(System.Enum))
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
        protected float m_Min = 0.0f;
        protected float m_Max = 1.0f;

        public DebugItemHandlerFloatMinMax(float min, float max)
        {
            m_Min = min;
            m_Max = max;
        }

        public override void ValidateValues(Func<object> getter, Action<object> setter)
        {
            setter(Mathf.Clamp((float)getter(), m_Min, m_Max));
        }

#if UNITY_EDITOR
        public override bool OnEditorGUIImpl()
        {
            Initialize();

            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(m_DebugItem.name, (float)m_DebugItem.GetValue(), m_Min, m_Max);
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }
#endif
    }

    public class DebugItemHandlerUIntMinMax
    : DefaultDebugItemHandler
    {
        protected uint m_Min = 0;
        protected uint m_Max = 1;

        public DebugItemHandlerUIntMinMax(uint min, uint max)
        {
            m_Min = min;
            m_Max = max;
        }

        public override void ValidateValues(Func<object> getter, Action<object> setter)
        {
            setter(Math.Min(m_Max, Math.Max(m_Min, (uint)getter())));
        }

#if UNITY_EDITOR
        public override bool OnEditorGUIImpl()
        {
            Initialize();

            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.IntSlider(m_DebugItem.name, (int)(uint)m_DebugItem.GetValue(), (int)m_Min, (int)m_Max);
            if (EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue((uint)value);
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

        public override DebugItemUI BuildGUI(GameObject parent)
        {
            Initialize();

            return new DebugEnumItemUI(parent, m_DebugItem, m_Label.text, m_IntEnumStrings, m_IntEnumValues);
        }

#if UNITY_EDITOR
        public override bool OnEditorGUIImpl()
        {
            Initialize();

            UnityEditor.EditorGUI.BeginChangeCheck();
            int value = UnityEditor.EditorGUILayout.IntPopup(m_Label, (int)m_DebugItem.GetValue(), m_IntEnumStrings, m_IntEnumValues);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                m_DebugItem.SetValue(value);
                return true;
            }

            return false;
        }
#endif
    }

}

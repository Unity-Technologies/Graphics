using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    // Class that users need to extend for runtime debug menu item UI
    public abstract class DebugItemUI
    {
        protected GameObject    m_Root = null;
        protected DebugItem     m_DebugItem = null;

        public bool dynamicDisplay { get { return (m_DebugItem.flags & DebugItemFlag.DynamicDisplay) != 0; } }

        public DebugItem debugItem { get { return m_DebugItem; } }

        protected DebugItemUI(DebugItem debugItem)
        {
            m_DebugItem = debugItem;
        }

        // Implement for selection specific beahavior (like changing color for example)
        public abstract void SetSelected(bool value);
        // Implement behavior when user execute the "Validate" action
        public abstract void OnValidate();
        // Implement behavior when user execute the "Increment" action
        public abstract void OnIncrement();
        // Implement behavior when user execute the "Decrement" action
        public abstract void OnDecrement();
        // Implement this method to update the UI with current item value.
        // User must call it whenever Validate/Increment/Decrement is called. It will also be automatically called for dynamically displayed items.
        public abstract void Update();
    }

    public class DebugSimpleItemUI : DebugItemUI
    {
        protected GameObject m_Name = null;
        protected GameObject m_Value = null;

        protected DebugSimpleItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(debugItem)
        {

            m_Root = DebugMenuUI.CreateHorizontalLayoutGroup("", true, true, false, false, parent);
            m_Name = DebugMenuUI.CreateTextElement(m_DebugItem.name, name, 10, TextAnchor.MiddleLeft, m_Root);
            var layoutElem = m_Name.AddComponent<UI.LayoutElement>();
            layoutElem.minWidth = DebugMenuUI.kDebugItemNameWidth;
            m_Value = DebugMenuUI.CreateTextElement(string.Format("{0} value", name), "", 10, TextAnchor.MiddleLeft, m_Root);
        }

        public override void SetSelected(bool value)
        {
            m_Name.GetComponent<UI.Text>().color = value ? DebugMenuUI.kColorSelected : DebugMenuUI.kColorUnSelected;
            m_Value.GetComponent<UI.Text>().color = value ? DebugMenuUI.kColorSelected : DebugMenuUI.kColorUnSelected;
        }

        public override void OnValidate()
        {
            throw new System.NotImplementedException();
        }

        public override void OnIncrement()
        {
            throw new System.NotImplementedException();
        }

        public override void OnDecrement()
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }

    public class DebugBoolItemUI : DebugSimpleItemUI
    {
        public DebugBoolItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(parent, debugItem, name)
        {
            Update();
        }

        public override void Update()
        {
            m_Value.GetComponent<UI.Text>().text = (bool)m_DebugItem.GetValue() ? "True" : "False";
        }


        public override void OnValidate()
        {
            m_DebugItem.SetValue(!(bool)m_DebugItem.GetValue());
            Update();
        }

        public override void OnIncrement()
        {
            OnValidate();
        }

        public override void OnDecrement()
        {
            OnValidate();
        }
    }

    public class DebugFloatItemUI : DebugSimpleItemUI
    {
        bool m_SelectIncrementMode = false;
        int m_CurrentIncrementIndex = -1;

        public DebugFloatItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(parent, debugItem, name)
        {
            Update();
        }

        public override void Update()
        {
            float currentValue = (float)m_DebugItem.GetValue();
            bool isNegative = currentValue < 0.0f;
            // Easier to format the string without caring about the '-' sign. We add it back at the end
            currentValue = Mathf.Abs(currentValue);

            char separator = System.Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            // Start with the maximum amount of trailing zeros
            string valueWithMaxDecimals = string.Format("{0:0.00000}", currentValue);

            // Remove trailing zeros until we reach the separator or we reach the decimal we are currently editing.
            int separatorIndex = valueWithMaxDecimals.LastIndexOf(separator);
            int index = valueWithMaxDecimals.Length - 1;
            while (
                valueWithMaxDecimals[index] == '0' // Remove trailing zeros
                && index > (separatorIndex + 1) // until we reach the separator
                && index > (System.Math.Abs(m_CurrentIncrementIndex) + separatorIndex)) // Or it's the index of the current decimal being edited (so as to display the last trailing zero in this case)
            {
                index--;
            }

            string finalValue = new string(valueWithMaxDecimals.ToCharArray(), 0, index + 1);

            // Add leading zeros until we reach where the current order is being edited.
            if (m_CurrentIncrementIndex > 0)
            {
                float incrementValue = Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex);
                if (incrementValue > currentValue)
                {
                    float compareValue = currentValue + 1.0f; // Start at 1.0f because we know that we are going to increment by 10 or more
                    while (incrementValue > compareValue)
                    {
                        finalValue = finalValue.Insert(0, "0");
                        compareValue *= 10.0f;
                    }
                }
            }

            // When selecting which decimal/order you want to edit, we show brackets around the figure to show the user.
            if (m_SelectIncrementMode)
            {
                separatorIndex = finalValue.LastIndexOf(separator); // separator may have changed place if we added leading zeros
                int bracketIndex = separatorIndex - m_CurrentIncrementIndex;
                if (m_CurrentIncrementIndex >= 0) // Skip separator
                    bracketIndex -= 1;

                finalValue = finalValue.Insert(bracketIndex, "[");
                finalValue = finalValue.Insert(bracketIndex + 2, "]");
            }

            if (isNegative)
                finalValue = finalValue.Insert(0, "-");

            m_Value.GetComponent<UI.Text>().text = finalValue;
        }

        public override void OnValidate()
        {
            m_SelectIncrementMode = !m_SelectIncrementMode;
            Update();
        }

        public override void OnIncrement()
        {
            if (!m_SelectIncrementMode)
            {
                m_DebugItem.SetValue((float)m_DebugItem.GetValue() + Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex));
            }
            else
            {
                m_CurrentIncrementIndex -= 1; // * 0.1 (10^m_CurrentIncrementIndex)
                m_CurrentIncrementIndex = System.Math.Max(-5, m_CurrentIncrementIndex);
            }
            Update();
        }

        public override void OnDecrement()
        {
            if (!m_SelectIncrementMode)
            {
                m_DebugItem.SetValue((float)m_DebugItem.GetValue() - Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex));
            }
            else
            {
                m_CurrentIncrementIndex += 1; // * 10 (10^m_CurrentIncrementIndex)
            }
            Update();
        }
    }

    // Everything is done with int. We don't really care about values > 2b for debugging.
    public class DebugIntegerItemUI : DebugSimpleItemUI
    {
        bool m_SelectIncrementMode = false;
        int m_CurrentIncrementIndex = 0;

        public DebugIntegerItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(parent, debugItem, name)
        {
        }

        protected void UpdateText(int value)
        {
            bool isNegative = value < 0f;
            // Easier to format the string without caring about the '-' sign. We add it back at the end
            value = System.Math.Abs(value);

            string finalValue = string.Format("{0}", value);

            // Add leading zeros until we reach where the current order is being edited.
            if (m_CurrentIncrementIndex > 0)
            {
                int incrementValue = (int)System.Math.Pow(10, m_CurrentIncrementIndex);
                if (incrementValue > value)
                {
                    int compareValue = System.Math.Max(value, 1);
                    while (incrementValue > compareValue)
                    {
                        finalValue = finalValue.Insert(0, "0");
                        compareValue *= 10;
                    }
                }
            }

            // When selecting which decimal/order you want to edit, we show brackets around the figure to show the user.
            if (m_SelectIncrementMode)
            {
                int bracketIndex = finalValue.Length - 1 - m_CurrentIncrementIndex;

                finalValue = finalValue.Insert(bracketIndex, "[");
                finalValue = finalValue.Insert(bracketIndex + 2, "]");
            }

            if (isNegative)
                finalValue = finalValue.Insert(0, "-");

            m_Value.GetComponent<UI.Text>().text = finalValue;
        }

        protected virtual int GetIntegerValue()
        {
            throw new System.NotImplementedException();
        }

        protected virtual void SetIntegerValue(int value)
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            UpdateText(GetIntegerValue());
        }

        public override void OnValidate()
        {
            m_SelectIncrementMode = !m_SelectIncrementMode;
            Update();
        }

        public override void OnIncrement()
        {
            if (!m_SelectIncrementMode)
            {
                SetIntegerValue(GetIntegerValue() + (int)Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex));
            }
            else
            {
                m_CurrentIncrementIndex -= 1; // *= 0.1 (10^m_CurrentIncrementIndex)
                m_CurrentIncrementIndex = System.Math.Max(0, m_CurrentIncrementIndex);
            }
            Update();
        }

        public override void OnDecrement()
        {
            if (!m_SelectIncrementMode)
            {
                SetIntegerValue(GetIntegerValue() - (int)Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex));
            }
            else
            {
                m_CurrentIncrementIndex += 1; // *= 10 (10^m_CurrentIncrementIndex)
                m_CurrentIncrementIndex = System.Math.Max(0, m_CurrentIncrementIndex);
            }
            Update();
        }
    }
    public class DebugIntItemUI : DebugIntegerItemUI
    {
        public DebugIntItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(parent, debugItem, name)
        {
            UpdateText((int)m_DebugItem.GetValue());
        }

        protected override int GetIntegerValue()
        {
            return (int)m_DebugItem.GetValue();
        }

        protected override void SetIntegerValue(int value)
        {
            m_DebugItem.SetValue(value);
        }
    }

    public class DebugUIntItemUI : DebugIntegerItemUI
    {
        public DebugUIntItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(parent, debugItem, name)
        {
            UpdateText((int)(uint)m_DebugItem.GetValue());
        }

        protected override int GetIntegerValue()
        {
            return (int)(uint)m_DebugItem.GetValue();
        }

        protected override void SetIntegerValue(int value)
        {
            m_DebugItem.SetValue((uint)System.Math.Max(0, value));
        }
    }

    public class DebugEnumItemUI : DebugSimpleItemUI
    {
        int                 m_CurrentValueIndex = 0;
        GUIContent[]        m_ValueNames;
        int[]               m_Values;

        public DebugEnumItemUI(GameObject parent, DebugItem debugItem, string name, GUIContent[] valueNames, int[] values)
            : base(parent, debugItem, name)
        {
            m_Values = values;
            m_ValueNames = valueNames;
            m_CurrentValueIndex = FindIndexForValue((int)m_DebugItem.GetValue());

            Update();
        }

        private int FindIndexForValue(int value)
        {
            for (int i = 0; i < m_Values.Length; ++i)
            {
                if (m_Values[i] == value)
                    return i;
            }

            return -1;
        }

        public override void Update()
        {
            if (m_CurrentValueIndex != -1)
            {
                m_Value.GetComponent<UI.Text>().text = m_ValueNames[m_CurrentValueIndex].text;
            }
        }

        public override void OnValidate()
        {
            OnIncrement();
        }

        public override void OnIncrement()
        {
            m_CurrentValueIndex = (m_CurrentValueIndex + 1) % m_Values.Length;
            m_DebugItem.SetValue(m_Values[m_CurrentValueIndex]);
            Update();
        }

        public override void OnDecrement()
        {
            m_CurrentValueIndex -= 1;
            if (m_CurrentValueIndex < 0)
                m_CurrentValueIndex = m_Values.Length - 1;

            m_DebugItem.SetValue(m_Values[m_CurrentValueIndex]);
            Update();
        }
    }

    public class DebugColorItemUI : DebugItemUI
    {
        protected GameObject m_Name = null;
        protected GameObject m_ColorRect = null;

        public DebugColorItemUI(GameObject parent, DebugItem debugItem, string name)
            : base(debugItem)
        {
            m_DebugItem = debugItem;

            m_Root = DebugMenuUI.CreateHorizontalLayoutGroup(name, true, true, false, false, parent);

            m_Name = DebugMenuUI.CreateTextElement(m_DebugItem.name, name, 10, TextAnchor.MiddleLeft, m_Root);
            var layoutElemName = m_Name.AddComponent<UI.LayoutElement>();
            layoutElemName.minWidth = DebugMenuUI.kDebugItemNameWidth;

            // Force layout because we need the right height for the color rect element afterward.
            UI.LayoutRebuilder.ForceRebuildLayoutImmediate(m_Root.GetComponent<RectTransform>());
            RectTransform nameRect = m_Name.GetComponent<RectTransform>();

            m_ColorRect = new GameObject();
            m_ColorRect.transform.SetParent(m_Root.transform, false);
            m_ColorRect.AddComponent<UI.Image>();
            UI.LayoutElement layoutElem = m_ColorRect.AddComponent<UI.LayoutElement>();
            // We need to set min width/height because without an image, the size would be zero.
            layoutElem.minHeight = nameRect.rect.height;
            layoutElem.minWidth = 40.0f;

            Update();
        }

        public override void Update()
        {
            Color currentValue = (Color)m_DebugItem.GetValue();
            UI.Image image = m_ColorRect.GetComponent<UI.Image>();
            image.color = currentValue;
        }

        public override void SetSelected(bool value)
        {
            m_Name.GetComponent<UI.Text>().color = value ? DebugMenuUI.kColorSelected : DebugMenuUI.kColorUnSelected;
        }

        // TODO: Edit mode!
        public override void OnValidate()
        {
        }

        public override void OnIncrement()
        {
        }

        public override void OnDecrement()
        {
        }
    }
}

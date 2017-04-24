using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public abstract class DebugMenuItemUI
    {
        protected GameObject m_Root = null;
        protected DebugMenuItem m_MenuItem = null;

        public abstract void SetSelected(bool value);
        public abstract void OnValidate();
        public abstract void OnIncrement();
        public abstract void OnDecrement();
    }

    public class DebugMenuSimpleItemUI : DebugMenuItemUI
    {
        protected GameObject m_Name = null;
        protected GameObject m_Value = null;

        protected DebugMenuSimpleItemUI(GameObject parent, DebugMenuItem menuItem)
        {
            m_MenuItem = menuItem;

            m_Root = new GameObject();
            m_Root.transform.SetParent(parent.transform, false);
            UI.HorizontalLayoutGroup horizontalLayout = m_Root.AddComponent<UI.HorizontalLayoutGroup>();
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childForceExpandWidth = false;

            m_Name = DebugMenuUI.CreateTextDebugElement(m_MenuItem.name, m_MenuItem.name, 10, TextAnchor.MiddleLeft, m_Root);
            var layoutElem = m_Name.AddComponent<UI.LayoutElement>();
            layoutElem.minWidth = DebugMenuUI.kDebugItemNameWidth;
            m_Value = DebugMenuUI.CreateTextDebugElement(string.Format("{0} value", m_MenuItem.name), "", 10, TextAnchor.MiddleLeft, m_Root);
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
    }

    public class DebugMenuBoolItemUI : DebugMenuSimpleItemUI
    {
        public DebugMenuBoolItemUI(GameObject parent, DebugMenuItem menuItem)
            : base(parent, menuItem)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            m_Value.GetComponent<UI.Text>().text = (bool)m_MenuItem.GetValue() ? "True" : "False";
        }


        public override void OnValidate()
        {
            m_MenuItem.SetValue(!(bool)m_MenuItem.GetValue());
            UpdateText();
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

    public class DebugMenuFloatItemUI : DebugMenuSimpleItemUI
    {
        bool m_SelectIncrementMode = false;
        int m_CurrentIncrementIndex = -1;

        public DebugMenuFloatItemUI(GameObject parent, DebugMenuItem menuItem)
            : base(parent, menuItem)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            float currentValue = (float)m_MenuItem.GetValue();
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
            if(m_CurrentIncrementIndex > 0)
            {
                float incrementValue = Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex);
                if(incrementValue > currentValue)
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
            if(m_SelectIncrementMode)
            {
                separatorIndex = finalValue.LastIndexOf(separator); // separator may have changed place if we added leading zeros
                int bracketIndex = separatorIndex - m_CurrentIncrementIndex;
                if(m_CurrentIncrementIndex >= 0) // Skip separator
                    bracketIndex -= 1;

                finalValue = finalValue.Insert(bracketIndex, "[");
                finalValue = finalValue.Insert(bracketIndex + 2, "]");
            }

            if(isNegative)
                finalValue = finalValue.Insert(0, "-");

            m_Value.GetComponent<UI.Text>().text = finalValue;
        }

        public override void OnValidate()
        {
            m_SelectIncrementMode = !m_SelectIncrementMode;
            UpdateText();
        }

        public override void OnIncrement()
        {
            if(!m_SelectIncrementMode)
            {
                m_MenuItem.SetValue((float)m_MenuItem.GetValue() + Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex));
            }
            else
            {
                m_CurrentIncrementIndex -= 1; // * 0.1 (10^m_CurrentIncrementIndex)
                m_CurrentIncrementIndex = System.Math.Max(-5, m_CurrentIncrementIndex);
            }
            UpdateText();
        }

        public override void OnDecrement()
        {
            if (!m_SelectIncrementMode)
            {
                m_MenuItem.SetValue((float)m_MenuItem.GetValue() - Mathf.Pow(10.0f, (float)m_CurrentIncrementIndex));
            }
            else
            {
                m_CurrentIncrementIndex += 1; // * 10 (10^m_CurrentIncrementIndex)
            }
            UpdateText();            
        }
    }

    // Everything is done with int. We don't really care about values > 2b for debugging.
    public class DebugMenuIntegerItemUI : DebugMenuSimpleItemUI
    {
        bool m_SelectIncrementMode = false;
        int m_CurrentIncrementIndex = 0;

        public DebugMenuIntegerItemUI(GameObject parent, DebugMenuItem menuItem)
            : base(parent, menuItem)
        {
        }

        protected void UpdateText(int value)
        {
            bool isNegative = value < 0f;
            // Easier to format the string without caring about the '-' sign. We add it back at the end
            value = System.Math.Abs(value);

            string finalValue = string.Format("{0}", value);

            // Add leading zeros until we reach where the current order is being edited.
            if(m_CurrentIncrementIndex > 0)
            {
                int incrementValue = (int)System.Math.Pow(10, m_CurrentIncrementIndex);
                if(incrementValue > value)
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
            if(m_SelectIncrementMode)
            {
                int bracketIndex = finalValue.Length - 1 - m_CurrentIncrementIndex;

                finalValue = finalValue.Insert(bracketIndex, "[");
                finalValue = finalValue.Insert(bracketIndex + 2, "]");
            }

            if(isNegative)
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

        public override void OnValidate()
        {
            m_SelectIncrementMode = !m_SelectIncrementMode;
            UpdateText(GetIntegerValue());
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
            UpdateText(GetIntegerValue());
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
            UpdateText(GetIntegerValue());
        }
    }
    public class DebugMenuIntItemUI : DebugMenuIntegerItemUI
    {
        public DebugMenuIntItemUI(GameObject parent, DebugMenuItem menuItem)
            : base(parent, menuItem)
        {
            UpdateText((int)m_MenuItem.GetValue());
        }

        protected override int GetIntegerValue()
        {
            return (int)m_MenuItem.GetValue();
        }

        protected override void SetIntegerValue(int value)
        {
            m_MenuItem.SetValue(value);
        }
    }

    public class DebugMenuUIntItemUI : DebugMenuIntegerItemUI
    {
        public DebugMenuUIntItemUI(GameObject parent, DebugMenuItem menuItem)
            : base(parent, menuItem)
        {
            UpdateText((int)(uint)m_MenuItem.GetValue());
        }

        protected override int GetIntegerValue()
        {
            return (int)(uint)m_MenuItem.GetValue();
        }

        protected override void SetIntegerValue(int value)
        {
            m_MenuItem.SetValue((uint)System.Math.Max(0, value));
        }
    }
}

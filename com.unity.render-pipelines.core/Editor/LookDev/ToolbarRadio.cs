using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    class ToolbarRadio : Toolbar, INotifyValueChanged<int>
    {
        public new class UxmlFactory : UxmlFactory<ToolbarRadio, UxmlTraits> { }
        public new class UxmlTraits : Button.UxmlTraits { }

        List<ToolbarToggle> radios = new List<ToolbarToggle>();

        public new static readonly string ussClassName = "unity-toolbar-radio";

        bool m_CanDeselectAll = false;

        public int radioLength { get; private set; } = 0;

        int m_Value;
        public int value
        {
            get => m_Value;
            set
            {
                if (value == m_Value)
                    return;

                if (panel != null)
                {
                    using (ChangeEvent<int> evt = ChangeEvent<int>.GetPooled(m_Value, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(evt);
                    }
                }
                else
                {
                    SetValueWithoutNotify(value);
                }
            }
        }

        public ToolbarRadio() : this(null, false) { }

        public ToolbarRadio(string label = null, bool canDeselectAll = false)
        {
            RemoveFromClassList(Toolbar.ussClassName);
            AddToClassList(ussClassName);

            m_CanDeselectAll = canDeselectAll;
            if (label != null)
                Add(new Label() { text = label });
        }

        public void AddRadio(string text = null, Texture2D icon = null)
        {
            var toggle = new ToolbarToggle();
            toggle.RegisterValueChangedCallback(InnerValueChanged(radioLength));
            toggle.SetValueWithoutNotify(radioLength == (m_CanDeselectAll ? -1 : 0));
            radios.Add(toggle);
            if (icon != null)
            {
                var childsContainer = toggle.Q(null, ToolbarToggle.inputUssClassName);
                childsContainer.Add(new Image() { image = icon });
                if (text != null)
                    childsContainer.Add(new Label() { text = text });
            }
            else
                toggle.text = text;
            Add(toggle);
            if (radioLength == 0)
                toggle.style.borderLeftWidth = 1;
            radioLength++;
        }

        public void AddRadios(string[] labels)
        {
            foreach (var label in labels)
                AddRadio(label);
        }

        public void AddRadios(Texture2D[] icons)
        {
            foreach (var icon in icons)
                AddRadio(null, icon);
        }

        public void AddRadios((string text, Texture2D icon)[] labels)
        {
            foreach (var label in labels)
                AddRadio(label.text, label.icon);
        }

        EventCallback<ChangeEvent<bool>> InnerValueChanged(int radioIndex)
        {
            return (ChangeEvent<bool> evt) =>
            {
                if (radioIndex == m_Value)
                {
                    if (!evt.newValue && !m_CanDeselectAll)
                        radios[radioIndex].SetValueWithoutNotify(true);
                    else
                        value = -1;
                }
                else
                    value = radioIndex;
            };
        }

        public void SetValueWithoutNotify(int newValue)
        {
            if (m_Value != newValue)
            {
                if (newValue < (m_CanDeselectAll ? -1 : 0) || newValue >= radioLength)
                    throw new System.IndexOutOfRangeException();

                if (m_Value == newValue && m_CanDeselectAll)
                {
                    if (m_Value > -1)
                        radios[m_Value].SetValueWithoutNotify(false);
                    m_Value = -1;
                }
                else
                {
                    if (m_Value > -1)
                        radios[m_Value].SetValueWithoutNotify(false);
                    if (newValue > -1)
                        radios[newValue].SetValueWithoutNotify(true);
                    m_Value = newValue;
                }
            }
        }
    }
}

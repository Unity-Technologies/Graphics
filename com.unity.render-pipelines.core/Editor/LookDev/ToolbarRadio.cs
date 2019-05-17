using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    public class ToolbarRadio : Toolbar, INotifyValueChanged<int>
    {
        public new class UxmlFactory : UxmlFactory<ToolbarRadio, UxmlTraits> { }
        public new class UxmlTraits : Button.UxmlTraits { }

        List<ToolbarToggle> radios = new List<ToolbarToggle>();

        public new static readonly string ussClassName = "unity-toolbar-radio";

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

        public ToolbarRadio()
        {
            RemoveFromClassList(Toolbar.ussClassName);
            AddToClassList(ussClassName);
        }

        public void AddRadio(string text = null, Texture2D icon = null)
        {
            var toggle = new ToolbarToggle();
            toggle.RegisterValueChangedCallback(InnerValueChanged(radioLength));
            toggle.SetValueWithoutNotify(radioLength == 0);
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
                    if (!evt.newValue)
                        radios[radioIndex].SetValueWithoutNotify(true);
                }
                else
                    value = radioIndex;
            };
        }

        public void SetValueWithoutNotify(int newValue)
        {
            if (m_Value != newValue)
            {
                if (newValue < 0 || newValue >= radioLength)
                    throw new System.IndexOutOfRangeException();

                radios[m_Value].SetValueWithoutNotify(false);
                radios[newValue].SetValueWithoutNotify(true);

                m_Value = newValue;
            }
        }
    }
}

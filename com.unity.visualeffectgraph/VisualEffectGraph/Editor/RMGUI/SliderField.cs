using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class SliderField : FloatField
    {
        Slider m_Slider;

        void CreateSlider(Vector2 range)
        {
            m_Slider = new Slider(range.x, range.y, ValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
        }

        public SliderField(string label, Vector2 range) : base(label)
        {
            CreateSlider(range);
            Add(m_Slider);
        }

        public SliderField(VisualElement existingLabel, Vector2 range) : base(existingLabel)
        {
            CreateSlider(range);
            Add(m_Slider);
        }

        void ValueChanged(float newValue)
        {
            SetValue(newValue);
        }

        protected override void ValueToGUI()
        {
            base.ValueToGUI();
            m_Slider.value = GetValue();
        }
    }
    class IntSliderField : IntField
    {
        Slider m_Slider;

        void CreateSlider(Vector2 range)
        {
            m_Slider = new Slider(range.x, range.y, ValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
        }

        public IntSliderField(string label, Vector2 range) : base(label)
        {
            CreateSlider(range);
            Add(m_Slider);
        }

        public IntSliderField(VisualElement existingLabel, Vector2 range) : base(existingLabel)
        {
            CreateSlider(range);
            Add(m_Slider);
        }

        void ValueChanged(float newValue)
        {
            SetValue((int)newValue);
        }

        protected override void ValueToGUI()
        {
            base.ValueToGUI();
            m_Slider.value = (float)GetValue();
        }
    }
}

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
			m_Slider = new Slider(range.x, range.y, OnValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
        }

        public SliderField(string label, Vector2 range) : base(label)
        {
			CreateSlider(range);
            AddChild(m_Slider);
        }

        public SliderField(VisualElement existingLabel, Vector2 range) : base(existingLabel)
        {
			CreateSlider(range);
			AddChild(m_Slider);
        }

		void OnValueChanged(float newValue)
		{
			SetValue(newValue);
		}

		protected override void ValueToGUI()
		{
			base.ValueToGUI();
			m_Slider.value = GetValue();
		}
    }
}

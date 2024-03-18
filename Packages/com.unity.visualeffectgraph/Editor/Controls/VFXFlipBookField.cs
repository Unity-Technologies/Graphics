using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXFlipBookField : VFXControl<FlipBook>
    {
        IntegerField m_X;
        IntegerField m_Y;

        void CreateTextField()
        {
            m_X = new IntegerField("X");
            m_Y = new IntegerField("Y");

            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<int>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<int>>(OnYValueChanged);
        }

        void OnXValueChanged(ChangeEvent<int> e)
        {
            FlipBook newValue = value;
            newValue.x = (int)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<int> e)
        {
            FlipBook newValue = value;
            newValue.y = (int)m_Y.value;
            SetValueAndNotify(newValue);
        }

        public override bool indeterminate
        {
            get => m_X.showMixedValue;
            set
            {
                m_X.showMixedValue = value;
                m_Y.showMixedValue = value;
            }
        }

        public VFXFlipBookField(string label)
        {
            var labelElement = new Label(label);
            labelElement.AddToClassList("label");
            Add(labelElement);
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
        }

        protected override void ValueToGUI(bool force)
        {
            if (!m_X.HasFocus() || force)
                m_X.value = value.x;

            if (!m_Y.HasFocus() || force)
                m_Y.value = value.y;
        }
    }
}

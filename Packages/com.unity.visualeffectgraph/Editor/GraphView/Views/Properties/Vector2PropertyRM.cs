using UnityEditor.UIElements;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    /// <summary>
    /// Vector2 properties can represent a Min-Max range. This custom PropertyRM allows to display it as a MinMaxSlider
    /// </summary>
    class Vector2PropertyRM : SimpleUIPropertyRM<Vector2, Vector2>
    {
        VFXVector2Field m_VectorField;
        protected VFXMinMaxSliderField m_Slider;

        public Vector2PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        { }

        private void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }

        private void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public override INotifyValueChanged<Vector2> CreateField()
        {
            INotifyValueChanged<Vector2> result;
            bool isMinMax = m_Provider.attributes.Is(VFXPropertyAttributes.Type.MinMax);
            if (isMinMax)
            {
                Vector2 range = m_Provider.attributes.FindRange();
                result = CreateSliderField(out m_Slider);
                m_Slider.range = range;
            }
            else
            {
                result = CreateSimpleField(out m_VectorField);
            }

            return result;
        }

        void OnFocusLost(BlurEvent e)
        {
            DelayedNotifyValueChange();
            UpdateGUI(true);
        }

        void DelayedNotifyValueChange()
        {
            if (isDelayed && hasChangeDelayed)
            {
                hasChangeDelayed = false;
                NotifyValueChanged();
            }
        }

        protected override void UpdateIndeterminate()
        {
            m_VectorField.indeterminate = indeterminate;
            if (m_Slider != null)
                m_Slider.indeterminate = indeterminate;
        }

        public override void UpdateGUI(bool force)
        {
            if (m_Slider != null)
            {
                Vector2 range = m_Provider.attributes.FindRange();

                m_Slider.range = range;
            }
            base.UpdateGUI(force);
        }

        public override object FilterValue(object value)
        {
            Vector2 range = m_Provider.attributes.FindRange();

            if (range != Vector2.zero)
            {
                var vectorValue = (Vector2)value;
                vectorValue.x = Mathf.Clamp(vectorValue.x, range.x, range.y);
                vectorValue.y = Mathf.Clamp(vectorValue.y, range.x, range.y);
                return vectorValue;
            }

            return value;
        }

        protected override bool HasFocus()
        {
            if (m_Slider != null)
                return m_Slider.HasFocus();
            if (m_VectorField != null)
                return m_VectorField.HasFocus();
            return false;
        }

        protected INotifyValueChanged<Vector2> CreateSliderField(out VFXMinMaxSliderField slider)
        {
            var field = new VFXLabeledField<VFXMinMaxSliderField, Vector2>(m_Label);
            slider = field.control;
            slider.onValueDragFinished = ValueDragFinished;
            slider.onValueDragStarted = ValueDragStarted;
            slider.RegisterCallback<BlurEvent>(OnFocusLost);
            return field;
        }

        protected INotifyValueChanged<Vector2> CreateSimpleField(out VFXVector2Field textField)
        {
            var field = new VFXLabeledField<VFXVector2Field, Vector2>(m_Label);
            field.onValueDragFinished = t => ValueDragFinished();
            field.onValueDragStarted = t => ValueDragStarted();
            textField = field.control;
            return field;
        }
    }
}

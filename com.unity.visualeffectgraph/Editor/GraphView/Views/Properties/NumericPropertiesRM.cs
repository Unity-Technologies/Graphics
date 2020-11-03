using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VFX.UIElements;


namespace UnityEditor.VFX.UI
{
    abstract class NumericPropertyRM<T, U> : SimpleUIPropertyRM<T, U>
    {
        public NumericPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            Vector2 range = m_Provider.attributes.FindRange();
            if (RangeShouldCreateSlider(range))
            {
                return 120;
            }
            return 60;
        }

        protected virtual bool RangeShouldCreateSlider(Vector2 range)
        {
            return range != Vector2.zero && range.y != Mathf.Infinity;
        }

        protected VFXBaseSliderField<U> m_Slider;
        protected TextValueField<U>     m_TextField;

        protected abstract INotifyValueChanged<U> CreateSimpleField(out TextValueField<U> textField);
        protected abstract INotifyValueChanged<U> CreateSliderField(out VFXBaseSliderField<U> slider);

        public override INotifyValueChanged<U> CreateField()
        {
            Vector2 range = m_Provider.attributes.FindRange();
            INotifyValueChanged<U> result;
            if (!RangeShouldCreateSlider(range))
            {
                result = CreateSimpleField(out m_TextField);
                if (m_TextField != null)
                {
                    m_TextField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnKeyDown);
                    m_TextField.Q("unity-text-input").RegisterCallback<BlurEvent>(OnFocusLost);
                }
            }
            else
            {
                result = CreateSliderField(out m_Slider);
                m_Slider.onValueDragFinished = ValueDragFinished;
                m_Slider.onValueDragStarted = ValueDragStarted;
                m_Slider.RegisterCallback<BlurEvent>(OnFocusLost);
                m_Slider.range = range;
            }
            return result;
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.character == '\n')
            {
                DelayedNotifyValueChange();
                UpdateGUI(true);
            }
        }

        void OnFocusLost(BlurEvent e)
        {
            DelayedNotifyValueChange();
            UpdateGUI(true);
        }

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }

        void DelayedNotifyValueChange()
        {
            if (isDelayed && hasChangeDelayed)
            {
                hasChangeDelayed = false;
                NotifyValueChanged();
            }
        }

        protected override bool HasFocus()
        {
            if (m_Slider != null)
                return m_Slider.HasFocus();
            if (m_TextField != null)
                return m_TextField.HasFocus();
            return false;
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            Vector2 range = m_Provider.attributes.FindRange();

            return RangeShouldCreateSlider(range) != (m_Slider == null);
        }

        public override void UpdateGUI(bool force)
        {
            if (m_Slider != null)
            {
                Vector2 range = m_Provider.attributes.FindRange();

                m_Slider.range = range;
            }
            if (m_TooltipHolder != null && m_Value != null)
                m_TooltipHolder.tooltip = m_Value.ToString();

            base.UpdateGUI(force);
        }

        public abstract T FilterValue(Vector2 range, T value);
        public override object FilterValue(object value)
        {
            Vector2 range = m_Provider.attributes.FindRange();

            if (range != Vector2.zero)
            {
                value = FilterValue(range, (T)value);
            }

            return value;
        }
    }
    abstract class IntegerPropertyRM<T, U> : NumericPropertyRM<T, U>
    {
        VisualElement m_IndeterminateLabel;
        public IntegerPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);
        }

        protected override void UpdateIndeterminate()
        {
            VisualElement field = this.field as VisualElement;
            if (indeterminate)
            {
                if (m_IndeterminateLabel.parent == null)
                {
                    field.RemoveFromHierarchy();
                    m_FieldParent.Add(m_IndeterminateLabel);
                }
            }
            else
            {
                if (field.parent == null)
                {
                    m_IndeterminateLabel.RemoveFromHierarchy();
                    m_FieldParent.Add(field);
                }
            }
        }
    }

    class UintPropertyRM : IntegerPropertyRM<uint, long>
    {
        VFX32BitField m_BitField;

        public UintPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }
        public override float GetPreferredControlWidth()
        {
            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum))
                return 120;

            return base.GetPreferredControlWidth() ;
        }
        protected VFXEnumValuePopup m_EnumPopup;

        public override INotifyValueChanged<long> CreateField()
        {
            INotifyValueChanged<long> result;
            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum))
            {
                result = m_EnumPopup = new VFXEnumValuePopup();
                m_EnumPopup.enumValues = m_Provider.attributes.FindEnum();
            }
            else
                result = base.CreateField();
            return result;
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && (uint)range.x < (uint)range.y;
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;


            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum) == (m_EnumPopup == null))
                return false;

            if(m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum))
            {
                string[] enumValues = m_Provider.attributes.FindEnum();

                return Enumerable.SequenceEqual(enumValues, m_EnumPopup.enumValues);
            }
            return true;
        }

        protected override INotifyValueChanged<long> CreateSimpleField(out TextValueField<long> textField)
        {
            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.BitField))
            {
                var bitfield = new VFXLabeledField<VFX32BitField, long>(m_Label);
                textField = null;
                return bitfield;
            }
            var field =  new VFXLabeledField<LongField, long>(m_Label);

            field.onValueDragFinished = t => ValueDragFinished();
            field.onValueDragStarted = t => ValueDragStarted();
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<long> CreateSliderField(out VFXBaseSliderField<long> slider)
        {
            var field = new VFXLabeledField<VFXLongSliderField, long>(m_Label);
            slider = field.control;
            return field;
        }

        public override object FilterValue(object value)
        {
            if ((uint)value < 0)
            {
                value = (uint)0;
            }
            return base.FilterValue(value);
        }

        public override uint Convert(object value)
        {
            long longValue = (long)value;

            if (longValue < 0)
            {
                longValue = 0;
            }

            return (uint)longValue;
        }

        public override uint FilterValue(Vector2 range, uint value)
        {
            uint val = value;
            if (range.x > val)
            {
                val = (uint)range.x;
            }
            if (range.y < val)
            {
                val = (uint)range.y;
            }

            return val;
        }
    }

    class IntPropertyRM : IntegerPropertyRM<int, int>
    {
        public IntPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && (int)range.x < (int)range.y;
        }

        protected override INotifyValueChanged<int> CreateSimpleField(out TextValueField<int> textField)
        {
            var field = new VFXLabeledField<IntegerField, int>(m_Label);
            textField = field.control;
            field.onValueDragFinished = t => ValueDragFinished();
            field.onValueDragStarted = t => ValueDragStarted();
            return field;
        }

        protected override INotifyValueChanged<int> CreateSliderField(out VFXBaseSliderField<int> slider)
        {
            var field = new VFXLabeledField<VFXIntSliderField, int>(m_Label);
            slider = field.control;
            return field;
        }

        public override int FilterValue(Vector2 range, int value)
        {
            int val = value;
            if (range.x > val)
            {
                val = (int)range.x;
            }
            if (range.y < val)
            {
                val = (int)range.y;
            }

            return val;
        }
    }

    class FloatPropertyRM : NumericPropertyRM<float, float>
    {
        public FloatPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && range.x < range.y;
        }

        protected override INotifyValueChanged<float> CreateSimpleField(out TextValueField<float> textField)
        {
            var field = new VFXLabeledField<FloatField, float>(m_Label);
            field.onValueDragFinished = t => ValueDragFinished();
            field.onValueDragStarted = t => ValueDragStarted();
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<float> CreateSliderField(out VFXBaseSliderField<float> slider)
        {
            var field = new VFXLabeledField<VFXFloatSliderField, float>(m_Label);
            slider = field.control;
            return field;
        }

        protected override void UpdateIndeterminate()
        {
            if (m_TextField != null)
            {
                (field as VFXLabeledField<FloatField, float>).indeterminate = indeterminate;
            }

            if (m_Slider != null)
                (m_Slider as VFXFloatSliderField).indeterminate = indeterminate;
        }

        public override float FilterValue(Vector2 range, float value)
        {
            float val = value;
            if (range.x > val)
            {
                val = range.x;
            }
            if (range.y < val)
            {
                val = range.y;
            }

            return val;
        }
    }
}

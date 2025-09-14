using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class NumericPropertyRM<T, U> : SimpleUIPropertyRM<T, U>
    {
        protected NumericPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            var range = m_Provider.attributes.FindRange();

            return HasValidRange(range) ? 120 : 60;
        }

        protected override void UpdateIndeterminate()
        {
            if (field is IMixedValueSupport mixedValueSupport)
            {
                mixedValueSupport.showMixedValue = indeterminate;
            }

            if (m_Slider != null)
            {
                m_Slider.isIndeterminate = indeterminate;
            }
        }

        private bool HasValidRange(Vector2 range)
        {
            return range != Vector2.zero && !float.IsPositiveInfinity(range.y) && range.x < range.y;
        }

        private VFXBaseSliderField<U> m_Slider;
        private VisualElement m_TextField;

        protected abstract VisualElement CreateSimpleField(string label);
        protected abstract VFXBaseSliderField<U> CreateSliderField(string label);
        protected abstract T FilterValue(Vector2 range, T value);

        public override INotifyValueChanged<U> CreateField()
        {
            Vector2 range = m_Provider.attributes.FindRange();
            VisualElement createdField;
            if (!HasValidRange(range))
            {
                createdField = CreateSimpleField(string.IsNullOrEmpty(provider.name) ? "  " : ObjectNames.NicifyVariableName(provider.name));
                m_TextField = createdField.Q<TextElement>(null, "unity-text-element__selectable");
                if (m_TextField != null)
                {
                    m_TextField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                    m_TextField.RegisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                }
            }
            else
            {
                m_Slider = CreateSliderField(ObjectNames.NicifyVariableName(m_Provider.name));
                m_Slider.SetOnValueDragStarted(ValueDragStarted);
                m_Slider.SetOnValueDragFinished(ValueDragFinished);
                m_Slider.RegisterCallback<BlurEvent>(OnFocusLost);
                m_Slider.range = range;
                if (IsLogarithmic() && m_Provider.attributes.FindLogarithmicBase() is var logBase and > 0)
                {
                    bool snapToPower = m_Provider.attributes.FindSnapToPower();
                    if (range.x > 0)
                    {
                        m_Slider.scale = new LogarithmicSliderScale(range, logBase, snapToPower);
                    }
                    else
                    {
                        Debug.LogWarning($"Property `{this.provider.name}`: logarithmic scale does not support range minimum value <=0\nFallback to linear scale");
                    }
                }

                createdField = m_Slider;
            }

            if (createdField.Q<Label>() is { } label)
            {
                label.RegisterCallback<PointerCaptureEvent>(x => ValueDragStarted());
                label.RegisterCallback<PointerCaptureOutEvent>(x => ValueDragFinished());
            }

            return createdField as INotifyValueChanged<U>;
        }

        bool IsLogarithmic()
        {
            return m_Provider.attributes.Is(VFXPropertyAttributes.Type.Logarithmic);
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

            return HasValidRange(range) != (m_Slider == null);
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
            var range = m_Provider.attributes.FindRange();

            if (range != Vector2.zero)
            {
                value = FilterValue(range, (T)value);
            }
            return value;
        }
    }

    class UintPropertyRM : NumericPropertyRM<uint, long>
    {
        private VFXEnumValuePopup m_EnumPopup;

        public UintPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum) ? 120 : base.GetPreferredControlWidth();
        }

        protected override uint FilterValue(Vector2 range, uint value) => (uint)Math.Clamp(value, range.x, (double)range.y);

        public override INotifyValueChanged<long> CreateField()
        {
            INotifyValueChanged<long> result;
            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum))
            {
                result = m_EnumPopup = new VFXEnumValuePopup("Value", m_Provider.attributes.FindEnum().ToList());
            }
            else
                result = base.CreateField();
            return result;
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;


            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum) == (m_EnumPopup == null))
                return false;

            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum))
            {
                string[] enumValues = m_Provider.attributes.FindEnum();

                return enumValues.SequenceEqual(m_EnumPopup.choices);
            }
            return true;
        }

        protected override VisualElement CreateSimpleField(string label)
        {
            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.BitField))
            {
                var nameLabel = new Label(label);
                nameLabel.AddToClassList("label");
                nameLabel.AddToClassList("bitfield-label");
                Insert(0, nameLabel);
                return new VFX32BitField();
            }

            return  new LongField(label);
        }

        protected override VFXBaseSliderField<long> CreateSliderField(string label) => new VFXLongSliderField(label);

        public override uint Convert(object value)
        {
            long longValue = (long)value;

            if (longValue < 0)
            {
                longValue = 0;
            }

            return (uint)longValue;
        }
    }

    class IntPropertyRM : NumericPropertyRM<int, int>
    {
        public IntPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override VisualElement CreateSimpleField(string label)
        {
            return new IntegerField(label);
        }

        protected override VFXBaseSliderField<int> CreateSliderField(string label) => new VFXIntSliderField(label);

        protected override int FilterValue(Vector2 range, int value) => (int)Math.Clamp(value, range.x, (double)range.y);
    }

    class FloatPropertyRM : NumericPropertyRM<float, float>
    {
        public FloatPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override VisualElement CreateSimpleField(string label)
        {
            return new FloatField(label);
        }

        protected override VFXBaseSliderField<float> CreateSliderField(string label) => new VFXFloatSliderField(label);

        protected override float FilterValue(Vector2 range, float value) => Math.Max(Math.Min(range.y, value), range.x);
    }
}

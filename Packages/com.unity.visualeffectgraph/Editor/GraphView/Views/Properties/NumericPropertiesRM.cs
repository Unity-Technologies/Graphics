using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;


namespace UnityEditor.VFX.UI
{
    abstract class NumericPropertyRM<T, U> : SimpleUIPropertyRM<T, U>
    {
        const string SimpleFieldControlName = "VFXSimpleFieldControlName";

        readonly VisualElement m_IndeterminateLabel;

        protected NumericPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);
        }

        public override float GetPreferredControlWidth()
        {
            var range = m_Provider.attributes.FindRange();

            return HasValidRange(range) ? 120 : 60;
        }

        protected override void UpdateIndeterminate()
        {
            if (field is VisualElement element)
            {
                if (indeterminate)
                {
                    if (m_IndeterminateLabel.parent == null)
                    {
                        element.RemoveFromHierarchy();
                        m_FieldParent.Add(m_IndeterminateLabel);
                    }
                }
                else
                {
                    if (element.parent == null)
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        m_FieldParent.Add(element);
                    }
                }
            }
        }

        private bool HasValidRange(Vector2 range)
        {
            return range != Vector2.zero && !float.IsPositiveInfinity(range.y) && range.x < range.y;
        }

        private VFXBaseSliderField<U> m_Slider;
        private TextValueField<U> m_TextField;

        protected abstract VisualElement CreateSimpleField(string controlName);
        protected abstract (VisualElement, VFXBaseSliderField<U>) CreateSliderField();
        protected abstract T FilterValue(Vector2 range, T value);

        public override INotifyValueChanged<U> CreateField()
        {
            Vector2 range = m_Provider.attributes.FindRange();
            VisualElement result;
            if (!HasValidRange(range))
            {
                result = CreateSimpleField(SimpleFieldControlName);
                if (result is IVFXDraggedElement draggedElement)
                {
                    draggedElement.SetOnValueDragFinished(ValueDragStarted);
                    draggedElement.SetOnValueDragFinished(ValueDragFinished);
                }

                m_TextField = result.Q(SimpleFieldControlName) as TextValueField<U>;
                if (m_TextField != null)
                {
                    m_TextField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                    m_TextField.RegisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                }
            }
            else
            {
                (result, m_Slider) = CreateSliderField();
                m_Slider.SetOnValueDragFinished(ValueDragFinished);
                m_Slider.SetOnValueDragStarted(ValueDragStarted);
                m_Slider.RegisterCallback<BlurEvent>(OnFocusLost);
                m_Slider.range = range;
                if (IsLogarithmic() && m_Provider.attributes.FindLogarithmicBase() is float logBase && logBase > 0)
                {
                    bool snapToPower = m_Provider.attributes.FindSnapToPower();
                    if (range.x > 0)
                    {
                        m_Slider.scale = new LogarithmicSliderScale(range, logBase, snapToPower);
                    }
                    else
                    {
                        Debug.LogWarning($"Property `{this.m_Label.text}`: logarithmic scale does not support range minimum value <=0\nFallback to linear scale");
                    }
                }
            }

            return result as INotifyValueChanged<U>;
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

        protected void ValueDragFinished(IVFXDraggedElement draggedElement)
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted(IVFXDraggedElement draggedElement)
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

            return HasValidRange(range) != (m_Slider == null);
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
        private VFX32BitField m_BitField;

        public UintPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum) ? 120 : base.GetPreferredControlWidth();
        }

        protected override uint FilterValue(Vector2 range, uint value) => (uint)Math.Max(Math.Min(range.y, value), range.x);

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

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;


            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum) == (m_EnumPopup == null))
                return false;

            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Enum))
            {
                string[] enumValues = m_Provider.attributes.FindEnum();

                return enumValues.SequenceEqual(m_EnumPopup.enumValues);
            }
            return true;
        }

        protected override VisualElement CreateSimpleField(string controlName)
        {
            return m_Provider.attributes.Is(VFXPropertyAttributes.Type.BitField)
                ? new VFXLabeledField<VFX32BitField, long>(m_Label, controlName)
                : new VFXLabeledField<LongField, long>(m_Label, controlName);
        }

        protected override (VisualElement, VFXBaseSliderField<long>) CreateSliderField()
        {
            var labelField = new VFXLabeledField<VFXLongSliderField, long>(m_Label);
            return (labelField, labelField.control);
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
    }

    class IntPropertyRM : NumericPropertyRM<int, int>
    {
        public IntPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override VisualElement CreateSimpleField(string controlName)
        {
            return new VFXLabeledField<IntegerField, int>(m_Label, controlName);
        }

        protected override (VisualElement, VFXBaseSliderField<int>) CreateSliderField()
        {
            var labelField = new VFXLabeledField<VFXIntSliderField, int>(m_Label);
            return (labelField, labelField.control);
        }

        protected override int FilterValue(Vector2 range, int value) => (int)Math.Max(Math.Min(range.y, value), range.x);
    }

    class FloatPropertyRM : NumericPropertyRM<float, float>
    {
        public FloatPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override VisualElement CreateSimpleField(string controlName)
        {
            return new VFXLabeledField<FloatField, float>(m_Label, controlName);
        }

        protected override (VisualElement, VFXBaseSliderField<float>) CreateSliderField()
        {
            var labelField = new VFXLabeledField<VFXFloatSliderField, float>(m_Label);
            return (labelField, labelField.control);
        }

        protected override float FilterValue(Vector2 range, float value) => Math.Max(Math.Min(range.y, value), range.x);
    }
}

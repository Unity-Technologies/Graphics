using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;

using CurveField = UnityEditor.VFX.UIElements.LabeledField<UnityEditor.Experimental.UIElements.CurveField, UnityEngine.AnimationCurve>;

using Vector3Field = UnityEditor.VFX.UIElements.Vector3Field;
using Vector2Field = UnityEditor.VFX.UIElements.Vector2Field;
using Vector4Field = UnityEditor.VFX.UIElements.Vector4Field;
using FloatField = UnityEditor.Experimental.UIElements.FloatField;

namespace UnityEditor.VFX
{
    interface IStringProvider
    {
        string[] GetAvailableString();
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class StringProviderAttribute : PropertyAttribute
    {
        public StringProviderAttribute(Type providerType)
        {
            if (!typeof(IStringProvider).IsAssignableFrom(providerType))
                throw new InvalidCastException("StringProviderAttribute excepts a type which implements interface IStringProvider : " + providerType);
            this.providerType = providerType;
        }

        public Type providerType { get; private set; }
    }

    interface IPushButtonBehavior
    {
        void OnClicked(string currentValue);
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class PushButtonAttribute : PropertyAttribute
    {
        public PushButtonAttribute(Type pushButtonProvider, string buttonName)
        {
            if (!typeof(IPushButtonBehavior).IsAssignableFrom(pushButtonProvider))
                throw new InvalidCastException("PushButtonAttribute excepts a type which implements interface IPushButtonBehavior : " + pushButtonProvider);
            this.pushButtonProvider = pushButtonProvider;
            this.buttonName = buttonName;
        }

        public Type pushButtonProvider { get; private set; }
        public string buttonName { get; private set; }
    }
}

namespace UnityEditor.VFX.UI
{
    class UintPropertyRM : SimpleUIPropertyRM<uint, long>
    {
        public UintPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 60;
        }

        public override INotifyValueChanged<long> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(VFXPropertyAttribute.Create(m_Provider.customAttributes));
            if (range == Vector2.zero || range.y == Mathf.Infinity)
            {
                var field = new LabeledField<IntegerField, long>(m_Label);
                return field;
            }
            else
            {
                range.x = Mathf.Max(0, Mathf.Round(range.x));
                range.y = Mathf.Max(range.x + 1, Mathf.Round(range.y));

                var field = new LabeledField<IntSliderField, long>(m_Label);
                field.control.range = range;
                return field;
            }
        }

        protected override bool HasFocus()
        {
            if (field is LabeledField<IntegerField, long>)
                return (field as LabeledField<IntegerField, long>).control.hasFocus;
            return (field as LabeledField<IntSliderField, long>).control.hasFocus;
        }

        public override object FilterValue(object value)
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            if (range != Vector2.zero && (range.y == Mathf.Infinity || (uint)range.x < (uint)range.y))
            {
                uint val = (uint)value;

                if (range.x > val)
                {
                    val = (uint)range.x;
                }
                if (range.y < val)
                {
                    val = (uint)range.y;
                }

                value = val;
            }

            return value;
        }
    }

    class IntPropertyRM : SimpleUIPropertyRM<int, long>
    {
        public IntPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<long> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(VFXPropertyAttribute.Create(m_Provider.customAttributes));
            if (range != Vector2.zero && (range.y == Mathf.Infinity || (uint)range.x < (uint)range.y))
            {
                var field = new LabeledField<IntegerField, long>(m_Label);
                return field;
            }
            else
            {
                range.x = Mathf.Round(range.x);
                range.y = Mathf.Max(range.x + 1, Mathf.Round(range.y));

                var field = new LabeledField<IntSliderField, long>(m_Label);
                field.control.range = range;
                return field;
            }
        }

        protected override bool HasFocus()
        {
            if (field is LabeledField<IntegerField, long>)
                return (field as LabeledField<IntegerField, long>).control.hasFocus;
            return (field as LabeledField<IntSliderField, long>).control.hasFocus;
        }

        public override object FilterValue(object value)
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            if (range != Vector2.zero && (int)range.x < (int)range.y)
            {
                int val = (int)value;

                if (range.x > val)
                {
                    val = (int)range.x;
                }
                if (range.y < val)
                {
                    val = (int)range.y;
                }

                value = val;
            }

            return value;
        }

        public override float GetPreferredControlWidth()
        {
            return 60;
        }
    }
    class EnumPropertyRM : SimplePropertyRM<int>
    {
        public EnumPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public override ValueControl<int> CreateField()
        {
            return new EnumField(m_Label, m_Provider.portType);
        }
    }

    class FloatPropertyRM : SimpleUIPropertyRM<float, float>
    {
        public FloatPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<float> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            if (range == Vector2.zero || range.y == Mathf.Infinity)
            {
                var field = new LabeledField<FloatField, float>(m_Label);
                return field;
            }
            else
            {
                var field = new LabeledField<DoubleSliderField, float>(m_Label);
                field.control.range = range;
                return field;
            }
        }

        protected override bool HasFocus()
        {
            if (field is LabeledField<FloatField, float>)
                return (field as LabeledField<FloatField, float>).control.hasFocus;
            return (field as LabeledField<DoubleSliderField, float>).control.hasFocus;
        }

        public override object FilterValue(object value)
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            if (range != Vector2.zero && range.x < range.y)
            {
                float val = (float)value;

                if (range.x > val)
                {
                    val = range.x;
                }
                if (range.y < val)
                {
                    val = range.y;
                }

                value = val;
            }

            return value;
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }

    class Vector4PropertyRM : SimpleUIPropertyRM<Vector4, Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<Vector4> CreateField()
        {
            var field = new LabeledField<Vector4Field, Vector4>(m_Label);

            return field;
        }

        public override float GetPreferredControlWidth()
        {
            return 260;
        }
    }

    class Matrix4x4PropertyRM : SimpleUIPropertyRM<Matrix4x4, Matrix4x4>
    {
        public Matrix4x4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<Matrix4x4> CreateField()
        {
            var field = new LabeledField<Matrix4x4Field, Matrix4x4>(m_Label);

            return field;
        }

        public override float GetPreferredControlWidth()
        {
            return 260;
        }
    }

    class Vector2PropertyRM : SimpleUIPropertyRM<Vector2, Vector2>
    {
        public Vector2PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<Vector2> CreateField()
        {
            var field = new LabeledField<Vector2Field, Vector2>(m_Label);

            return field;
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }

    class FlipBookPropertyRM : SimpleUIPropertyRM<FlipBook, FlipBook>
    {
        public FlipBookPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<FlipBook> CreateField()
        {
            var field = new LabeledField<FlipBookField, FlipBook>(m_Label);

            return field;
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }

    class StringPropertyRM : SimplePropertyRM<string>
    {
        public StringPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 140;
        }

        public static Func<string[]> FindStringProvider(object[] customAttributes)
        {
            if (customAttributes != null)
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute is StringProviderAttribute)
                    {
                        var instance = Activator.CreateInstance((attribute as StringProviderAttribute).providerType);
                        var stringProvider = instance as IStringProvider;
                        return () => stringProvider.GetAvailableString();
                    }
                }
            }
            return null;
        }

        public struct StringPushButtonInfo
        {
            public Action<string> action;
            public string buttonName;
        }

        public static StringPushButtonInfo FindPushButtonBehavior(object[] customAttributes)
        {
            if (customAttributes != null)
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute is PushButtonAttribute)
                    {
                        var instance = Activator.CreateInstance((attribute as PushButtonAttribute).pushButtonProvider);
                        var pushButtonBehavior = instance as IPushButtonBehavior;
                        return new StringPushButtonInfo() {action = (a) => pushButtonBehavior.OnClicked(a), buttonName = (attribute as PushButtonAttribute).buttonName};
                    }
                }
            }
            return new StringPushButtonInfo();
        }

        public override ValueControl<string> CreateField()
        {
            var stringProvider = FindStringProvider(m_Provider.customAttributes);
            var pushButtonProvider = FindPushButtonBehavior(m_Provider.customAttributes);
            if (stringProvider != null)
            {
                return new StringFieldProvider(m_Label, stringProvider);
            }
            else if (pushButtonProvider.action != null)
            {
                return new StringFieldPushButton(m_Label, pushButtonProvider.action, pushButtonProvider.buttonName);
            }
            else
            {
                return new StringField(m_Label);
            }
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            var stringProvider = FindStringProvider(m_Provider.customAttributes);
            var pushButtonInfo = FindPushButtonBehavior(m_Provider.customAttributes);

            if (stringProvider != null)
            {
                return m_Field is StringFieldProvider && (m_Field as StringFieldProvider).stringProvider == stringProvider;
            }
            else if (pushButtonInfo.action != null)
            {
                return m_Field is StringFieldPushButton && (m_Field as StringFieldPushButton).pushButtonProvider == pushButtonInfo.action;
            }

            return !(m_Field is StringFieldProvider) && !(m_Field is StringFieldPushButton);
        }
    }
}

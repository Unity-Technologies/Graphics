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
        public PushButtonAttribute(Type pushButtonProvider)
        {
            if (!typeof(IPushButtonBehavior).IsAssignableFrom(pushButtonProvider))
                throw new InvalidCastException("PushButtonAttribute excepts a type which implements interface IPushButtonBehavior : " + pushButtonProvider);
            this.pushButtonProvider = pushButtonProvider;
        }

        public Type pushButtonProvider { get; private set; }
    }
}

namespace UnityEditor.VFX.UI
{
    class UintPropertyRM : SimplePropertyRM<uint>
    {
        public UintPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<uint> CreateField()
        {
            return new UintField(m_Label);
        }
    }

    class IntPropertyRM : SimplePropertyRM<int>
    {
        public IntPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<int> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(VFXPropertyAttribute.Create(m_Provider.customAttributes));
            /*
            if (range == Vector2.zero)
                return new IntField(m_Label);
            else*/
            range.x = 0;
            range.y = 10;
            return new IntSliderField(m_Label, range);
        }
    }
    class EnumPropertyRM : SimplePropertyRM<int>
    {
        public EnumPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<int> CreateField()
        {
            return new EnumField(m_Label, m_Provider.anchorType);
        }
    }

    class FloatPropertyRM : SimpleUIPropertyRM<float, double>
    {
        public FloatPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override INotifyValueChanged<double> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(VFXPropertyAttribute.Create(m_Provider.customAttributes));

            if (range == Vector2.zero)
            {
                var field = new LabeledField<DoubleField, double>(m_Label);
                field.control.dynamicUpdate = true;
                return field;
            }
            else
            {
                var field = new LabeledField<DoubleSliderField, double>(m_Label);

                return field;
            }
        }
    }

    class Vector4PropertyRM : SimpleUIPropertyRM<Vector4, Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override INotifyValueChanged<Vector4> CreateField()
        {
            var field = new LabeledField<Vector4Field, Vector4>(m_Label);
            field.control.dynamicUpdate = true;

            return field;
        }
    }

    class Vector3PropertyRM : SimpleUIPropertyRM<Vector3, Vector3>
    {
        public Vector3PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override INotifyValueChanged<Vector3> CreateField()
        {
            var field = new LabeledField<Vector3Field, Vector3>(m_Label);

            field.control.dynamicUpdate = true;

            return field;
        }
    }

    class Vector2PropertyRM : SimpleUIPropertyRM<Vector2, Vector2>
    {
        public Vector2PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override INotifyValueChanged<Vector2> CreateField()
        {
            var field = new LabeledField<Vector2Field, Vector2>(m_Label);

            field.control.dynamicUpdate = true;

            return field;
        }
    }

    class StringPropertyRM : SimplePropertyRM<string>
    {
        public StringPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
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

        public static Action<string> FindPushButtonBehavior(object[] customAttributes)
        {
            if (customAttributes != null)
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute is PushButtonAttribute)
                    {
                        var instance = Activator.CreateInstance((attribute as PushButtonAttribute).pushButtonProvider);
                        var pushButtonBehavior = instance as IPushButtonBehavior;
                        return (a) => pushButtonBehavior.OnClicked(a);
                    }
                }
            }
            return null;
        }

        public override ValueControl<string> CreateField()
        {
            var stringProvider = FindStringProvider(m_Provider.customAttributes);
            var pushButtonProvider = FindPushButtonBehavior(m_Provider.customAttributes);
            if (stringProvider != null)
            {
                return new StringFieldProvider(m_Label, stringProvider);
            }
            else if (pushButtonProvider != null)
            {
                return new StringFieldPushButton(m_Label, pushButtonProvider);
            }
            else
            {
                return new StringField(m_Label);
            }
        }
    }
}

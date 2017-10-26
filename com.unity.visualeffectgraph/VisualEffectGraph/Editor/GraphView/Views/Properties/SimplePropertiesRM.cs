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

            if (range == Vector2.zero)
                return new IntField(m_Label);
            else
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

    class FloatPropertyRM : SimplePropertyRM<float>
    {
        public FloatPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<float> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(VFXPropertyAttribute.Create(m_Provider.customAttributes));

            if (range == Vector2.zero)
                return new FloatField(m_Label);
            else
                return new SliderField(m_Label, range);
        }
    }

    class Vector4PropertyRM : SimplePropertyRM<Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<Vector4> CreateField()
        {
            return new Vector4Field(m_Label);
        }
    }

    class Vector3PropertyRM : SimplePropertyRM<Vector3>
    {
        public Vector3PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<Vector3> CreateField()
        {
            return new Vector3Field(m_Label);
        }
    }

    class Vector2PropertyRM : SimplePropertyRM<Vector2>
    {
        public Vector2PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<Vector2> CreateField()
        {
            return new Vector2Field(m_Label);
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

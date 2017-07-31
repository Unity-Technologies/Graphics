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

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class StringProviderAttribute : PropertyAttribute
    {
        public StringProviderAttribute(Type providerType)
        {
            m_ProviderType = providerType;
        }

        public Type m_ProviderType;
    }
}

namespace UnityEditor.VFX.UI
{
    class IntPropertyRM : SimplePropertyRM<int>
    {
        public IntPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<int> CreateField()
        {
            return new IntField(m_Label);
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

    class CurvePropertyRM : SimplePropertyRM<AnimationCurve>
    {
        public CurvePropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
        }

        public override ValueControl<AnimationCurve> CreateField()
        {
            return new CurveField(m_Label);
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
                        var instance = Activator.CreateInstance((attribute as StringProviderAttribute).m_ProviderType);
                        if (instance is IStringProvider)
                        {
                            var stringProvider = instance as IStringProvider;
                            return () => stringProvider.GetAvailableString();
                        }
                    }
                }
            }
            return null;
        }

        public override ValueControl<string> CreateField()
        {
            var stringProvider = FindStringProvider(m_Provider.customAttributes);
            if (stringProvider != null)
            {
                return new StringFieldProvider(m_Label, stringProvider);
            }
            else
            {
                return new StringField(m_Label);
            }
        }
    }
}

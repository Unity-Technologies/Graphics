using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.VFX.UIElements;

using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;

namespace UnityEditor.VFX
{
    interface IStringProvider
    {
        string[] GetAvailableString();
    }

    interface IVFXModelStringProvider
    {
        string[] GetAvailableString(VFXModel model);
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class StringProviderAttribute : PropertyAttribute
    {
        public StringProviderAttribute(Type providerType)
        {
            if (!typeof(IStringProvider).IsAssignableFrom(providerType) && !typeof(IVFXModelStringProvider).IsAssignableFrom(providerType))
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
    class PushButtonAttribute : PropertyAttribute
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
    class StringPropertyRM : SimplePropertyRM<string>
    {
        public StringPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 140;
        }

        public static Func<string[]> FindStringProvider(VFXModel model, object[] customAttributes)
        {
            if (customAttributes != null)
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute is StringProviderAttribute)
                    {
                        var instance = Activator.CreateInstance((attribute as StringProviderAttribute).providerType);
                        if (instance is IStringProvider stringProvider)
                        {
                            return () => stringProvider.GetAvailableString();
                        }
                        else if (model != null && instance is IVFXModelStringProvider modelStringProvider)
                        {
                            return () => modelStringProvider.GetAvailableString(model);
                        }
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
                        return new StringPushButtonInfo() { action = (a) => pushButtonBehavior.OnClicked(a), buttonName = (attribute as PushButtonAttribute).buttonName };
                    }
                }
            }
            return new StringPushButtonInfo();
        }

        VFXStringField m_StringField;
        VFXStringFieldPushButton m_StringFieldPushButton;

        VFXStringFieldProvider m_StringFieldProvider;


        protected override void UpdateIndeterminate()
        {
            if (m_StringField != null)
            {
                m_StringField.indeterminate = indeterminate;
            }
        }

        public override ValueControl<string> CreateField()
        {
            var stringProvider = FindStringProvider(null, m_Provider.customAttributes);
            var pushButtonProvider = FindPushButtonBehavior(m_Provider.customAttributes);
            var label = new Label(ObjectNames.NicifyVariableName(provider.name));

            if (stringProvider != null)
            {
                m_StringFieldProvider = new VFXStringFieldProvider(label, stringProvider);
                return m_StringFieldProvider;
            }
            else if (pushButtonProvider.action != null)
            {
                m_StringFieldPushButton = new VFXStringFieldPushButton(label, pushButtonProvider.action, pushButtonProvider.buttonName);
                if (isDelayed)
                {
                    VisualElement input = m_StringFieldPushButton.textfield.Q("unity-text-input");
                    input.RegisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                    input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                }
                return m_StringFieldPushButton;
            }
            else
            {
                m_StringField = new VFXStringField(label);
                if (isDelayed)
                {
                    VisualElement input = m_StringField.textfield.Q("unity-text-input");
                    input.RegisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                    input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                }
                return m_StringField;
            }
        }

        public override bool isDelayed
        {
            get => base.isDelayed;

            set
            {
                if (base.isDelayed != value)
                {
                    base.isDelayed = value;

                    if (m_StringField != null)
                    {
                        VisualElement input = m_StringField.textfield.Q("unity-text-input");
                        if (isDelayed)
                        {
                            input.RegisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                            input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                        }
                        else
                        {
                            input.UnregisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                            input.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                        }
                    }

                    if (m_StringFieldPushButton != null)
                    {
                        VisualElement input = m_StringFieldPushButton.textfield.Q("unity-text-input");
                        if (isDelayed)
                        {
                            input.RegisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                            input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                        }
                        else
                        {
                            input.UnregisterCallback<BlurEvent>(OnFocusLost, TrickleDown.TrickleDown);
                            input.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                        }
                    }
                }
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.character == '\n')
            {
                if (isDelayed && hasChangeDelayed)
                {
                    NotifyValueChanged();
                }
                UpdateGUI(true);
            }
        }

        void OnFocusLost(BlurEvent e)
        {
            if (isDelayed && hasChangeDelayed)
            {
                NotifyValueChanged();
            }
            UpdateGUI(true);
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            var stringProvider = FindStringProvider(null, m_Provider.customAttributes);
            var pushButtonInfo = FindPushButtonBehavior(m_Provider.customAttributes);

            if (stringProvider != null)
            {
                return m_Field is VFXStringFieldProvider vfxStringFieldProvider && vfxStringFieldProvider.stringProvider == stringProvider;
            }
            else if (pushButtonInfo.action != null)
            {
                return m_Field is VFXStringFieldPushButton vfxStringFieldPushButton && vfxStringFieldPushButton.pushButtonProvider == pushButtonInfo.action;
            }

            return !(m_Field is VFXStringFieldProvider) && !(m_Field is VFXStringFieldPushButton);
        }
    }
}

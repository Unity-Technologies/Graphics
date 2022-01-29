using System;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    #if UNITY_EDITOR
    public class ColorField : UnityEditor.UIElements.ColorField
    {
        public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> { }
    }
    #else
    class ColorFieldChannel
    {
        private readonly VisualElement m_MainElement;
        private string m_ChannelName;

        private readonly Slider m_Slider;
        private readonly FloatField m_FloatField;

        public event Action<float> onChannelChanged;
        public ColorFieldChannel(VisualElement element, string channelName)
        {
            m_ChannelName = channelName;
            m_MainElement = element.Q<VisualElement>(channelName);
            m_Slider = m_MainElement.Q<Slider>($"{channelName}Slider");
            m_Slider.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                m_FloatField.value = evt.newValue;
                onChannelChanged?.Invoke(evt.newValue);
            });
            m_FloatField = m_MainElement.Q<FloatField>($"{channelName}FloatField");
            m_FloatField.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                m_FloatField.value = Mathf.Clamp(evt.newValue, m_Slider.lowValue, m_Slider.highValue);
                m_Slider.value = evt.newValue;
                onChannelChanged?.Invoke(evt.newValue);
            });
        }

        public void SetValue(float channelValue)
        {
            m_Slider.value = channelValue;
            m_FloatField.value = channelValue;
        }
    }

    public class ColorField : BaseField<Color>
    {
        public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> { }

        private readonly VisualElement m_MainElement;
        private readonly ColorFieldChannel m_RedChannel;
        private readonly ColorFieldChannel m_GreenChannel;
        private readonly ColorFieldChannel m_BlueChannel;
        private readonly ColorFieldChannel m_AlphaChannel;

        public ColorField(string label)
            : base(label, null)
        {
            var visualTreeAsset = Resources.Load<VisualTreeAsset>(nameof(ColorField));
            m_MainElement = visualTreeAsset.Instantiate();
            m_RedChannel = new ColorFieldChannel(m_MainElement,"Red");
            m_RedChannel.onChannelChanged += r => value = new Color(r, value.g, value.b, value.a);
            m_GreenChannel = new ColorFieldChannel(m_MainElement,"Green");
            m_GreenChannel.onChannelChanged += g => value = new Color(value.r, g, value.b, value.a);
            m_BlueChannel = new ColorFieldChannel(m_MainElement,"Blue");
            m_BlueChannel.onChannelChanged += b => value = new Color(value.r, value.g, b, value.a);
            m_AlphaChannel = new ColorFieldChannel(m_MainElement,"Alpha");
            m_AlphaChannel.onChannelChanged += a => value = new Color(value.r, value.g, value.b, a);

            this.Q<VisualElement>(className:"unity-base-field__input").Add(m_MainElement);

            this.RegisterCallback<ChangeEvent<Color>>(evt =>
            {
                m_RedChannel.SetValue(evt.newValue.r);
                m_GreenChannel.SetValue(evt.newValue.g);
                m_BlueChannel.SetValue(evt.newValue.b);
                m_AlphaChannel.SetValue(evt.newValue.a);
            });
        }

        public ColorField()
            : this(null)
        {
        }
    }
    #endif
}

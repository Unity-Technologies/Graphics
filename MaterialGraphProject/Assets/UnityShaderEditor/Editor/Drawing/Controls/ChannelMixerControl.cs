using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChannelMixerControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        float m_Minimum;
        float m_Maximum;

        public ChannelMixerControlAttribute(string label = null, float minimum = -2f, float maximum = 2f)
        {
            m_Label = label;
            m_Minimum = minimum;
            m_Maximum = maximum;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ChannelMixerControlView(m_Label, m_Minimum, m_Maximum, node, propertyInfo);
        }
    }

    public class ChannelMixerControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        int m_OutChannel;

        Slider redSlider;
        Slider greenSlider;
        Slider blueSlider;

        float m_Minimum;
        float m_Maximum;

        public ChannelMixerControlView(string label, float minimum, float maximum, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_OutChannel = 0;

            m_Minimum = minimum;
            m_Maximum = maximum;

            if (propertyInfo.PropertyType != typeof(ChannelMixerNode.ChannelMixer))
                throw new ArgumentException("Property must be of type Channel Mixer.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            Action changedOutputRed = () => OnClickButton(0);
            var outputButtonRed = new Button(changedOutputRed);
            Add(outputButtonRed);

            Action changedOutputGreen = () => OnClickButton(1);
            var outputButtonGreen = new Button(changedOutputGreen);
            Add(outputButtonGreen);

            Action changedOutputBlue = () => OnClickButton(2);
            var outputButtonBlue = new Button(changedOutputBlue);
            Add(outputButtonBlue);

            ChannelMixerNode.ChannelMixer channelMixer = (ChannelMixerNode.ChannelMixer)m_PropertyInfo.GetValue(m_Node, null);

            Add(new Label("Red"));
            Action<float> changedRedIn = (s) => { OnChangeSlider(s, m_OutChannel, 0); };
            redSlider = new Slider(m_Minimum, m_Maximum, changedRedIn) { value = channelMixer.outChannels[m_OutChannel].inChannels[0] };
            redSlider.value = 1; // This refuses to initialize at its default value
            Add(redSlider);

            Add(new Label("Green"));
            Action<float> changedGreenIn = (s) => { OnChangeSlider(s, m_OutChannel, 1); };
            greenSlider = new Slider(m_Minimum, m_Maximum, changedGreenIn) { value = channelMixer.outChannels[m_OutChannel].inChannels[1] };
            Add(greenSlider);

            Add(new Label("Blue"));
            Action<float> changedBlueIn = (s) => { OnChangeSlider(s, m_OutChannel, 2); };
            blueSlider = new Slider(m_Minimum, m_Maximum, changedBlueIn) { value = channelMixer.outChannels[m_OutChannel].inChannels[2] };
            Add(blueSlider);

            ResetSliders();
        }

        void ResetSliders()
        {
            ChannelMixerNode.ChannelMixer channelMixer = (ChannelMixerNode.ChannelMixer)m_PropertyInfo.GetValue(m_Node, null);
            redSlider.value = channelMixer.outChannels[m_OutChannel].inChannels[0];
            greenSlider.value = channelMixer.outChannels[m_OutChannel].inChannels[1];
            blueSlider.value = channelMixer.outChannels[m_OutChannel].inChannels[2];
        }

        void OnChangeSlider(float value, int outChannel, int inChannel)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Slider Change");
            ChannelMixerNode.ChannelMixer channelMixer = (ChannelMixerNode.ChannelMixer)m_PropertyInfo.GetValue(m_Node, null);
            channelMixer.outChannels[outChannel].inChannels[inChannel] = value;
            m_PropertyInfo.SetValue(m_Node, channelMixer, null);
            Dirty(ChangeType.Repaint);
        }

        void OnClickButton(int outChannel)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Button Change");
            m_OutChannel = outChannel;
            ResetSliders();
            Dirty(ChangeType.Repaint);
        }
    }
}

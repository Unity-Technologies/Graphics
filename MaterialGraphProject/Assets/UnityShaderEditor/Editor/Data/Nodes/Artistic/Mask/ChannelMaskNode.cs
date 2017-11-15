using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum TextureChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

    [Title("Artistic/Mask/Channel Mask")]
    public class ChannelMaskNode : CodeFunctionNode
    {
        public ChannelMaskNode()
        {
            name = "Channel Mask";
        }

        [SerializeField]
        private TextureChannel m_Channel = TextureChannel.Red;

        [ChannelEnumControl("Channel")]
        public TextureChannel channel
        {
            get { return m_Channel; }
            set
            {
                if (m_Channel == value)
                    return;

                m_Channel = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_Channel)
            {
                case TextureChannel.Green:
                    return GetType().GetMethod("Unity_ChannelMask_Green", BindingFlags.Static | BindingFlags.NonPublic);
                case TextureChannel.Blue:
                    return GetType().GetMethod("Unity_ChannelMask_Blue", BindingFlags.Static | BindingFlags.NonPublic);
                case TextureChannel.Alpha:
                    return GetType().GetMethod("Unity_ChannelMask_Alpha", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_ChannelMask_Red", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_ChannelMask_Red(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    Out = In.xxxx;
}";
        }

        static string Unity_ChannelMask_Green(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    Out = In.yyyy;
}";
        }

        static string Unity_ChannelMask_Blue(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    Out = In.zzzz;
}";
        }

        static string Unity_ChannelMask_Alpha(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    Out = In.wwww;
}";
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class NewHueNode : IShaderNodeType
    {
        InputPortRef m_InPort;
        InputPortRef m_OffsetPort;
        OutputPortRef m_OutPort;

        public void Setup(ref NodeSetupContext context)
        {
            m_InPort = context.CreateInputPort(0, "In", PortValue.Vector3());
            m_OffsetPort = context.CreateInputPort(1, "Offset", PortValue.Vector1(0.5f));
            m_OutPort = context.CreateOutputPort(2, "Out", PortValueType.Vector3);
            var type = new NodeTypeDescriptor
            {
                path = "Artistic/Adjustment",
                name = "New Hue",
                inputs = new List<InputPortRef> { m_InPort, m_OffsetPort },
                outputs = new List<OutputPortRef> { m_OutPort }
            };
            context.CreateType(type);
        }

        HlslSourceRef m_Source;

        public void OnChange(ref NodeTypeChangeContext context)
        {
            // TODO: Figure out what should cause the user to create the hlsl source
            // TODO: How does sharing files between multiple node types work?
            if (!m_Source.isValid)
            {
                m_Source = context.CreateHlslSource("Packages/com.unity.shadergraph/Editor/Data/Nodes/Artistic/Adjustment/HueNode.hlsl");
            }

            foreach (var node in context.addedNodes)
            {
                var data = (HueData) context.GetData(node);
                if (data == null)
                {
                    data = new HueData { offsetFactor = 1f };
                    context.SetData(node, data);
                }

                data.offsetFactorControl = context.CreateControl(node, "Offset Factor", data.offsetFactor);
                data.offsetFactorValue = context.CreateHlslValue(data.offsetFactor);

                context.SetHlslFunction(node, new HlslFunctionDescriptor
                {
                    source = m_Source,
                    name = "Unity_Hue",
                    arguments = new HlslArgumentList { m_InPort, m_OffsetPort, data.offsetFactorValue },
                    returnValue = m_OutPort
                });
            }

            foreach (var node in context.modifiedNodes)
            {
                var data = (HueData) context.GetData(node);
                if (context.WasControlModified(data.offsetFactorControl))
                {
                    data.offsetFactor = context.GetControlValue(data.offsetFactorControl);
                    context.SetHlslValue(data.offsetFactorValue, data.offsetFactor);
                }
            }
        }

//        float GetOffsetFactor(HueData data)
//        {
//            return data.mode == HueMode.Degrees ? 1 / 360f : 1;
//        }
    }

    [Serializable]
    class HueData
    {
//        public HueMode mode;
        public float offsetFactor;

        [NonSerialized]
//        public HlslValueRef modeValue;
        public HlslValueRef offsetFactorValue;

        [NonSerialized]
        public ControlRef offsetFactorControl;
    }

// already defined in old HueNode file
//    enum HueMode
//    {
//        Degrees,
//        Normalized
//    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class NewHueNode : IShaderNode
    {
        PortRef m_InPort;
        PortRef m_OffsetPort;
        PortRef m_OutPort;

        public void Setup(ref NodeSetupContext context)
        {
            m_InPort = context.CreateInputPort(0, "In", PortValue.Vector3());
            m_OffsetPort = context.CreateInputPort(1, "Offset", PortValue.Vector1(0.5f));
            m_OutPort = context.CreateOutputPort(2, "Out", PortValueType.Vector3);
            var type = new NodeTypeDescriptor
            {
                path = "Artistic/Adjustment",
                name = "New Hue",
                inputs = new List<PortRef> { m_InPort, m_OffsetPort },
                outputs = new List<PortRef> { m_OutPort }
            };
            context.CreateType(type);
        }

        HlslSourceRef m_Source;

        public void OnChange(ref NodeChangeContext context)
        {
            Debug.Log("OnChange");
            // TODO: Figure out what should cause the user to create the hlsl source
            // TODO: How does sharing files between multiple node types work?
            if (!m_Source.isValid)
            {
                m_Source = context.CreateHlslSource("Packages/com.unity.shadergraph/Editor/Data/Nodes/Artistic/Adjustment/HueNode.hlsl");
            }

            foreach (var node in context.createdNodes.Concat(context.deserializedNodes))
            {
                Debug.Log("setting node dataaaa");
//                var data = new HueData();
//                data.modeValue = context.CreateHlslValue(GetOffsetFactor(data));
                context.SetHlslFunction(node, new HlslFunctionDescriptor
                {
                    source = m_Source,
                    name = "Unity_Hue",
                    arguments = new HlslArgumentList { m_InPort, m_OffsetPort, 1/360f },
                    returnValue = m_OutPort
                });
//                context.SetData(node, data);
            }

            // Later on
            // context.SetHlslValue(data.modeValue, GetOffsetFactor(data));
        }

        float GetOffsetFactor(HueData data)
        {
            return data.mode == HueMode.Degrees ? 1 / 360f : 1;
        }
    }

    [Serializable]
    class HueData
    {
        public HueMode mode;

        [NonSerialized]
        public HlslValueRef modeValue;
    }

// already defined in old HueNode file
//    enum HueMode
//    {
//        Degrees,
//        Normalized
//    }
}

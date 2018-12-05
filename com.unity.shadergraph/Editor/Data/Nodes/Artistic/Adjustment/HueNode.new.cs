using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class NewHueNode : ShaderNodeType
    {
        // Name can be specified or generated from variable name (strips `m_` prefix and `Port` suffix)
        // We can get rid of ID by using variable name instead. Similar to Unity serialization, you will need an
        // attribute referring to the old name if you do a rename, but I think that's nicer than IDs.
        [Vector3Port]
        public InputPort inPort;

        // The default port value can be specified, but otherwise it defaults to zero.
        [Vector1Port(0.5f)]
        public InputPort offsetPort;

        // Question: Output ports cannot have values, so should we just ignore the value, or have separate attributes
        // for input and outputs?
        [Vector3Port]
        public OutputPort outPort;

        public override void Setup(NodeSetupContext context)
        {
            var type = new NodeTypeDescriptor
            {
                path = "Artistic/Adjustment",
                name = "New Hue",
                inputs = new List<InputPort> { inPort, offsetPort },
                outputs = new List<OutputPort> { outPort }
            };
            context.CreateType(type);
        }

        public override void OnNodeAdded(NodeChangeContext context, ShaderNode node)
        {
            var data = (HueData) node.data;
            if (data == null)
            {
                data = new HueData { offsetFactor = 1f };
                node.data = data;
            }

            data.offsetFactorControl = context.CreateControl(node, "Offset Factor", data.offsetFactor);
            data.offsetFactorValue = context.CreateHlslValue(data.offsetFactor);

            context.SetHlslFunction(node, new HlslFunctionDescriptor
            {
                source = HlslSource.File("Packages/com.unity.shadergraph/Editor/Data/Nodes/Artistic/Adjustment/HueNode.hlsl"),
                name = "Unity_Hue",
                arguments = new HlslArgumentList { inPort, offsetPort, data.offsetFactorValue },
                returnValue = outPort
            });
        }

        public override void OnNodeModified(NodeChangeContext context, ShaderNode node)
        {
            var data = (HueData) node.data;
            if (context.WasControlModified(data.offsetFactorControl))
            {
                data.offsetFactor = context.GetControlValue(data.offsetFactorControl);
                context.SetHlslValue(data.offsetFactorValue, data.offsetFactor);
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
}

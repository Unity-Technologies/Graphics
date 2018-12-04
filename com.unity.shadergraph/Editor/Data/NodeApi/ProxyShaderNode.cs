using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct PortIdMapping
    {
        public int intId;
        public string stringId;

        public bool isValid => stringId != null;
    }

    // Currently most of Shader Graph relies on AbstractMaterialNode as an abstraction, so it's a bit of a mouthful to
    // remove it just like that. Therefore we have this class that represents an IShaderNode as a AbstractMaterialNode.
    [Serializable]
    sealed class ProxyShaderNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        [NonSerialized]
        object m_Data;

        [SerializeField]
        SerializationHelper.JSONSerializedElement m_SerializedData;

        [SerializeField]
        string m_ShaderNodeTypeName;

        [SerializeField]
        List<PortIdMapping> m_PortIdMap = new List<PortIdMapping>();

        NodeTypeState m_TypeState;

        public HlslFunctionDescriptor function { get; set; }

        public NodeTypeState typeState
        {
            get => m_TypeState;
            set
            {
                m_TypeState = value;
                m_ShaderNodeTypeName = value?.nodeType.GetType().FullName;
            }
        }

        public bool isNew { get; set; }

        public object data
        {
            get => m_Data;
            set => m_Data = value;
        }

        public string shaderNodeTypeName => m_ShaderNodeTypeName;

        public override bool hasPreview => true;

        public ProxyShaderNode()
        {
        }

        // This one is only really used in SearchWindowProvider, as we need a dummy node with slots for the code there.
        // Eventually we can make the code in SWP nicer, and remove this constructor.
        public ProxyShaderNode(NodeTypeState typeState)
        {
            this.typeState = typeState;
            name = typeState.type.name;
            isNew = true;

            UpdateSlots();
        }

        public override void ValidateNode()
        {
            base.ValidateNode();

            var errorDetected = true;
            if (owner == null)
            {
                Debug.LogError($"{name} ({guid}) has a null owner.");
            }
            else if (typeState == null)
            {
                Debug.LogError($"{name} ({guid}) has a null state.");
            }
            else if (typeState.owner != owner)
            {
                Debug.LogError($"{name} ({guid}) has an invalid state.");
            }
            else
            {
                errorDetected = false;
            }

            hasError |= errorDetected;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (data != null)
            {
                m_SerializedData = SerializationHelper.Serialize(data);
            }

            if (typeState != null)
            {
                m_ShaderNodeTypeName = typeState.nodeType.GetType().FullName;
            }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            if (m_SerializedData.typeInfo.IsValid())
            {
                m_Data = SerializationHelper.Deserialize<object>(m_SerializedData, GraphUtil.GetLegacyTypeRemapping());
                m_SerializedData = default;
            }

            UpdateStateReference();
        }

        public void UpdateStateReference()
        {
            var materialOwner = (AbstractMaterialGraph)owner;
            typeState = materialOwner.nodeTypeStates.FirstOrDefault(x => x.nodeType.GetType().FullName == shaderNodeTypeName);
            if (typeState == null)
            {
                throw new InvalidOperationException($"Cannot find an {nameof(ShaderNodeType)} with type name {shaderNodeTypeName}");
            }
            UpdateSlots();
        }

        void UpdateSlots()
        {
            var validSlotIds = new List<int>();

            // Cull unused entries in the port ID map.
            var newPortIdMap = new List<PortIdMapping>();

            var inputPortIds = typeState.type.inputs.Select(p => typeState.inputPorts[p.index].id);
            var outputPortIds = typeState.type.outputs.Select(p => typeState.outputPorts[p.index].id);

            foreach (var id in inputPortIds.Concat(outputPortIds))
            {
                var mapping = m_PortIdMap.FirstOrDefault(x => x.stringId == id);
                if (mapping.isValid)
                {
                    newPortIdMap.Add(new PortIdMapping { intId = mapping.intId, stringId = id });
                    validSlotIds.Add(mapping.intId);
                }
            }

            m_PortIdMap = newPortIdMap;

            // Build up a list of free IDs in between the current IDs.
            validSlotIds.Sort();
            var lastId = validSlotIds.LastOrDefault();
            var freeIds = new Queue<int>();

            for (var i = 0; i < validSlotIds.Count - 1; i++)
            {
                for (var j = validSlotIds[i] + 1; j < validSlotIds[i + 1]; j++)
                {
                    freeIds.Enqueue(j);
                }
            }

            void CreatePort(string stringId, string displayName, SlotType direction, PortValue value)
            {
                var mapping = m_PortIdMap.FirstOrDefault(x => x.stringId == stringId);

                int id;
                if (mapping.isValid)
                {
                    id = mapping.intId;
                }
                else
                {
                    id = freeIds.Count > 0 ? freeIds.Dequeue() : ++lastId;
                    m_PortIdMap.Add(new PortIdMapping { intId = id, stringId = stringId });
                    validSlotIds.Add(id);
                }

                var outputName = $"{NodeUtils.GetHLSLSafeName(displayName)}_{stringId}";
                switch (value.type)
                {
                    case PortValueType.Vector1:
                        AddSlot(new Vector1MaterialSlot(id, displayName, outputName, direction, value.vector1Value));
                        break;
                    case PortValueType.Vector2:
                        AddSlot(new Vector2MaterialSlot(id, displayName, outputName, direction, value.vector2Value));
                        break;
                    case PortValueType.Vector3:
                        AddSlot(new Vector3MaterialSlot(id, displayName, outputName, direction, value.vector3Value));
                        break;
                    case PortValueType.Vector4:
                        AddSlot(new Vector4MaterialSlot(id, displayName, outputName, direction, value.vector4Value));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var portRef in typeState.type.inputs)
            {
                var port = typeState.inputPorts[portRef.index];
                CreatePort(port.id, port.displayName, SlotType.Input, port.value);
            }

            foreach (var portRef in typeState.type.outputs)
            {
                var port = typeState.outputPorts[portRef.index];
                CreatePort(port.id, port.displayName, SlotType.Output, new PortValue(port.type));
            }

            RemoveSlotsNameNotMatching(validSlotIds, true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var builder = new ShaderStringBuilder();

            // Declare variables for output ports.
            foreach (var argument in function.arguments)
            {
                if (argument.type != HlslArgumentType.OutputPort)
                {
                    continue;
                }

                var stringId = typeState.outputPorts[argument.outputPortRef.index].id;
                var intId = m_PortIdMap.First(x => x.stringId == stringId).intId;
                var slot = FindSlot<MaterialSlot>(intId);
                var typeStr = NodeUtils.ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
                var variableStr = GetVariableNameForSlot(intId);
                builder.Append($"{typeStr} {variableStr};");
            }

            // Declare variable for return value, and set it to the return value from the following function call.
            if (function.returnValue.isValid)
            {
                var stringId = typeState.outputPorts[function.returnValue.index].id;
                var intId = m_PortIdMap.First(x => x.stringId == stringId).intId;
                var slot = FindSlot<MaterialSlot>(intId);
                var typeStr = NodeUtils.ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
                builder.Append($"{typeStr} {GetVariableNameForSlot(intId)} = ");
            }

            // Build up the function call.
            builder.Append($"{function.name}(");

            // Add in function arguments.
            var first = true;
            foreach (var argument in function.arguments)
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;

                switch (argument.type)
                {
                    case HlslArgumentType.InputPort:
                        var inputStringId = typeState.inputPorts[argument.inputPortRef.index].id;
                        var inputIntId = m_PortIdMap.First(x => x.stringId == inputStringId).intId;
                        builder.Append(GetSlotValue(inputIntId, generationMode));
                        break;
                    case HlslArgumentType.OutputPort:
                        var outputStringId = typeState.outputPorts[argument.outputPortRef.index].id;
                        var outputIntId = m_PortIdMap.First(x => x.stringId == outputStringId).intId;
                        builder.Append(GetVariableNameForSlot(outputIntId));
                        break;
                    case HlslArgumentType.Vector1:
                        builder.Append(NodeUtils.FloatToShaderValue(argument.vector1Value));
                        break;
                    case HlslArgumentType.Value:
                        if (generationMode == GenerationMode.Preview)
                        {
                            builder.Append($"{GetVariableNameForNode()}_v{argument.valueRef.index}");
                        }
                        else
                        {
                            var hlslValue = typeState.hlslValues[argument.valueRef.index];
                            builder.Append(NodeUtils.FloatToShaderValue(hlslValue.value));
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            builder.Append(");");

            visitor.AddShaderChunk(builder.ToString());
        }

        // TODO: This should be inserted at a higher level, but it will do for now
        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(function.source.value, builder =>
            {
                switch (function.source.type)
                {
                    case HlslSourceType.File:
                        builder.AppendLine($"#include \"{function.source.value}\"");
                        break;
                    case HlslSourceType.String:
                        builder.AppendLines(function.source.value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        public override void GetSourceAssetDependencies(List<string> paths)
        {
            foreach (var source in typeState.hlslSources)
            {
                if (source.type == HlslSourceType.File)
                {
                    paths.Add(source.value);
                }
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            if (generationMode != GenerationMode.Preview)
            {
                return;
            }

            foreach (var argument in function.arguments)
            {
                if (argument.type != HlslArgumentType.Value)
                    continue;
                properties.AddShaderProperty(new Vector1ShaderProperty
                {
                    overrideReferenceName = $"{GetVariableNameForNode()}_v{argument.valueRef.index}",
                    generatePropertyBlock = false
                });
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            foreach (var argument in function.arguments)
            {
                if (argument.type != HlslArgumentType.Value)
                    continue;
                var hlslValue = typeState.hlslValues[argument.valueRef.index];
                properties.Add(new PreviewProperty(PropertyType.Vector1)
                {
                    name = $"{GetVariableNameForNode()}_v{argument.valueRef.index}",
                    floatValue = hlslValue.value
                });
            }
        }
    }
}

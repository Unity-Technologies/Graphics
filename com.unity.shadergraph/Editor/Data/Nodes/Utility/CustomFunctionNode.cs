using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Custom Function")]
    class CustomFunctionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IHasSettings
    {
        public CustomFunctionNode()
        {
            name = "Custom Function";
        }

        public override bool hasPreview => true;

        [SerializeField]
        public HlslSourceType m_SourceType = HlslSourceType.File;

        public HlslSourceType sourceType
        {
            get => m_SourceType;
            set => m_SourceType = value;
        }

        [SerializeField]
        private string m_FunctionName = "Enter function name here...";

        public string functionName 
        {
            get => m_FunctionName;
            set => m_FunctionName = value;
        }

        [SerializeField]
        private string m_FunctionSource = "Enter function source file path here...";

        public string functionSource
        {
            get => m_FunctionSource;
            set => m_FunctionSource = value;
        }

        [SerializeField]
        private string m_FunctionBody = "Enter function body here...";

        public string functionBody
        {
            get => m_FunctionBody;
            set => m_FunctionBody = value;
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();

            GetOutputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
                visitor.AddShaderChunk(string.Format("{0} _{1}_{2};",
                    NodeUtils.ConvertConcreteSlotValueTypeToString(precision, argument.concreteValueType),
                    GetVariableNameForNode(), NodeUtils.GetHLSLSafeName(argument.shaderOutputName)));

            string call = string.Format("{0}_{1}(", functionName, precision);
            bool first = true;
            
            slots.Clear();
            GetInputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    call += ", ";
                first = false;
                call += SlotInputValue(argument, generationMode);
            }

            slots.Clear();
            GetOutputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    call += ", ";
                first = false;
                call += string.Format("_{0}_{1}", GetVariableNameForNode(), NodeUtils.GetHLSLSafeName(argument.shaderOutputName));
            }
            call += ");";
            visitor.AddShaderChunk(call, true);
        }

        string SlotInputValue(MaterialSlot port, GenerationMode generationMode)
        {
            IEdge[] edges = port.owner.owner.GetEdges(port.slotReference).ToArray();
            if (edges.Any())
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                if (fromNode == null)
                    return string.Empty;

                var slot = fromNode.FindOutputSlot<MaterialSlot>(fromSocketRef.slotId);
                if (slot == null)
                    return string.Empty;

                return ShaderGenerator.AdaptNodeOutput(fromNode, slot.id, port.concreteValueType);
            }

            return port.GetDefaultValue(generationMode);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(functionName, builder =>
            {
                switch (sourceType)
                {
                    case HlslSourceType.File:
                        builder.AppendLine($"#include \"{functionSource}\"");
                        break;
                    case HlslSourceType.String:
                        builder.AppendLine(GetFunctionHeader());
                        using(builder.BlockScope())
                        {
                            builder.AppendLines(functionBody);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private string GetFunctionHeader()
        {
            string header = string.Format("void {0}_{1}(", functionName, precision);
            var first = true;
            List<MaterialSlot> slots = new List<MaterialSlot>();

            GetInputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    header += ", ";
                first = false;
                header += string.Format("{0} {1}", argument.concreteValueType.ToString(precision), argument.shaderOutputName);
            }

            slots.Clear();
            GetOutputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    header += ", ";
                first = false;
                header += string.Format("out {0} {1}", argument.concreteValueType.ToString(precision), argument.shaderOutputName);
            }
            header += ")";
            return header;
        }
        
        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();
            ps.Add(new ShaderValueDescriptorListView(this, SlotType.Input));
            ps.Add(new ShaderValueDescriptorListView(this, SlotType.Output));
            ps.Add(new HlslFunctionView(this));
            return ps;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Switch")]
    class SwitchNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IShaderInputObserver
    {
        const int kPredicateSlot = 0;
        const int kDefaultSlot = 1;
        const int kOutputSlot = 2;
        const int kFirstCaseSlot = 3;

        public override bool hasPreview => true;

        [Serializable]
        internal struct EntryCase
        {
            [SerializeField]
            internal string name;
            [SerializeField]
            internal ComparisonType comparisonType;
            [SerializeField]
            internal float threshold;
        }

        protected override bool CanPropagateFloatLiteral => true;

        [SerializeField]
        internal bool m_floorPredicate = true;

        [SerializeField]
        internal List<EntryCase> m_cases = new();


        [SerializeField]
        internal bool m_useProperty = false;

        [SerializeField]
        internal List<EntryCase> m_casesFromUpstreamEnumProperty = new();

        internal Vector1ShaderProperty UpstreamFloatEnumProperty
        {
            get
            {
                try
                {
                    if (GetInputNodeFromSlot(kPredicateSlot) is PropertyNode propertyNode && propertyNode.property is Vector1ShaderProperty property && property.floatType == FloatType.Enum)
                        return property;
                    else return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        List<EntryCase> cases => m_useProperty ? m_casesFromUpstreamEnumProperty : m_cases;

        public SwitchNode()
        {
            name = "Switch";
            synonyms = new string[] { "switch" };
            UpdateNodeAfterDeserialization();
        }

        public override void Concretize()
        {
            // concretize is called when a connection is made.
            // we can use this opportunity to check if we need to switch modes.

            if (m_useProperty = UpstreamFloatEnumProperty != null) // automatic mode means reserializing our properties.
            {
                UpstreamFloatEnumProperty?.AddObserver(this);
                m_casesFromUpstreamEnumProperty = new();
                for (int i = 0; i < UpstreamFloatEnumProperty.enumNames.Count; ++i)
                {
                    m_casesFromUpstreamEnumProperty.Add(new EntryCase { name = UpstreamFloatEnumProperty.enumNames[i], threshold = UpstreamFloatEnumProperty.enumValues[i], comparisonType = ComparisonType.Equal });
                }
            }

            UpdateNodeAfterDeserialization();
            base.Concretize();
        }


        public override void UpdateNodeAfterDeserialization()
        {
            List<int> validSlots = new() { kPredicateSlot, kDefaultSlot, kOutputSlot };
            AddSlot(new Vector1MaterialSlot(kPredicateSlot, "Predicate", "Predicate", SlotType.Input, 0));
            AddSlot(new DynamicVectorMaterialSlot(kDefaultSlot, "Fallback", "Fallback", SlotType.Input, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(kOutputSlot, "Out", "Out", SlotType.Output, Vector4.zero));

            for (int i = 0; i < cases.Count; ++i)
            {
                var displayName = m_useProperty ? cases[i].name : $"{(char)('A'+i)}";
                var safeName = "case_"+NodeUtils.GetHLSLSafeName(displayName);

                AddSlot(new DynamicVectorMaterialSlot(kFirstCaseSlot + i, displayName, safeName, SlotType.Input, Vector4.zero));
                validSlots.Add(kFirstCaseSlot + i);
            }
            RemoveSlotsNameNotMatching(validSlots, true);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(hlslFunctionName, s => { GetFunctionDefinition(s); });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            using (var inputSlots = PooledList<MaterialSlot>.Get())
            using (var outputSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(inputSlots);
                GetOutputSlots(outputSlots);

                // Declare Outputs
                foreach (var output in outputSlots)
                    sb.AppendLine("{0} {1};", output.concreteValueType.ToShaderString(), GetVariableNameForSlot(output.id));

                // Call Function
                sb.TryAppendIndentation();
                sb.Append(hlslFunctionName);
                sb.Append("(");
                bool first = true;

                foreach (var input in inputSlots)
                {
                    string argument = SlotInputValue(input, generationMode);
                    if (first && m_floorPredicate) // predicate is first
                        argument = $"floor({argument})";
                    if (!first)
                        sb.Append(", ");
                    first = false;

                    sb.Append(argument);
                }

                foreach (var output in outputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    sb.Append(GetVariableNameForSlot(output.id));
                }
                sb.Append(");");
                sb.AppendNewLine();
            }
        }

        string hlslFunctionName => $"SwitchNode_$precision{this.objectId}";

        static string Op(ComparisonType op)
        {
            switch (op)
            {
                default:
                case ComparisonType.Equal:          return "==";
                case ComparisonType.NotEqual:       return "!=";
                case ComparisonType.Less:           return "<";
                case ComparisonType.LessOrEqual:    return "<=";
                case ComparisonType.Greater:        return ">";
                case ComparisonType.GreaterOrEqual: return ">=";
            }
        }

        void GetFunctionDefinition(ShaderStringBuilder sb)
        {
            using (var inputSlots = PooledList<MaterialSlot>.Get())
            using (var outputSlots = PooledList<MaterialSlot>.Get())
            {
                ShaderStringBuilder body = new();
                body.AppendLine("Out = Fallback;");

                GetInputSlots(inputSlots);
                GetOutputSlots(outputSlots);

                sb.Append("void ");
                sb.Append(hlslFunctionName);
                sb.Append("(");

                var first = true;

                foreach (var argument in inputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    argument.AppendHLSLParameterDeclaration(sb, argument.shaderOutputName);

                    if (argument.id >= kFirstCaseSlot)
                    {
                        var entry = cases[argument.id - kFirstCaseSlot];
                        if (argument.id == kFirstCaseSlot)
                            body.AppendLine("[branch]");
                        body.AppendLine($"{(argument.id > kFirstCaseSlot ? "else " : "")}if (Predicate {Op(entry.comparisonType)} {entry.threshold})");
                        using (body.BlockScope())
                        {
                            body.AppendLine($"Out = {argument.shaderOutputName};");
                        }
                    }
                }

                foreach (var argument in outputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    sb.Append("out ");
                    argument.AppendHLSLParameterDeclaration(sb, argument.shaderOutputName);
                }
                sb.AppendLine(")");
                using (sb.BlockScope())
                    sb.AppendLines(body.ToString());
            }
        }

        string SlotInputValue(MaterialSlot port, GenerationMode generationMode)
        {
            List<IEdge> edges = new(port.owner.owner.GetEdges(port.slotReference));
            if (edges.Count > 0)
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = fromSocketRef.node;
                if (fromNode == null)
                    return string.Empty;

                return fromNode.GetOutputForSlot(fromSocketRef, port.concreteValueType, generationMode);
            }
            return port.GetDefaultValue(generationMode);
        }

        public void OnShaderInputUpdated(ModificationScope modificationScope)
        {
            Concretize();
            Dirty(ModificationScope.Topological);
        }
    }
}

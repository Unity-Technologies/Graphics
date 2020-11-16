using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.ViewDirectionNode")]
    [Title("Input", "Geometry", "View Direction")]
    class ViewDirectionNode : GeometryNode, IMayRequireViewDirection, IGeneratesFunction, IGeneratesVariables
    {
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";
        public override int latestVersion => 1;

        public ViewDirectionNode()
        {
            name = "View Direction";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    kOutputSlotName,
                    kOutputSlotName,
                    SlotType.Output,
                    Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch(m_SGVersion)
            {
                case 0:
                    return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.ViewDirection));
                case 1:
                    return GetVariableName();
                default:
                    return null;
            }
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }

        private readonly string functionName = "NormalizeIfURP";

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            switch (sgVersion)
            {
                case 0:
                    if(generationMode == GenerationMode.Preview)
                    {
                        registry.ProvideFunction(functionName, s =>
                        {
                            s.AppendLine("$precision3 {0} ($precision3 In)", functionName);
                            using (s.BlockScope())
                            {
                                s.AppendLine("return normalize(In);");
                            }
                        });
                    }
                    break;
                case 1:
                default:
                    registry.ProvideFunction(functionName, s =>
                    {
                        s.AppendLine("$precision3 {0} ($precision3 In)", functionName);
                        using (s.BlockScope())
                        {
                            s.AppendLine("#ifdef UNIVERSAL_PIPELINE_CORE_INCLUDED");
                            s.AppendLine("\treturn normalize(In);");
                            s.AppendLine("#else");
                            s.AppendLine("\treturn In;");
                            s.AppendLine("#endif");
                        }
                    });
                    break;
            }
        }

        public void GenerateNodeVariables(VariableRegistry registry, GenerationMode generationMode)
        {
            switch (sgVersion)
            {
                case 0:
                    if(generationMode == GenerationMode.Preview)
                    {
                        registry.ProvideVariable(GetVariableName(), s =>
                        {
                            s.AppendLine("$precision3 {0} = {1}(IN.{2});",
                                GetVariableName(),
                                functionName,
                                space.ToVariableName(InterpolatorType.ViewDirection)
                                );
                        });
                    }
                    break;
                case 1:
                default:
                    registry.ProvideVariable(GetVariableName(), s =>
                    {
                        s.AppendLine("$precision3 {0} = {1}(IN.{2});",
                            GetVariableName(),
                            functionName,
                            space.ToVariableName(InterpolatorType.ViewDirection)
                            );
                    });
                    break;
            }
        }

        private string GetVariableName()
        {
            return "normalizedViewDirection";
        }
    }
}

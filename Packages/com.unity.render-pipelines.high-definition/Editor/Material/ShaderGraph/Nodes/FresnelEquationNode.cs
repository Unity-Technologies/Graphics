using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    enum FresnelEquationMode
    {
        Schlick,
        Dielectric,
        DielectricGeneric
    };

    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Math", "Advanced", "High Definition Render Pipeline", "Fresnel Equation")]
    class HDFresnelEquationNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
    {
        [SerializeField]
        private FresnelEquationMode m_FresnelEquationMode = FresnelEquationMode.Schlick;

        const string kDotVectorsInputSlotName = "Dot Vector";
        const string kF0InputSlotName = "F0";
        const string kIORSourceInputSlotName = "IOR Source";
        const string kIORMediumInputSlotName = "IOR Medium";
        const string kIORMediumImInputSlotName = "IOR Medium K";

        const string kFresnelOutputSlotName = "Fresnel";

        public override string documentationURL => Documentation.GetPageLink("Fresnel-Equation-Node");

        private enum FresnelSlots
        {
            kDotVectorsInputSlotId,
            kF0InputSlotId,
            kIORSourceInputSlotId,
            kIORMediumInputSlotId,
            kIORMediumKInputSlotId,
            kFresnelOutputSlotId
        }

        Vector1MaterialSlot dotInputSlot;
        Vector1MaterialSlot f0InputSlot;
        Vector1MaterialSlot iorSourceInputSlot;
        Vector1MaterialSlot iorMediumInputSlot;
        Vector1MaterialSlot iorMediumKInputSlot;
        Vector1MaterialSlot fresnelOutputSlot;

        [EnumControl("Mode")]
        public FresnelEquationMode fresnelEquationMode
        {
            get { return m_FresnelEquationMode; }
            set
            {
                if (m_FresnelEquationMode == value)
                    return;

                m_FresnelEquationMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        public HDFresnelEquationNode()
        {
            name = "Fresnel Equation";
            synonyms = new string[] { "fresnel", "schlick", "metal", "dielectric", "tir", "reflection", "critical" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var addedSlots = new List<int>();
            dotInputSlot = null;
            f0InputSlot = null;
            iorSourceInputSlot = null;
            iorMediumInputSlot = null;
            iorMediumKInputSlot = null;
            fresnelOutputSlot = null;

            dotInputSlot = new Vector1MaterialSlot((int)FresnelSlots.kDotVectorsInputSlotId, kDotVectorsInputSlotName, kDotVectorsInputSlotName, SlotType.Input, 0.0f);
            AddSlot(dotInputSlot);
            addedSlots.Add(dotInputSlot.id);

            if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
            {
                f0InputSlot = new Vector1MaterialSlot((int)FresnelSlots.kF0InputSlotId, kF0InputSlotName, kF0InputSlotName, SlotType.Input, 0.04f);
                AddSlot(f0InputSlot);
                addedSlots.Add(f0InputSlot.id);
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
            {
                iorSourceInputSlot = new Vector1MaterialSlot((int)FresnelSlots.kIORSourceInputSlotId, kIORSourceInputSlotName, kIORSourceInputSlotName, SlotType.Input, 1.0f);
                iorMediumInputSlot = new Vector1MaterialSlot((int)FresnelSlots.kIORMediumInputSlotId, kIORMediumInputSlotName, kIORMediumInputSlotName, SlotType.Input, 1.5f);
                AddSlot(iorSourceInputSlot);
                addedSlots.Add(iorSourceInputSlot.id);

                AddSlot(iorMediumInputSlot);
                addedSlots.Add(iorMediumInputSlot.id);
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
            {
                iorSourceInputSlot = new Vector1MaterialSlot((int)FresnelSlots.kIORSourceInputSlotId, kIORSourceInputSlotName, kIORSourceInputSlotName, SlotType.Input, 1.0f);
                iorMediumInputSlot = new Vector1MaterialSlot((int)FresnelSlots.kIORMediumInputSlotId, kIORMediumInputSlotName, kIORMediumInputSlotName, SlotType.Input, 1.5f);
                iorMediumKInputSlot = new Vector1MaterialSlot((int)FresnelSlots.kIORMediumKInputSlotId, kIORMediumImInputSlotName, kIORMediumImInputSlotName, SlotType.Input, 2.0f);
                AddSlot(iorSourceInputSlot);
                addedSlots.Add(iorSourceInputSlot.id);

                AddSlot(iorMediumInputSlot);
                addedSlots.Add(iorMediumInputSlot.id);

                AddSlot(iorMediumKInputSlot);
                addedSlots.Add(iorMediumKInputSlot.id);
            }

            fresnelOutputSlot = new Vector1MaterialSlot((int)FresnelSlots.kFresnelOutputSlotId, kFresnelOutputSlotName, kFresnelOutputSlotName, SlotType.Output, 0.5f);
            AddSlot(fresnelOutputSlot);
            addedSlots.Add(fresnelOutputSlot.id);

            RemoveSlotsNameNotMatching(addedSlots, supressWarnings: true);
        }

        string GetFunctionName()
        {
            return string.Format("Unity_FresnelEquation_{0}_$precision", m_FresnelEquationMode.ToString());
        }

        string GetResultType()
        {
            string resultType;
            if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
            {
                resultType = f0InputSlot.concreteValueType.ToShaderString();
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
            {
                resultType = ((ConcreteSlotValueType)System.Math.Min((int)iorSourceInputSlot.concreteValueType, (int)iorMediumInputSlot.concreteValueType)).ToShaderString();
            }
            else //if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
            {
                resultType = ((ConcreteSlotValueType)System.Math.Min(System.Math.Min((int)iorSourceInputSlot.concreteValueType, (int)iorMediumInputSlot.concreteValueType), (int)iorMediumKInputSlot.concreteValueType)).ToShaderString();
            }
            return resultType;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl");

            string resultType = GetResultType();
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
                {
                    s.AppendLine("void {0}(out {1} FresnelValue, $precision cos0, {1} f0)", GetFunctionName(), resultType, resultType);
                }
                else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
                {
                    s.AppendLine("void {0}(out {1} FresnelValue, $precision cos0, {2} iorSource, {3} iorMedium)",
                        GetFunctionName(),
                        resultType,
                        iorSourceInputSlot.concreteValueType.ToShaderString(),
                        iorMediumInputSlot.concreteValueType.ToShaderString());
                }
                else //if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
                {
                    s.AppendLine("void {0}(out {1} FresnelValue, $precision cos0, {2} iorSource, {3} iorMedium, {4} iorMediumK)",
                        GetFunctionName(),
                        resultType,
                        iorSourceInputSlot.concreteValueType.ToShaderString(),
                        iorMediumInputSlot.concreteValueType.ToShaderString(),
                        iorMediumKInputSlot.concreteValueType.ToShaderString());
                }
                using (s.BlockScope())
                {
                    if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
                    {
                        s.AppendLine("FresnelValue = F_Schlick(f0, cos0);");
                    }
                    else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
                    {
                        s.AppendLine("FresnelValue = F_FresnelDielectric(iorMedium/iorSource, cos0);");
                    }
                    else //if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
                    {
                        s.AppendLine("FresnelValue = F_FresnelConductor(iorMedium/iorSource, iorMediumK/iorSource, cos0);");
                    }
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("{0} {1};", GetResultType(), GetVariableNameForSlot((int)FresnelSlots.kFresnelOutputSlotId));
            if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
            {
                sb.AppendLine("{0}({1}, {2}, {3});",
                    GetFunctionName(),
                    GetVariableNameForSlot(fresnelOutputSlot.id),
                    GetSlotValue(dotInputSlot.id, generationMode),
                    GetSlotValue(f0InputSlot.id, generationMode)
                );
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
            {
                sb.AppendLine("{0}({1}, {2}, {3}, {4});",
                    GetFunctionName(),
                    GetVariableNameForSlot(fresnelOutputSlot.id),
                    GetSlotValue(dotInputSlot.id, generationMode),
                    GetSlotValue(iorSourceInputSlot.id, generationMode),
                    GetSlotValue(iorMediumInputSlot.id, generationMode)
                );
            }
            else //if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
            {
                sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5});",
                    GetFunctionName(),
                    GetVariableNameForSlot(fresnelOutputSlot.id),
                    GetSlotValue(dotInputSlot.id, generationMode),
                    GetSlotValue(iorSourceInputSlot.id, generationMode),
                    GetSlotValue(iorMediumInputSlot.id, generationMode),
                    GetSlotValue(iorMediumKInputSlot.id, generationMode)
                );
            }
        }
    }
}

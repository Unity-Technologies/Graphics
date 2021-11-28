using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    enum FresnelEquationMode
    {
        Schlick,
        Dielectric,
        DielectricGeneric
    };

    [Title("Math", "Advanced", "Fresnel Equation")]
    class FresnelEquationNode : AbstractMaterialNode
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

        int kDotVectorsInputSlotId;
        int kF0InputSlotId;
        int kIORSourceInputSlotId;
        int kIORMediumInputSlotId;
        int kIORMediumImInputSlotId;
        int kFresnelOutputSlotId;

        Vector1MaterialSlot f0InputSlot;
        Vector1MaterialSlot iorSourceInputSlot;
        Vector1MaterialSlot iorMediumInputSlot;
        Vector1MaterialSlot iorMediumKInputSlot;

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

        public FresnelEquationNode()
        {
            name = "Fresnel Equation";
            synonyms = new string[] { "fresnel", "schlick", "metal", "dielectric", "tir", "reflection", "critical" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            int slotId = 0;

            if (GetSlotProperty(kDotVectorsInputSlotId) != null)
                RemoveSlot(kDotVectorsInputSlotId);
            if (GetSlotProperty(kF0InputSlotId) != null)
                RemoveSlot(kF0InputSlotId);
            if (GetSlotProperty(kIORSourceInputSlotId) != null)
                RemoveSlot(kIORSourceInputSlotId);
            if (GetSlotProperty(kIORMediumInputSlotId) != null)
                RemoveSlot(kIORMediumInputSlotId);
            if (GetSlotProperty(kIORMediumImInputSlotId) != null)
                RemoveSlot(kIORMediumImInputSlotId);
            if (GetSlotProperty(kFresnelOutputSlotId) != null)
                RemoveSlot(kFresnelOutputSlotId);

            f0InputSlot = null;
            iorSourceInputSlot = null;
            iorMediumInputSlot = null;
            iorMediumKInputSlot = null;

            kDotVectorsInputSlotId = slotId++;
            AddSlot(new Vector1MaterialSlot(kDotVectorsInputSlotId, kDotVectorsInputSlotName, kDotVectorsInputSlotName, SlotType.Input, 0.0f));

            if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
            {
                kF0InputSlotId = slotId++;
                f0InputSlot = new Vector1MaterialSlot(kF0InputSlotId, kF0InputSlotName, kF0InputSlotName, SlotType.Input, 0.04f);
                AddSlot(f0InputSlot);
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
            {
                kIORSourceInputSlotId = slotId++;
                kIORMediumInputSlotId = slotId++;
                iorSourceInputSlot = new Vector1MaterialSlot(kIORSourceInputSlotId, kIORSourceInputSlotName, kIORSourceInputSlotName, SlotType.Input, 1.0f);
                iorMediumInputSlot = new Vector1MaterialSlot(kIORMediumInputSlotId, kIORMediumInputSlotName, kIORMediumInputSlotName, SlotType.Input, 1.5f);
                AddSlot(iorSourceInputSlot);
                AddSlot(iorMediumInputSlot);
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
            {
                kIORSourceInputSlotId = slotId++;
                kIORMediumInputSlotId = slotId++;
                kIORMediumImInputSlotId = slotId++;
                iorSourceInputSlot = new Vector1MaterialSlot(kIORSourceInputSlotId, kIORSourceInputSlotName, kIORSourceInputSlotName, SlotType.Input, 1.0f);
                iorMediumInputSlot = new Vector1MaterialSlot(kIORMediumInputSlotId, kIORMediumInputSlotName, kIORMediumInputSlotName, SlotType.Input, 1.5f);
                iorMediumKInputSlot = new Vector1MaterialSlot(kIORMediumImInputSlotId, kIORMediumImInputSlotName, kIORMediumImInputSlotName, SlotType.Input, 2.0f);
                AddSlot(iorSourceInputSlot);
                AddSlot(iorMediumInputSlot);
                AddSlot(iorMediumKInputSlot);
            }

            kFresnelOutputSlotId = slotId++;
            AddSlot(new Vector1MaterialSlot(kFresnelOutputSlotId, kFresnelOutputSlotName, kFresnelOutputSlotName, SlotType.Output, 0.5f));

            RemoveSlotsNameNotMatching(new[]
            {
                kDotVectorsInputSlotId,
                kF0InputSlotId,
                kIORSourceInputSlotId,
                kIORMediumInputSlotId,
                kIORMediumImInputSlotId,
                kFresnelOutputSlotId
            });
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
                        s.AppendLine("FresnelValue = F_FresnelDielectric(IorToFresnel0(iorMedium, iorSource), cos0);");
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
            sb.AppendLine("{0} {1};", GetResultType(), GetVariableNameForSlot(kFresnelOutputSlotId));
            if (m_FresnelEquationMode == FresnelEquationMode.Schlick)
            {
                sb.AppendLine("{0}({1}, {2}, {3});",
                    GetFunctionName(),
                    GetVariableNameForSlot(kFresnelOutputSlotId),
                    GetSlotValue(kDotVectorsInputSlotId, generationMode),
                    GetSlotValue(kF0InputSlotId, generationMode)
                );
            }
            else if (m_FresnelEquationMode == FresnelEquationMode.Dielectric)
            {
                sb.AppendLine("{0}({1}, {2}, {3}, {4});",
                    GetFunctionName(),
                    GetVariableNameForSlot(kFresnelOutputSlotId),
                    GetSlotValue(kDotVectorsInputSlotId, generationMode),
                    GetSlotValue(kIORSourceInputSlotId, generationMode),
                    GetSlotValue(kIORMediumInputSlotId, generationMode)
                );
            }
            else //if (m_FresnelEquationMode == FresnelEquationMode.DielectricGeneric)
            {
                sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5});",
                    GetFunctionName(),
                    GetVariableNameForSlot(kFresnelOutputSlotId),
                    GetSlotValue(kDotVectorsInputSlotId, generationMode),
                    GetSlotValue(kIORSourceInputSlotId, generationMode),
                    GetSlotValue(kIORMediumInputSlotId, generationMode),
                    GetSlotValue(kIORMediumImInputSlotId, generationMode)
                );
            }
        }
    }
}

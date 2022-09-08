using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    enum RefractMode
    {
        CriticalAngle,
        Safe
    };

    [FormerName("UnityEditor.Rendering.HighDefinition.HDRefractNode")]
    [Title("Math", "Vector", "Refract")]
    class RefractNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
    {
        [SerializeField]
        private RefractMode m_RefractMode = RefractMode.Safe;

        const int kIncidentInputSlotId = 0;
        const string kIncidentInputSlotName = "Incident";
        const int kNormalInputSlotId = 1;
        const string kNormalInputSlotName = "Normal";
        const int kIORSourceInputSlotId = 2;
        const string kIORSourceInputSlotName = "IORSource";
        const int kIORMediumInputSlotId = 3;
        const string kIORMediumInputSlotName = "IORMedium";

        const int kRefractedOutputSlotId = 4;
        const string kRefractedOutputSlotName = "Refracted";
        const int kIntensityOutputSlotId = 5;
        const string kIntensityOutputSlotName = "Intensity";

        [EnumControl("Mode")]
        public RefractMode refractMode
        {
            get { return m_RefractMode; }
            set
            {
                if (m_RefractMode == value)
                    return;

                m_RefractMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public RefractNode()
        {
            name = "Refract";
            synonyms = new string[] { "refract", "warp", "bend", "distort" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kIncidentInputSlotId, kIncidentInputSlotName, kIncidentInputSlotName, SlotType.Input, Vector3.forward));
            AddSlot(new Vector3MaterialSlot(kNormalInputSlotId, kNormalInputSlotName, kNormalInputSlotName, SlotType.Input, -Vector3.forward));
            AddSlot(new Vector1MaterialSlot(kIORSourceInputSlotId, kIORSourceInputSlotName, kIORSourceInputSlotName, SlotType.Input, 1.0f));
            AddSlot(new Vector1MaterialSlot(kIORMediumInputSlotId, kIORMediumInputSlotName, kIORMediumInputSlotName, SlotType.Input, 1.5f));

            AddSlot(new Vector3MaterialSlot(kRefractedOutputSlotId, kRefractedOutputSlotName, kRefractedOutputSlotName, SlotType.Output, Vector3.forward));
            AddSlot(new Vector1MaterialSlot(kIntensityOutputSlotId, kIntensityOutputSlotName, kIntensityOutputSlotName, SlotType.Output, 1.0f));

            RemoveSlotsNameNotMatching(new[]
            {
                kIncidentInputSlotId,
                kNormalInputSlotId,
                kIORSourceInputSlotId,
                kIORMediumInputSlotId,
                kRefractedOutputSlotId,
                kIntensityOutputSlotId
            });
        }

        string GetFunctionName()
        {
            return string.Format("Unity_Refract_{0}_$precision", m_RefractMode.ToString());
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl");

            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("void {0}(out $precision3 Refracted, out $precision3 Intensity, $precision3 Incident, $precision3 Normal, $precision IORSource, $precision IORMedium)", GetFunctionName());
                using (s.BlockScope())
                {
                    s.AppendLine("$precision internalIORSource = max(IORSource, 1.0);");
                    s.AppendLine("$precision internalIORMedium = max(IORMedium, 1.0);");
                    s.AppendLine("$precision eta = internalIORSource/internalIORMedium;");
                    s.AppendLine("$precision cos0 = dot(Incident, Normal);");
                    s.AppendLine("$precision k = 1.0 - eta*eta*(1.0 - cos0*cos0);");
                    if (m_RefractMode == RefractMode.Safe)
                    {
                        s.AppendLine("Refracted = eta*Incident - (eta*cos0 + sqrt(max(k, 0.0)))*Normal;");
                    }
                    else
                    {
                        s.AppendLine("Refracted = k >= 0.0 ? eta*Incident - (eta*cos0 + sqrt(k))*Normal : reflect(Incident, Normal);");
                    }
                    s.AppendLine("Intensity = internalIORSource <= internalIORMedium ?");
                    s.AppendLine("    saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :");
                    s.AppendLine("    (k >= 0.0 ? F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0) : ");
                    if (m_RefractMode == RefractMode.Safe)
                    {
                        s.Append("0.0);");
                    }
                    else
                    {
                        s.Append("1.0);");
                    }
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0};", GetVariableNameForSlot(kRefractedOutputSlotId));
            sb.AppendLine("$precision3 {0};", GetVariableNameForSlot(kIntensityOutputSlotId));
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6});",
                GetFunctionName(),
                GetVariableNameForSlot(kRefractedOutputSlotId),
                GetVariableNameForSlot(kIntensityOutputSlotId),
                GetSlotValue(kIncidentInputSlotId, generationMode),
                GetSlotValue(kNormalInputSlotId, generationMode),
                GetSlotValue(kIORSourceInputSlotId, generationMode),
                GetSlotValue(kIORMediumInputSlotId, generationMode)
            );
        }
    }
}

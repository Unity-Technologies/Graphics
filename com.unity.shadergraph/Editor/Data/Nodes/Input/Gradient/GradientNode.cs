using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Gradient")]
    class GradientNode : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private float m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public GradientNode()
        {
            name = "Gradient";
            synonyms = new string[] { "ramp" };
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return string.Format("Unity_{0}", GetVariableNameForNode());
        }

        Gradient m_Gradient = new Gradient();

        [SerializeField]
        Vector4[] m_SerializableColorKeys = { new Vector4(1f, 1f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f), };

        [SerializeField]
        Vector2[] m_SerializableAlphaKeys = { new Vector2(1f, 0f), new Vector2(1f, 1f) };

        [SerializeField]
        int m_SerializableMode = 0;

        [GradientControl("")]
        public Gradient gradient
        {
            get
            {
                if (m_SerializableAlphaKeys != null && m_SerializableColorKeys != null)
                {
                    m_Gradient = new Gradient();
                    var colorKeys = m_SerializableColorKeys.Select(k => new GradientColorKey(new Color(k.x, k.y, k.z, 1f), k.w)).ToArray();
                    var alphaKeys = m_SerializableAlphaKeys.Select(k => new GradientAlphaKey(k.x, k.y)).ToArray();
                    m_SerializableAlphaKeys = null;
                    m_SerializableColorKeys = null;
                    m_Gradient.SetKeys(colorKeys, alphaKeys);
                    m_Gradient.mode = (GradientMode)m_SerializableMode;
                }

                return m_Gradient;
            }
            set
            {
                var scope = ModificationScope.Nothing;

                if (!GradientUtil.CheckEquivalency(gradient, value))
                    scope = scope < ModificationScope.Graph ? ModificationScope.Graph : scope;

                if (scope > ModificationScope.Nothing)
                {
                    var newColorKeys = value.colorKeys;
                    var newAlphaKeys = value.alphaKeys;

                    m_Gradient.SetKeys(newColorKeys, newAlphaKeys);
                    m_Gradient.mode = value.mode;
                    Dirty(ModificationScope.Node);
                }
            }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (m_Gradient != null)
            {
                m_SerializableColorKeys = m_Gradient.colorKeys.Select(k => new Vector4(k.color.r, k.color.g, k.color.b, k.time)).ToArray();
                m_SerializableAlphaKeys = m_Gradient.alphaKeys.Select(k => new Vector2(k.alpha, k.time)).ToArray();
                m_SerializableMode = (int)m_Gradient.mode;
            }
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new GradientMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
            {
                sb.AppendLine("Gradient {0} = {1};", GetVariableNameForSlot(outputSlotId), GradientUtil.GetGradientForPreview(GetVariableNameForNode()));
            }
            else
            {
                sb.AppendLine("Gradient {0} = {1}", GetVariableNameForSlot(outputSlotId), GradientUtil.GetGradientValue(gradient, ";"));
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Gradient)
            {
                name = GetVariableNameForNode(),
                gradientValue = gradient
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            GradientUtil.GetGradientPropertiesForPreview(properties, GetVariableNameForNode(), gradient);
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new GradientShaderProperty { value = gradient };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}

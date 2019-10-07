using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

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
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return string.Format("Unity_{0}", GetVariableNameForNode());
        }

        [JsonProperty]
        Gradient m_Gradient = new Gradient();

        [GradientControl("")]
        public Gradient gradient
        {
            get
            {
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

        [JsonExtensionData]
        Dictionary<string, JToken> m_ExtensionData = default;

        [OnDeserialized]
        void OnDeserialized(StreamingContext _)
        {
            if (m_ExtensionData.ContainsKey("m_SerializableColorKeys"))
            {
                m_Gradient = new Gradient();
                m_Gradient.mode = (GradientMode)m_ExtensionData["m_SerializableMode"].Value<int>();
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new Vector4Converter(), new Vector2Converter() }
                });
                var serializableColorKeys = serializer.Deserialize<Vector4[]>(m_ExtensionData["m_SerializableColorKeys"].CreateReader());
                var serializableAlphaKeys = serializer.Deserialize<Vector2[]>(m_ExtensionData["m_SerializableAlphaKeys"].CreateReader());
                var colorKeys = serializableColorKeys.Select(k => new GradientColorKey(new Color(k.x, k.y, k.z, 1f), k.w)).ToArray();
                var alphaKeys = serializableAlphaKeys.Select(k => new GradientAlphaKey(k.x, k.y)).ToArray();
                m_Gradient.SetKeys(colorKeys, alphaKeys);
            }

            m_ExtensionData.Clear();
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new GradientMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
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
            if(!generationMode.IsPreview())
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

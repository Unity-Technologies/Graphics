using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum DielectricMaterialType
    {
        Common,
        RustedMetal,
        Water,
        Ice,
        Glass,
        Custom
    };

    [Title("Input", "PBR", "Dielectric Specular")]
    public class DielectricSpecularNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public DielectricSpecularNode()
        {
            name = "Dielectric Specular";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        DielectricMaterial m_Material = new DielectricMaterial(DielectricMaterialType.Common, 0.5f, 1.0f);

        [Serializable]
        public struct DielectricMaterial
        {
            public DielectricMaterialType type;
            public float range;
            public float indexOfRefraction;

            public DielectricMaterial(DielectricMaterialType type, float range, float indexOfRefraction)
            {
                this.type = type;
                this.range = range;
                this.indexOfRefraction = indexOfRefraction;
            }
        }

        [DielectricSpecularControl()]
        public DielectricMaterial material
        {
            get { return m_Material; }
            set
            {
                if ((value.type == m_Material.type) && (value.range == m_Material.range) && (value.indexOfRefraction == m_Material.indexOfRefraction))
                    return;
                DielectricMaterialType previousType = m_Material.type;
                m_Material = value;
                if(value.type != previousType)
                    Dirty(ModificationScope.Graph);
                else
                    Dirty(ModificationScope.Node);
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            if (!generationMode.IsPreview())
            {
                switch (material.type)
                {
                    case DielectricMaterialType.Custom:
                        sb.AppendLine("{0} _{1}_IOR = {2};", precision, GetVariableNameForNode(), material.indexOfRefraction);
                        break;
                    case DielectricMaterialType.Common:
                        sb.AppendLine("{0} _{1}_Range = {2};", precision, GetVariableNameForNode(), material.range);
                        break;
                    default:
                        break;
                }
            }
            switch (material.type)
            {
                case DielectricMaterialType.RustedMetal:
                    sb.AppendLine(string.Format("{0}3 {1} = {0}3(0.030, 0.030, 0.030);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterialType.Water:
                    sb.AppendLine(string.Format("{0}3 {1} = {0}3(0.020, 0.020, 0.020);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterialType.Ice:
                    sb.AppendLine(string.Format("{0}3 {1} = {0}3(0.018, 0.018, 0.018);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterialType.Glass:
                    sb.AppendLine(string.Format("{0}3 {1} = {0}3(0.040, 0.040, 0.040);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterialType.Custom:
                    sb.AppendLine(string.Format("{0}3 {1} = pow({2} - 1, 2) / pow({2} + 1, 2);", precision, GetVariableNameForSlot(kOutputSlotId),
                        string.Format("_{0}_IOR", GetVariableNameForNode())), true);
                    break;
                default:
                    sb.AppendLine(string.Format("{0}3 {1} = lerp(0.034, 0.048, {2});", precision, GetVariableNameForSlot(kOutputSlotId), 
                        string.Format("_{0}_Range", GetVariableNameForNode())), true);
                    break;
            }
            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if(material.type == DielectricMaterialType.Common)
            {
                properties.Add(new PreviewProperty(PropertyType.Float)
                {
                    name = string.Format("_{0}_Range", GetVariableNameForNode()),
                    floatValue = material.range
                });
            }
            else if (material.type == DielectricMaterialType.Custom)
            {
                properties.Add(new PreviewProperty(PropertyType.Float)
                {
                    name = string.Format("_{0}_IOR", GetVariableNameForNode()),
                    floatValue = material.indexOfRefraction
                });
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);
            
            if (material.type == DielectricMaterialType.Common)
            {
                properties.AddShaderProperty(new FloatShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_Range", GetVariableNameForNode()),
                    generatePropertyBlock = false
                });
            }
            else if (material.type == DielectricMaterialType.Custom)
            {
                properties.AddShaderProperty(new FloatShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_IOR", GetVariableNameForNode()),
                    generatePropertyBlock = false
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum DielectricMaterial
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
        private DielectricMaterial m_Material = DielectricMaterial.Common;

        [EnumControl("Material")]
        public DielectricMaterial material
        {
            get { return m_Material; }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private float m_Range = 0.5f;
        
        [MultiFloatControl("Range")]
        public float range
        {
            get { return m_Range; }
            set
            {
                if (m_Range == value)
                    return;

                m_Range = value;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private float m_IndexOfRefraction = 1.0f;

        [MultiFloatControl("IOR")]
        public float indexOfRefraction
        {
            get { return m_IndexOfRefraction; }
            set
            {
                if (m_IndexOfRefraction == value)
                    return;

                m_IndexOfRefraction = value;
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
            switch (m_Material)
            {
                case DielectricMaterial.RustedMetal:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.030, 0.030, 0.030);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterial.Water:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.020, 0.020, 0.020);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterial.Ice:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.018, 0.018, 0.018);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterial.Glass:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.040, 0.040, 0.040);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case DielectricMaterial.Custom:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = pow({2} - 1, 2) / pow({2} + 1, 2);", precision, GetVariableNameForSlot(kOutputSlotId),
                        string.Format("_{0}_IOR", GetVariableNameForNode())), true);
                    break;
                default:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = lerp(0.034, 0.048, {2});", precision, GetVariableNameForSlot(kOutputSlotId), 
                        string.Format("_{0}_Range", GetVariableNameForNode())), true);
                    break;
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if(material == DielectricMaterial.Common)
            {
                properties.Add(new PreviewProperty(PropertyType.Float)
                {
                    name = string.Format("_{0}_Range", GetVariableNameForNode()),
                    floatValue = range
                });
            }
            else if (material == DielectricMaterial.Custom)
            {
                properties.Add(new PreviewProperty(PropertyType.Float)
                {
                    name = string.Format("_{0}_IOR", GetVariableNameForNode()),
                    floatValue = indexOfRefraction
                });
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            if (material == DielectricMaterial.Common)
            {
                properties.AddShaderProperty(new FloatShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_Range", GetVariableNameForNode()),
                    generatePropertyBlock = false
                });
            }
            else if (material == DielectricMaterial.Custom)
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

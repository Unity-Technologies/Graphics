using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 2x2")]
    class Matrix2Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector2 m_Row0;

        [SerializeField]
        Vector2 m_Row1;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector2 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector2 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        void SetRow(ref Vector2 row, Vector2 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Node);
        }

        public Matrix2Node()
        {
            name = "Matrix 2x2";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row0
            });

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row1
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("$precision2 _{0}_m0 = $precision2 ({1}, {2});", GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row0.x),
                    NodeUtils.FloatToShaderValue(m_Row0.y));
                sb.AppendLine("$precision2 _{0}_m1 = $precision2 ({1}, {2});", GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row1.x),
                    NodeUtils.FloatToShaderValue(m_Row1.y));
            }
            sb.AppendLine("$precision2x2 {0} = $precision2x2 (_{0}_m0.x, _{0}_m0.y, _{0}_m1.x, _{0}_m1.y);", GetVariableNameForNode());
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = string.Format("_{0}_m0", GetVariableNameForNode()),
                vector4Value = m_Row0
            });

            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = string.Format("_{0}_m1", GetVariableNameForNode()),
                vector4Value = m_Row1
            });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new Matrix2ShaderProperty
            {
                value = new Matrix4x4()
                {
                    m00 = row0.x,
                    m01 = row0.y,
                    m02 = 0,
                    m03 = 0,
                    m10 = row1.x,
                    m11 = row1.y,
                    m12 = 0,
                    m13 = 0,
                    m20 = 0,
                    m21 = 0,
                    m22 = 0,
                    m23 = 0,
                    m30 = 0,
                    m31 = 0,
                    m32 = 0,
                    m33 = 0,
                }
            };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}

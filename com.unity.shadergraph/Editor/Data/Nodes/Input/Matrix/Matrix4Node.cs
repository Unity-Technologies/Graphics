using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 4x4")]
    class Matrix4Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector4 m_Row0;

        [SerializeField]
        Vector4 m_Row1;

        [SerializeField]
        Vector4 m_Row2;

        [SerializeField]
        Vector4 m_Row3;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row2
        {
            get { return m_Row2; }
            set { SetRow(ref m_Row2, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row3
        {
            get { return m_Row3; }
            set { SetRow(ref m_Row3, value); }
        }

        void SetRow(ref Vector4 row, Vector4 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Node);
        }

        public Matrix4Node()
        {
            name = "Matrix 4x4";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row0
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row1
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m2", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row2
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m3", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row3
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("$precision4 _{0}_m0 = $precision4 ({1}, {2}, {3}, {4});", GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row0.x),
                    NodeUtils.FloatToShaderValue(m_Row0.y),
                    NodeUtils.FloatToShaderValue(m_Row0.z),
                    NodeUtils.FloatToShaderValue(m_Row0.w));
                sb.AppendLine("$precision4 _{0}_m1 = $precision4 ({1}, {2}, {3}, {4});", GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row1.x),
                    NodeUtils.FloatToShaderValue(m_Row1.y),
                    NodeUtils.FloatToShaderValue(m_Row1.z),
                    NodeUtils.FloatToShaderValue(m_Row1.w));
                sb.AppendLine("$precision4 _{0}_m2 = $precision4 ({1}, {2}, {3}, {4});", GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row2.x),
                    NodeUtils.FloatToShaderValue(m_Row2.y),
                    NodeUtils.FloatToShaderValue(m_Row2.z),
                    NodeUtils.FloatToShaderValue(m_Row2.w));
                sb.AppendLine("$precision4 _{0}_m3 = $precision4 ({1}, {2}, {3}, {4});", GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row3.x),
                    NodeUtils.FloatToShaderValue(m_Row3.y),
                    NodeUtils.FloatToShaderValue(m_Row3.z),
                    NodeUtils.FloatToShaderValue(m_Row3.w));
            }
            sb.AppendLine("$precision4x4 {0} = $precision4x4 (_{0}_m0.x, _{0}_m0.y, _{0}_m0.z, _{0}_m0.w, _{0}_m1.x, _{0}_m1.y, _{0}_m1.z, _{0}_m1.w, _{0}_m2.x, _{0}_m2.y, _{0}_m2.z, _{0}_m2.w, _{0}_m3.x, _{0}_m3.y, _{0}_m3.z, _{0}_m3.w);",
                GetVariableNameForNode());
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = string.Format("_{0}_m0", GetVariableNameForNode()),
                vector4Value = m_Row0
            });

            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = string.Format("_{0}_m1", GetVariableNameForNode()),
                vector4Value = m_Row1
            });

            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = string.Format("_{0}_m2", GetVariableNameForNode()),
                vector4Value = m_Row2
            });

            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = string.Format("_{0}_m3", GetVariableNameForNode()),
                vector4Value = m_Row3
            });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new Matrix4ShaderProperty
            {
                value = new Matrix4x4()
                {
                    m00 = row0.x,
                    m01 = row0.y,
                    m02 = row0.z,
                    m03 = row0.w,
                    m10 = row1.x,
                    m11 = row1.y,
                    m12 = row1.z,
                    m13 = row1.w,
                    m20 = row2.x,
                    m21 = row2.y,
                    m22 = row2.z,
                    m23 = row2.w,
                    m30 = row3.x,
                    m31 = row3.y,
                    m32 = row3.z,
                    m33 = row3.w,
                }
            };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}

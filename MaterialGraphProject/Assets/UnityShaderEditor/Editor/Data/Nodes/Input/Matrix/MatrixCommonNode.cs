using System.ComponentModel;
using UnityEngine.Graphing;
using System.Collections.Generic;
using UnityEditor.MaterialGraph.Drawing.Controls;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Matrix/CommonMatrix")]
    public class MatrixCommonNode : AbstractMaterialNode
    {
        static Dictionary<CommonMatrixType, string> m_matrixList = new Dictionary<CommonMatrixType, string>
        {
            {CommonMatrixType.ModelView, "UNITY_MATRIX_MV"},
            {CommonMatrixType.View, "UNITY_MATRIX_V"},
            {CommonMatrixType.Projection, "UNITY_MATRIX_P"},
            {CommonMatrixType.ViewProjection, "UNITY_MATRIX_VP"},
            {CommonMatrixType.TransposeModelView, "UNITY_MATRIX_T_MV"},
            {CommonMatrixType.InverseTransposeModelView, "UNITY_MATRIX_IT_MV"},
            {CommonMatrixType.ObjectToWorld, "unity_ObjectToWorld"},
            {CommonMatrixType.WorldToObject, "unity_WorldToObject"},
        };

        [SerializeField]
        private CommonMatrixType m_matrix = CommonMatrixType.ModelView;

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Output";

        public override bool hasPreview { get { return false; } }

        [EnumControl("")]
        public CommonMatrixType matrix
        {
            get { return m_matrix; }
            set
            {
                if (m_matrix == value)
                    return;

                m_matrix = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public MatrixCommonNode()
        {
            name = "CommonMatrix";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Matrix4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return m_matrixList[matrix];
        }

        public bool RequiresVertexColor()
        {
            return true;
        }
    }
}

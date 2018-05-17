using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum TransformationMatrixType
    {
        ModelView,
        View,
        Projection,
        ViewProjection,
        TransposeModelView,
        InverseTransposeModelView,
        ObjectToWorld,
        WorldToObject
    };

    [Title("Input", "Matrix", "Transformation Matrix")]
    public class TransformationMatrixNode : AbstractMaterialNode
    {
        static Dictionary<TransformationMatrixType, string> m_matrixList = new Dictionary<TransformationMatrixType, string>
        {
            {TransformationMatrixType.ModelView, "UNITY_MATRIX_MV"},
            {TransformationMatrixType.View, "UNITY_MATRIX_V"},
            {TransformationMatrixType.Projection, "UNITY_MATRIX_P"},
            {TransformationMatrixType.ViewProjection, "UNITY_MATRIX_VP"},
            {TransformationMatrixType.TransposeModelView, "UNITY_MATRIX_T_MV"},
            {TransformationMatrixType.InverseTransposeModelView, "UNITY_MATRIX_IT_MV"},
            {TransformationMatrixType.ObjectToWorld, "UNITY_MATRIX_M"},
            {TransformationMatrixType.WorldToObject, "UNITY_MATRIX_I_M"},
        };

        [SerializeField]
        private TransformationMatrixType m_matrix = TransformationMatrixType.ModelView;

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        [EnumControl("")]
        public TransformationMatrixType matrix
        {
            get { return m_matrix; }
            set
            {
                if (m_matrix == value)
                    return;

                m_matrix = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public TransformationMatrixNode()
        {
            name = "Transformation Matrix";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Transformation-Matrix-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return m_matrixList[matrix].ToString(CultureInfo.InvariantCulture);
        }

        public bool RequiresVertexColor()
        {
            return true;
        }
    }
}

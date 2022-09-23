using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// MatrixPart is a node part that displays a static matrix input.
    /// </summary>
    class MatrixPart : AbstractStaticPortPart
    {
        MatrixField m_MatrixField;
        readonly int m_Size;

        public override VisualElement Root => m_MatrixField;

        public MatrixPart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName, string portName, int size)
            : base(name, model, ownerElement, parentClassName, portName)
        {
            m_Size = size;
        }

        void OnMatrixChanged(ChangeEvent<Matrix4x4> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            var values = new float[m_Size * m_Size];
            var write = 0;
            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    values[write++] = change.newValue[j, i];
                }
            }

            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                (GraphType.Length)m_Size,
                (GraphType.Height)m_Size,
                values));
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_MatrixField = new MatrixField(size: m_Size) {name = PartName};
            m_MatrixField.RegisterValueChangedCallback(OnMatrixChanged);

            parent.Add(m_MatrixField);
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            var typeField = reader.GetTypeField();
            var matrixValue = m_Size switch
            {
                4 => GraphTypeHelpers.GetAsMat4(typeField),
                3 => GraphTypeHelpers.GetAsMat3(typeField),
                2 => GraphTypeHelpers.GetAsMat2(typeField),
                _ => Matrix4x4.zero,
            };

            m_MatrixField.SetValueWithoutNotify(matrixValue);
        }
    }
}

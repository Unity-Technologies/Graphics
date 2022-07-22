using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// General-purpose field for editing 2x2, 3x3, and 4x4 square matrices.
    ///
    /// The value of this field is always a Matrix4x4. When the field's size is less than 4, the resulting matrix is
    /// padded with zeros on the right and bottom.
    /// </summary>
    public class MatrixField : BaseField<Matrix4x4>
    {
        const string k_MatrixPartTemplate = "StaticPortParts/MatrixPart";
        const string k_MatrixRowClass = "sg-matrix-row";
        const string k_MatrixElementClass = "sg-matrix-element";
        const string k_MatrixContainerName = "sg-matrix-container";

        public FloatField[,] matrixElementFields { get; private set; }
        readonly int m_Size;

        public override void SetValueWithoutNotify(Matrix4x4 newValue)
        {
            rawValue = newValue;

            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    matrixElementFields[i, j].SetValueWithoutNotify(newValue[i, j]);
                }
            }
        }

        public MatrixField(string label = null, int size = 4)
            : base(label, null)
        {
            if (size is < 2 or > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Matrix size must be 2, 3, or 4");
            }

            m_Size = size;

            var input = this.Q(className: inputUssClassName);
            GraphElementHelper.LoadTemplateAndStylesheet(input, k_MatrixPartTemplate, "");
            CreateMatrixElementUIs(input.Q(k_MatrixContainerName), size);
        }

        void CreateMatrixElementUIs(VisualElement container, int size)
        {
            matrixElementFields = new FloatField[size, size];

            for (var i = 0; i < size; i++)
            {
                var rowVisualElement = new VisualElement();
                rowVisualElement.AddToClassList(k_MatrixRowClass);

                for (var j = 0; j < size; j++)
                {
                    var fieldVisualElement = new FloatField {name = $"{k_MatrixElementClass}-{i}-{j}", value = value[i, j], isDelayed = true};

                    fieldVisualElement.AddToClassList(k_MatrixElementClass);
                    fieldVisualElement.RegisterValueChangedCallback(OnMatrixElementChanged);
                    fieldVisualElement.tooltip = $"Element {i}, {j}";

                    matrixElementFields[i, j] = fieldVisualElement;
                    rowVisualElement.Add(fieldVisualElement);
                }

                container.Add(rowVisualElement);
            }
        }

        void OnMatrixElementChanged(ChangeEvent<float> change)
        {
            change.StopPropagation();

            var oldValue = rawValue;
            var newValue = new Matrix4x4();
            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    newValue[i, j] = matrixElementFields[i, j].value;
                }
            }

            using var changeEvent = ChangeEvent<Matrix4x4>.GetPooled(oldValue, newValue);
            changeEvent.target = this;
            SendEvent(changeEvent);
        }
    }
}

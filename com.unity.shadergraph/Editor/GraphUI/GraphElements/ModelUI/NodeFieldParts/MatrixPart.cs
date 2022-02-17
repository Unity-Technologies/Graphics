using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// MatrixPart is a node part that displays a static matrix input.
    /// </summary>
    public class MatrixPart : BaseModelUIPart
    {
        const string k_MatrixPartTemplate = "NodeFieldParts/MatrixPart";
        const string k_MatrixRowClass = "sg-matrix-row";
        const string k_MatrixElementClass = "sg-matrix-element";
        const string k_MatrixContainerName = "sg-matrix-container";

        VisualElement m_Root;
        FloatField[,] m_MatrixElementFields; // row, col

        public override VisualElement Root => m_Root;

        // TODO: Need to specify what field
        public MatrixPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        void CreateMatrixElementUIs(VisualElement container, int size)
        {
            m_MatrixElementFields = new FloatField[size, size];

            // TODO: Confirm our indexing, row/column major?
            for (var i = 0; i < size; i++)
            {
                var rowVisualElement = new VisualElement();
                rowVisualElement.AddToClassList(k_MatrixRowClass);

                for (var j = 0; j < size; j++)
                {
                    // Need to copy values so changes don't get captured
                    int row = i, col = j;

                    // TODO: Read the default/existing value instead of setting to 0
                    var fieldVisualElement = new FloatField {name = $"sg-matrix-element-{row}-{col}", value = 0};

                    fieldVisualElement.AddToClassList(k_MatrixElementClass);
                    fieldVisualElement.RegisterValueChangedCallback(c => OnMatrixElementChanged(row, col, c));
                    fieldVisualElement.tooltip = $"Element {row}, {col}";

                    m_MatrixElementFields[i, j] = fieldVisualElement;
                    rowVisualElement.Add(fieldVisualElement);
                }

                container.Add(rowVisualElement);
            }
        }

        void OnMatrixElementChanged(int row, int col, ChangeEvent<float> change)
        {
            // TODO: Get the node writer and update the user's data
            Debug.Log($"Matrix element {row}, {col} is now {change.newValue}");
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, k_MatrixPartTemplate, PartName);

            // TODO: Determine from data
            const int size = 4;
            CreateMatrixElementUIs(m_Root.Q<VisualElement>(k_MatrixContainerName), size);

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: Recreate matrix controls if necessary?
            //  Delete elements and call CreateMatrixElementUIs again

            // TODO: Update values
            //  i.e., change at (i, j): m_MatrixElementFields[i, j].value = newValue

            if (m_Model is not GraphDataNodeModel model) return;
            if (!model.TryGetNodeReader(out var reader)) return;
        }
    }
}

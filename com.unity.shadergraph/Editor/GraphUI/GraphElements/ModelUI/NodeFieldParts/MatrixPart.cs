using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
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
        string m_PortName;
        int m_Size;

        public override VisualElement Root => m_Root;

        public MatrixPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName, int size)
            : base(name, model, ownerElement, parentClassName)
        {
            m_PortName = portName;
            m_Size = size;
        }

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
                    var fieldVisualElement = new FloatField {name = $"sg-matrix-element-{i}-{j}", value = 0};

                    fieldVisualElement.AddToClassList(k_MatrixElementClass);
                    fieldVisualElement.RegisterValueChangedCallback(OnMatrixElementChanged);
                    fieldVisualElement.tooltip = $"Element {i}, {j}";

                    m_MatrixElementFields[i, j] = fieldVisualElement;
                    rowVisualElement.Add(fieldVisualElement);
                }

                container.Add(rowVisualElement);
            }
        }

        void OnMatrixElementChanged(ChangeEvent<float> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            var values = new float[m_Size * m_Size];
            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    var flatIndex = i * m_Size + j;
                    values[flatIndex] = m_MatrixElementFields[i, j].value;
                }
            }

            m_OwnerElement.View.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, m_PortName, values));
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, k_MatrixPartTemplate, PartName);

            CreateMatrixElementUIs(m_Root.Q<VisualElement>(k_MatrixContainerName), m_Size);

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel model) return;
            if (!model.TryGetNodeReader(out var nodeReader)) return;
            if (!nodeReader.TryGetPort(m_PortName, out var portReader)) return;

            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    var flatIndex = i * m_Size + j;
                    if (!portReader.GetField($"c{flatIndex}", out float value)) value = 0;
                    m_MatrixElementFields[i, j].SetValueWithoutNotify(value);
                }
            }
        }
    }
}

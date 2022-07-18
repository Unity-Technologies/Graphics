using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO: Most of this is directly copied from the static port part while testing.
    // Factor it out properly, perhaps as a VisualElement?
    public class MatrixConstantPropertyField : CustomizableModelPropertyField
    {
        const string k_MatrixPartTemplate = "StaticPortParts/MatrixPart";
        const string k_MatrixRowClass = "sg-matrix-row";
        const string k_MatrixElementClass = "sg-matrix-element";
        const string k_MatrixContainerName = "sg-matrix-container";

        public MatrixConstantPropertyField(
            IConstant constantModel,
            IGraphElementModel owner,
            ICommandTarget commandTarget,
            int size,
            string label = "")
            : base(commandTarget, label)
        {
            m_Root = new VisualElement();
            m_ConstantModel = constantModel;
            m_Owner = owner;
            m_Size = size;

            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, k_MatrixPartTemplate, "");

            CreateMatrixElementUIs(m_Root.Q<VisualElement>(k_MatrixContainerName), m_Size);

            Add(m_Root);
        }

        FloatField[,] m_MatrixElementFields; // row, col
        readonly IConstant m_ConstantModel;
        readonly IGraphElementModel m_Owner;
        readonly int m_Size;
        readonly VisualElement m_Root;

        void CreateMatrixElementUIs(VisualElement container, int size)
        {
            m_MatrixElementFields = new FloatField[size, size];

            for (var i = 0; i < size; i++)
            {
                var rowVisualElement = new VisualElement();
                rowVisualElement.AddToClassList(k_MatrixRowClass);

                for (var j = 0; j < size; j++)
                {
                    // UpdatePartFromModel will immediately update field's value.
                    var fieldVisualElement = new FloatField {name = $"{k_MatrixElementClass}-{i}-{j}", value = 0};

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
            var value = new Matrix4x4();
            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    value[i, j] = m_MatrixElementFields[i, j].value;
                }
            }

            CommandTarget.Dispatch(new UpdateConstantValueCommand(
                m_ConstantModel,
                value,
                m_Owner
            ));
        }

        public override bool UpdateDisplayedValue()
        {
            var value = (Matrix4x4)m_ConstantModel.ObjectValue;
            for (var i = 0; i < m_Size; i++)
            {
                for (var j = 0; j < m_Size; j++)
                {
                    m_MatrixElementFields[i, j].SetValueWithoutNotify(value[i, j]);
                }
            }

            return true; // TODO
        }
    }
}

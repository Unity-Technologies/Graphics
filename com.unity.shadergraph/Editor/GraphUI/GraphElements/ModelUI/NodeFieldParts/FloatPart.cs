using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class FloatPart : BaseModelUIPart
    {
        const string k_FloatPartTemplate = "NodeFieldParts/FloatPart";
        const string k_FloatFieldName = "sg-float-field";

        public FloatPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_PortName = portName;
        }

        VisualElement m_Root;
        FloatField m_FloatField;
        string m_PortName;

        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_FloatPartTemplate);

            m_FloatField = m_Root.Q<FloatField>(k_FloatFieldName);
            m_FloatField.RegisterValueChangedCallback(change =>
            {
                if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
                m_OwnerElement.View.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, m_PortName, (GraphType.Length)1, (GraphType.Height)1, new float[] {change.newValue}));
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel model) return;
            if (!model.TryGetNodeReader(out var nodeReader)) return;
            if (!nodeReader.TryGetPort(m_PortName, out var portReader)) return;

            if (!portReader.GetField("c0", out float value)) value = 0;
            m_FloatField.value = value;
        }
    }
}

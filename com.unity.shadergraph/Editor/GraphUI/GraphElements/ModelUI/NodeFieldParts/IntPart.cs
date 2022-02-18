using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class IntPart : BaseModelUIPart
    {
        const string k_IntPartTemplate = "NodeFieldParts/IntPart";
        const string k_IntFieldName = "sg-int-field";

        public IntPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_PortName = portName;
        }

        string m_PortName;
        VisualElement m_Root;
        IntegerField m_IntegerField;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_IntPartTemplate);

            m_IntegerField = m_Root.Q<IntegerField>(k_IntFieldName);
            m_IntegerField.RegisterValueChangedCallback(change =>
            {
                if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
                m_OwnerElement.View.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, m_PortName, (GraphType.Length)1, (GraphType.Height)1, new float[] { change.newValue }));
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel model) return;
            if (!model.TryGetNodeReader(out var nodeReader)) return;
            if (!nodeReader.TryGetPort(m_PortName, out var portReader)) return;

            if (!portReader.GetField("c0", out float value)) value = 0;
            m_IntegerField.value = (int) value;
        }
    }
}

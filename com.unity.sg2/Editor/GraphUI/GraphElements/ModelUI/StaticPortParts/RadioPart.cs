using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class RadioPart : SingleFieldPart<RadioButtonGroup, int>
    {
        protected override string UXMLTemplateName => "StaticPortParts/RadioPart";
        protected override string FieldName => "sg-radio";

        public RadioPart(
            string name,
            GraphElementModel model,
            ModelView ownerElement,
            string parentClassName,
            string portName
        ) : base(name, model, ownerElement, parentClassName, portName)
        {
        }

        protected override void OnFieldValueChanged(ChangeEvent<int> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                GraphType.Length.One,
                GraphType.Height.One,
                change.newValue));
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            if (!reader.GetTypeField().GetField("c0", out int value)) value = 0;
            m_Field.SetValueWithoutNotify(value);
        }
    }
}

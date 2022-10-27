using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class FloatPart : SingleFieldPart<FloatField, float>
    {
        protected override string UXMLTemplateName => "StaticPortParts/FloatPart";
        protected override string FieldName => "sg-float-field";

        public FloatPart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

        protected override void OnFieldValueChanged(ChangeEvent<float> change)
        {
            if (m_Model is not SGNodeModel graphDataNodeModel) return;
            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                GraphType.Length.One,
                GraphType.Height.One,
                change.newValue));
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            var field = reader.GetTypeField();
            var value = field != null ? GraphTypeHelpers.GetAsFloat(field) : 0;
            m_Field.SetValueWithoutNotify(value);
        }
    }
}

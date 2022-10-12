using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class IntPart : SingleFieldPart<IntegerField, int>
    {
        protected override string UXMLTemplateName =>"StaticPortParts/IntPart";

        protected override string FieldName => "sg-int-field";

        public IntPart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

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
            var field = reader.GetTypeField();
            var value = field != null ? GraphTypeHelpers.GetAsInt(field) : 0;
            m_Field.SetValueWithoutNotify(value);
        }
    }
}

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class TextFieldPart : SingleFieldPart<TextField, string>
    {
        protected override string UXMLTemplateName => "StaticPortParts/DropdownPart";
        protected override string FieldName => "sg-dropdown";

        public TextFieldPart(
            string name,
            IGraphElementModel model,
            IModelUI ownerElement,
            string parentClassName,
            string portName
        ) : base(name, model, ownerElement, parentClassName, portName)
        {
        }

        protected override void OnFieldValueChanged(ChangeEvent<string> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            // TODO (Brett) Figure out how / when we store strings
            // ---
            //m_OwnerElement.View.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
            //    m_PortName,
            //    GraphType.Length.One,
            //    GraphType.Height.One,
            //    change.newValue));
        }

        protected override void UpdatePartFromPortReader(IPortReader reader)
        {
            // TODO (Brett) Figure out how / when we store strings
            // ----
            //if (!reader.GetField("c0", out float value)) value = "";
            //m_Field.SetValueWithoutNotify(value);
        }
    }
}

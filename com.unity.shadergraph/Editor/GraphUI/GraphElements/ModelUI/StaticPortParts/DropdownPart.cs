using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

// TODO (Brett) Refactor this. Implement the 3-tiered combo box strat.
// TODO (Brett) 3-tiered Combo Box Strat
// TODO (Brett) - UIHint for combo boxes to allow default value selection for fields
// TODO (Brett) - Multiple FunctionDescriptors allowed in a node (with keys)
// TODO (Brett) - Expose enum types with Enum Visual Element (See: Enum).

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class DropdownPart : SingleFieldPart<DropdownField, string>
    {
        protected override string UXMLTemplateName => "StaticPortParts/DropdownPart";
        protected override string FieldName => "sg-dropdown";

        public DropdownPart(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName,
            string portName
        ) : base(name, model, ownerElement, parentClassName, portName)
        {
        }

        protected override void OnFieldValueChanged(ChangeEvent<string> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            // TODO (Brett) Turn the field value into the format that the node can handle.
            // ---
            //m_OwnerElement.View.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
            //    m_PortName,
            //    GraphType.Length.One,
            //    GraphType.Height.One,
            //    change.newValue));
        }

        protected override void UpdatePartFromPortReader(IPortReader reader)
        {
            // TODO (Brett) Read from the field and see which option that maps to.
            // TODO (Brett) Set the value and notify.
            // ----
            //if (!reader.GetField("c0", out float value)) value = "";
            //m_Field.SetValueWithoutNotify(value);
        }
    }
}

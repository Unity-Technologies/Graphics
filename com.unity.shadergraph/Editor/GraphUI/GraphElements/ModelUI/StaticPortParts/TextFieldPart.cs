using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

// TODO (Brett) Remove this. Implement the dynamic topology for Swizzle.
// TODO (Brett) This field type was added specifically for a Swizzle implementation.
// TODO (Brett) However, a generic UI or field type should not be supported here.
// TODO (Brett) Instead, Swizzle should be given a specific treatment.

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class TextFieldPart : SingleFieldPart<TextField, string>
    {
        protected override string UXMLTemplateName => "StaticPortParts/TextFieldPart";
        protected override string FieldName => "sg-textfield";

        public TextFieldPart(
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

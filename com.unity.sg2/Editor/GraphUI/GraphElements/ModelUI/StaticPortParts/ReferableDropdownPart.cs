using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ReferableDropdownPart : SingleFieldPart<DropdownField, string>
    {
        protected override string UXMLTemplateName => "StaticPortParts/DropdownPart";
        protected override string FieldName => "sg-dropdown";

        List<(string, object)> m_Options;

        public ReferableDropdownPart(
            string name,
            GraphElementModel model,
            ModelView ownerElement,
            string parentClassName,
            string portName
        ) : base(name, model, ownerElement, parentClassName, portName)
        {
            if (m_Model is not SGNodeModel sgNodeModel) return;

            m_Options = sgNodeModel.GetViewModel().GetParameterInfo(m_PortName).Options;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            base.BuildPartUI(parent);
            m_Field.choices = m_Options.Select(t => t.Item1).ToList();
            m_Field.index = 0;
        }

        protected override void OnFieldValueChanged(ChangeEvent<string> change)
        {
            if (m_Model is not SGNodeModel sgNodeModel) return;

            var newOption = m_Options.First(t => t.Item1.Equals(change.newValue));
            if (newOption.Item2 is not ReferenceValueDescriptor desc) return;
            m_OwnerElement.RootView.Dispatch(new SetReferableDropdownCommand(sgNodeModel, m_PortName, desc));

        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            if (m_Model is not SGNodeModel sgNodeModel) return;

            var desc = sgNodeModel.GetReferableDropdown(m_PortName);
            if (desc < 0) return;

            m_Field.SetValueWithoutNotify(m_Field.choices[desc]);
        }
    }
}

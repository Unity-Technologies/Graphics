using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // Non-static port options are handled by PortOptionsPropertyField.
    class StaticPortOptionsPart : SingleFieldPart<DropdownField, string>
    {
        protected override string UXMLTemplateName => "StaticPortParts/DropdownPart";
        protected override string FieldName => "sg-dropdown";

        List<(string, object)> m_Options;

        public StaticPortOptionsPart(
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
            m_OwnerElement.RootView.Dispatch(new SetPortOptionCommand(sgNodeModel, m_PortName, m_Field.index));
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            if (m_Model is not SGNodeModel sgNodeModel) return;

            var desc = sgNodeModel.GetCurrentPortOption(m_PortName);
            if (desc < 0) return;

            m_Field.SetValueWithoutNotify(m_Field.choices[desc]);
        }
    }
}

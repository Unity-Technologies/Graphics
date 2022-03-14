using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GradientPart : SingleFieldPart<GradientField, Gradient>
    {
        protected override string UXMLTemplateName => "StaticPortParts/GradientPart";
        protected override string FieldName => "sg-gradient-field";

        public GradientPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

        protected override void UpdatePartFromPortReader(IPortReader reader)
        {
            var gradient = GradientTypeHelpers.GetGradient((IFieldReader)reader);
            m_Field.SetValueWithoutNotify(gradient);
        }

        protected override void OnFieldValueChanged(ChangeEvent<Gradient> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            m_OwnerElement.View.Dispatch(new SetGradientTypeValueCommand(graphDataNodeModel, m_PortName, change.newValue));
        }
    }
}

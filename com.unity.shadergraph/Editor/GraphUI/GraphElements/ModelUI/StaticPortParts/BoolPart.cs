using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class BoolPart : SingleFieldPart<Toggle, bool>
    {
        protected override string UXMLTemplateName => "StaticPortParts/BoolPart";
        protected override string FieldName => "sg-bool-field";

        public BoolPart(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName,
            string portName
        ) : base(name, model, ownerElement, parentClassName, portName) { }

        protected override void OnFieldValueChanged(ChangeEvent<bool> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            m_OwnerElement.RootView.Dispatch(
                new SetGraphTypeValueCommand(graphDataNodeModel,
                    m_PortName,
                    GraphType.Length.One,
                    GraphType.Height.One,
                    change.newValue ? 1f : 0f
                )
            );
        }

        protected override void UpdatePartFromPortReader(IPortReader reader)
        {
            if (!reader.GetField("c0", out float value)) value = 0;
            bool v = !Mathf.Approximately(value, 0F);
            m_Field.SetValueWithoutNotify(v);
        }
    }
}

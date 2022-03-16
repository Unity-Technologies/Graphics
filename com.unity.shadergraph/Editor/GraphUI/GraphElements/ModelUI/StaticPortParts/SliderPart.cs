using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SliderPart : SingleFieldPart<Slider, float>
    {
        protected override string UXMLTemplateName => "StaticPortParts/SliderPart";
        protected override string FieldName => "sg-slider";

        public SliderPart(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName,
            string portName
        ) : base(name, model, ownerElement, parentClassName, portName)
        {
        }

        protected override void OnFieldValueChanged(ChangeEvent<float> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                GraphType.Length.One,
                GraphType.Height.One,
                change.newValue));
        }

        protected override void UpdatePartFromPortReader(IPortReader reader)
        {
            if (!reader.GetField("c0", out float value)) value = 0;
            m_Field.SetValueWithoutNotify(value);
        }
    }
}

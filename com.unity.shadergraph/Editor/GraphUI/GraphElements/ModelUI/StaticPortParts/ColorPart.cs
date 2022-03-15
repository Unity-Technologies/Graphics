using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ColorPart : SingleFieldPart<ColorField, Color>
    {
        protected override string UXMLTemplateName => "StaticPortParts/ColorPart";
        protected override string FieldName => "sg-color-field";

        bool m_IncludeAlpha;
        int length => m_IncludeAlpha ? 4 : 3;

        public ColorPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, string portName, bool includeAlpha)
            : base(name, model, ownerElement, parentClassName, portName)
        {
            m_IncludeAlpha = includeAlpha;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            base.BuildPartUI(parent);
            m_Field.showAlpha = m_IncludeAlpha;
            m_Field.AddStylesheet("StaticPortParts/ColorPart.uss");
        }

        protected override void UpdatePartFromPortReader(IPortReader reader)
        {
            var newColor = new Color();

            for (var i = 0; i < length; i++)
            {
                if (!reader.GetField($"c{i}", out float component)) continue;
                newColor[i] = component;
            }

            m_Field.SetValueWithoutNotify(newColor);
        }

        protected override void OnFieldValueChanged(ChangeEvent<Color> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            var values = new float[length];
            for (var i = 0; i < length; i++)
            {
                values[i] = change.newValue[i];
            }

            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                (GraphType.Length)length,
                GraphType.Height.One,
                values));
        }
    }
}

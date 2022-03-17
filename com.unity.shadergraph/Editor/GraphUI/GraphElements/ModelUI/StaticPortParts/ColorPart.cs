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

        public ColorPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName, bool includeAlpha)
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

        protected override void UpdatePartFromPortReader(PortHandler port)
        {
            var newColor = new Color();
            var reader = port.GetTypeField();

            for (var i = 0; i < length; i++)
            {
                var componentField = reader.GetSubField<float>($"c{i}");
                if (componentField == null)
                    continue;
                newColor[i] = componentField.GetData();
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

            m_OwnerElement.View.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                (GraphType.Length)length,
                GraphType.Height.One,
                values));
        }
    }
}

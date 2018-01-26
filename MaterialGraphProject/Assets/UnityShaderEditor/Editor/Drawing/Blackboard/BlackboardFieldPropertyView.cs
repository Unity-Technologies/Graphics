using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldPropertyView : VisualElement
    {
        readonly AbstractMaterialGraph m_Graph;

        public BlackboardFieldPropertyView(AbstractMaterialGraph graph, IShaderProperty property)
        {
            m_Graph = graph;
            if (property is FloatShaderProperty)
            {
                var floatProperty = (FloatShaderProperty)property;
                var field = new FloatField { value = floatProperty.value };
                field.OnValueChanged(evt =>
                {
                    floatProperty.value = (float)evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
                var floatModeField = new EnumField((Enum)floatProperty.floatType);
                floatModeField.OnValueChanged(evt =>
                {
                    floatProperty.floatType = (FloatType)evt.newValue;
                    DirtyNodes();
                });
                AddRow("Mode", floatModeField);
            }
            else if (property is Vector2ShaderProperty)
            {
                var vectorProperty = (Vector2ShaderProperty)property;
                var field = new Vector2Field { value = vectorProperty.value };
                field.OnValueChanged(evt =>
                {
                    vectorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if (property is Vector3ShaderProperty)
            {
                var vectorProperty = (Vector3ShaderProperty)property;
                var field = new Vector3Field { value = vectorProperty.value };
                field.OnValueChanged(evt =>
                {
                    vectorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if (property is Vector4ShaderProperty)
            {
                var vectorProperty = (Vector4ShaderProperty)property;
                var field = new Vector4Field { value = vectorProperty.value };
                field.OnValueChanged(evt =>
                {
                    vectorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if (property is ColorShaderProperty)
            {
                var colorProperty = (ColorShaderProperty)property;
                var colorField = new ColorField { value = property.defaultValue, showEyeDropper = false, hdr = colorProperty.colorMode == ColorMode.HDR };
                colorField.OnValueChanged(evt =>
                {
                    colorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", colorField);
                var colorModeField = new EnumField((Enum)colorProperty.colorMode);
                colorModeField.OnValueChanged(evt =>
                {
                    colorProperty.colorMode = (ColorMode)evt.newValue;
                    colorField.hdr = colorProperty.colorMode == ColorMode.HDR;
                    colorField.DoRepaint();
                    DirtyNodes();
                });
                AddRow("Mode", colorModeField);
            }
            else if (property is TextureShaderProperty)
            {
                var textureProperty = (TextureShaderProperty)property;
                var field = new ObjectField { value = textureProperty.value.texture, objectType = typeof(Texture) };
                field.OnValueChanged(evt =>
                {
                    textureProperty.value.texture = (Texture)evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if (property is CubemapShaderProperty)
            {
                var cubemapProperty = (CubemapShaderProperty)property;
                var field = new ObjectField { value = cubemapProperty.value.cubemap, objectType = typeof(Cubemap) };
                field.OnValueChanged(evt =>
                {
                    cubemapProperty.value.cubemap = (Cubemap)evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if (property is BooleanShaderProperty)
            {
                var booleanProperty = (BooleanShaderProperty)property;
                Action onBooleanChanged = () => 
                { 
                    booleanProperty.value = !booleanProperty.value;
                    DirtyNodes();
                };
                var field = new Toggle(onBooleanChanged) { on = booleanProperty.value };
                AddRow("Default", field);
            }
//            AddRow("Type", new TextField());
//            AddRow("Exposed", new Toggle(null));
//            AddRow("Range", new Toggle(null));
//            AddRow("Default", new TextField());
//            AddRow("Tooltip", new TextField());

            AddToClassList("sgblackboardFieldPropertyView");
        }

        void AddRow(string labelText, VisualElement control)
        {
            VisualElement rowView = new VisualElement();

            rowView.AddToClassList("rowView");

            Label label = new Label(labelText);

            label.AddToClassList("rowViewLabel");
            rowView.Add(label);

            control.AddToClassList("rowViewControl");
            rowView.Add(control);

            Add(rowView);
        }

        void DirtyNodes()
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
                node.Dirty(ModificationScope.Node);
        }
    }
}

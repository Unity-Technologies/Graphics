using System;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Generate/Color Node")]
    class ColorNode : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        private Color m_Color;

        private const string kOutputSlotName = "Color";

        public override void Init()
        {
            base.Init();
            name = "ColorNode";
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (HasBoundProperty() || !generationMode.IsPreview())
                return;

            visitor.AddShaderProperty(new ColorPropertyChunk(GetPropertyName(), GetPropertyName(), m_Color, true));
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return GetPropertyName();
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (HasBoundProperty() || !generationMode.IsPreview())
                return;

            visitor.AddShaderChunk("float4 " + GetPropertyName() + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            // we only need to generate node code if we are using a constant... otherwise we can just refer to the property :)
            if (HasBoundProperty() || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " + GetPropertyName() + " = " + precision + "4 (" + m_Color.r + ", " + m_Color.g + ", " + m_Color.b + ", " + m_Color.a + ");", true);
        }

        public override void NodeUI()
        {
            base.NodeUI();

            EditorGUI.BeginChangeCheck();
            m_Color = EditorGUILayout.ColorField(m_Color);
            if (EditorGUI.EndChangeCheck())
            {
                var boundProp = boundProperty as ColorProperty;
                if (boundProp != null)
                {
                    boundProp.defaultColor = m_Color;
                }
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
            }
        }

        public override void BindProperty(ShaderProperty property, bool rebuildShaders)
        {
            base.BindProperty(property, rebuildShaders);

            var colorProp = property as ColorProperty;
            if (colorProp)
            {
                m_Color = colorProp.defaultColor;
            }

            if (rebuildShaders)
                RegeneratePreviewShaders();
            else
            {
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
            }
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = GetPropertyName(),
                       m_PropType = PropertyType.Color,
                       m_Color = m_Color
                   };
        }
    }
}

using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Generate/Vector 4 Node")]
    class Vector4Node : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        private Vector4 m_Value;

        public override void Init()
        {
            base.Init();
            name = "V4Node";
            AddSlot(new Slot(SlotType.OutputSlot, "Value"));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (HasBoundProperty() || !generationMode.IsPreview())
                return;

            visitor.AddShaderProperty(new VectorPropertyChunk(GetPropertyName(), GetPropertyName(), m_Value, true));
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
            if (HasBoundProperty() || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " +  GetPropertyName() + " = " + precision + "4 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ", " + m_Value.w + ");", true);
        }

        public override void NodeUI()
        {
            base.NodeUI();

            EditorGUI.BeginChangeCheck();
            m_Value = EditorGUILayout.Vector4Field("Value", m_Value, GUILayout.Width(170));
            if (EditorGUI.EndChangeCheck())
            {
                var boundProp = boundProperty as VectorProperty;
                if (boundProp != null)
                {
                    boundProp.defaultVector = m_Value;
                }
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
            }
        }

        public override void BindProperty(ShaderProperty property, bool rebuildShaders)
        {
            base.BindProperty(property, rebuildShaders);

            var vectorProp = property as VectorProperty;
            if (vectorProp)
            {
                m_Value = vectorProp.defaultVector;
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
                       m_PropType = PropertyType.Vector4,
                       m_Vector4 = m_Value
                   };
        }
    }
}

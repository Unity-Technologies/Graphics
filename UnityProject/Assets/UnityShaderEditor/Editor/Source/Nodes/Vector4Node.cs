using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Generate/Vector 4 Node")]
    class Vector4Node : PropertyNode, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector4 m_Value;

        public override void OnCreate()
        {
            base.OnCreate();
            name = "V4Node"; 
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), null));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {}

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

        public override float GetNodeUIHeight(float width)
        {
            return 2*EditorGUIUtility.singleLineHeight;
        }

        public override bool NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_Value = EditorGUI.Vector4Field(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_Value);
            if (EditorGUI.EndChangeCheck())
            {
                var boundProp = boundProperty as VectorProperty;
                if (boundProp != null)
                {
                    boundProp.defaultVector = m_Value;
                }
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
                return true;
            }
            return false;
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

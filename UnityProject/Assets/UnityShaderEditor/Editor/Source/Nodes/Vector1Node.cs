using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Generate/Vector 1 Node")]
    class Vector1Node : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        private float m_Value;

        private const string kOutputSlotName = "Value";

        public override void OnCreate()
        {
            base.OnCreate();
            name = "V1Node";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), SlotValueType.Vector1));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
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

            visitor.AddShaderChunk("float " + GetPropertyName() + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (HasBoundProperty() || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + " " + GetPropertyName() + " = " + m_Value + ";", true);
        }

        public override float GetNodeUIHeight(float width)
        {
            return 2*EditorGUIUtility.singleLineHeight;
        }

        public override bool NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_Value = EditorGUI.FloatField(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_Value);
            if (EditorGUI.EndChangeCheck())
            {
                var boundProp = boundProperty as FloatProperty;
                if (boundProp != null)
                {
                    boundProp.defaultValue = m_Value;
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

            var vectorProp = property as FloatProperty;
            if (vectorProp)
            {
                m_Value = vectorProp.defaultValue;
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
                       m_PropType = PropertyType.Float,
                       m_Float = m_Value
                   };
        }
    }
}

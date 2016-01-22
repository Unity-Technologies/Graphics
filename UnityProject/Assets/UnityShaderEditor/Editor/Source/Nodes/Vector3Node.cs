using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Generate/Vector 3 Node")]
    class Vector3Node : PropertyNode, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector3 m_Value;

        public override void OnCreate()
        {
            base.OnCreate();
            name = "V3Node"; 
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), SlotValueType.Vector3));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float3 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "3 " +  propertyName + " = " + precision + "3 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ");", true);
        }
        
        public override bool NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_Value = EditorGUI.Vector3Field(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_Value);
            if (EditorGUI.EndChangeCheck())
                return true;
            return false;
        }
        
        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = propertyName,
                       m_PropType = PropertyType.Vector3,
                       m_Vector4 = m_Value
                   };
        }
    }
}

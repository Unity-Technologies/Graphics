using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/Vector 4 Node")]
    class Vector4Node : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        public Vector4 m_Value;
       
        private void InternalValidate() 
        {
            AddSlot(new MaterialSlot(this, kOutputSlotName, kOutputSlotName, MaterialSlot.SlotType.Output, SlotValueType.Vector4, Vector4.zero));
        }

        private const string kOutputSlotName = "Value";
        
        private NodeSpecificData m_NodeSpecificData = new NodeSpecificData();

        public Vector4Node(AbstractMaterialGraph owner) : base(owner)
        {
            name = "V4Node";
            InternalValidate();
        }
        
        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float4 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " +  propertyName + " = " + precision + "4 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ", " + m_Value.w + ");", true);
        }

        public override GUIModificationType NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_Value = EditorGUI.Vector4Field(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_Value);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(owner.owner);
                return GUIModificationType.Repaint;
            }
            return GUIModificationType.None;
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = propertyName,
                       m_PropType = PropertyType.Vector4,
                       m_Vector4 = m_Value
                   };
        }
    }
}

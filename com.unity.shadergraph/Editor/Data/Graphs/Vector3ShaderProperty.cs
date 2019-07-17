using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector3ShaderProperty : VectorShaderProperty
    {
        public Vector3ShaderProperty()
        {
            displayName = "Vector3";
        }
        
        public override PropertyType propertyType => PropertyType.Vector3;
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector3Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotZId).value = value.z;
            return node;
        }
        
        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override ShaderInput Copy()
        {
            return new Vector3ShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}

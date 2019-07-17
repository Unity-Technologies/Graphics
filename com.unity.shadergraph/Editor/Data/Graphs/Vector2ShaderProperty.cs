using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector2ShaderProperty : VectorShaderProperty
    {
        public Vector2ShaderProperty()
        {
            displayName = "Vector2";
        }
        
        public override PropertyType propertyType => PropertyType.Vector2;
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector2Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotYId).value = value.y;
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
            return new Vector2ShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}

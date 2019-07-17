using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector4ShaderProperty : VectorShaderProperty
    {
        public Vector4ShaderProperty()
        {
            displayName = "Vector4";
        }
        
        public override PropertyType propertyType => PropertyType.Vector4;
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector4Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotZId).value = value.z;
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotWId).value = value.w;
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
            return new Vector4ShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}

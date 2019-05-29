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

#region ShaderValueType
        public override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Vector1;
#endregion

#region Utility
        public override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector2Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotYId).value = value.y;
            return node;
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(ConcreteSlotValueType.Vector2)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new Vector2ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
#endregion
    }
}

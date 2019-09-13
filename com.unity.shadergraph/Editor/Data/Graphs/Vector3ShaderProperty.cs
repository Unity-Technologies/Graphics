using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Vector3ShaderProperty")]
    public sealed class Vector3ShaderProperty : VectorShaderProperty
    {
        internal Vector3ShaderProperty()
        {
            displayName = "Vector3";
        }

        public override PropertyType propertyType => PropertyType.Vector3;

        internal override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector3Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotZId).value = value.z;
            return node;
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        internal override ShaderInput Copy()
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

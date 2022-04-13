using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Vector3ShaderProperty")]
    [BlackboardInputInfo(3)]
    public sealed class Vector3ShaderProperty : VectorShaderProperty
    {
        internal Vector3ShaderProperty()
        {
            displayName = "Vector3";
        }

        internal override int vectorDimension => 3;

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
                value = value,
            };
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float3, referenceName, decl, concretePrecision));
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                LegacyShaderPropertyData.UpgradeToHLSLDeclarationOverride(json, this);
                ChangeVersion(1);
            }
        }
    }
}

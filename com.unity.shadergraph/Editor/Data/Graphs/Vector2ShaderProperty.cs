using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Vector2ShaderProperty")]
    [BlackboardInputInfo(1)]
    public sealed class Vector2ShaderProperty : VectorShaderProperty
    {
        internal Vector2ShaderProperty()
        {
            displayName = "Vector2";
        }

        internal override int vectorDimension => 2;

        public override PropertyType propertyType => PropertyType.Vector2;

        internal override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector2Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotYId).value = value.y;
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
            return new Vector2ShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                precision = precision,
                overrideHLSLDeclaration = overrideHLSLDeclaration,
                hlslDeclarationOverride = hlslDeclarationOverride
            };
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float2, referenceName, decl, concretePrecision));
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

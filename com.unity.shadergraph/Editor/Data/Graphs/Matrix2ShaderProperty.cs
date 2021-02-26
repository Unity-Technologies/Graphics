using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [BlackboardInputInfo(70)]
    class Matrix2ShaderProperty : MatrixShaderProperty
    {
        public Matrix2ShaderProperty()
        {
            displayName = "Matrix2x2";
            value = Matrix4x4.identity;
        }

        public override PropertyType propertyType => PropertyType.Matrix2;

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{precisionString}2x2 {referenceName}";
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new Matrix2Node
            {
                row0 = new Vector2(value.m00, value.m01),
                row1 = new Vector2(value.m10, value.m11)
            };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                matrixValue = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new Matrix2ShaderProperty()
            {
                displayName = displayName,
                value = value,
            };
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                // all old matrices were declared global; yes even if flagged hybrid!
                // maintain old behavior on versioning, users can always change the override if they wish
                overrideHLSLDeclaration = true;
                hlslDeclarationOverride = HLSLDeclaration.Global;
                ChangeVersion(1);
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [BlackboardInputInfo(72)]
    class Matrix4ShaderProperty : MatrixShaderProperty
    {
        public Matrix4ShaderProperty()
        {
            displayName = "Matrix4x4";
            value = Matrix4x4.identity;
        }

        public override PropertyType propertyType => PropertyType.Matrix4;

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{precisionString}4x4 {referenceName}";
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new Matrix4Node
            {
                row0 = new Vector4(value.m00, value.m01, value.m02, value.m03),
                row1 = new Vector4(value.m10, value.m11, value.m12, value.m13),
                row2 = new Vector4(value.m20, value.m21, value.m22, value.m23),
                row3 = new Vector4(value.m30, value.m31, value.m32, value.m33)
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
            return new Matrix4ShaderProperty()
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

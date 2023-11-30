using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    sealed class CollisionSDFDeprecated : CollisionBase
    {
        public override string name => GetNamePrefix(behavior) + "Signed Distance Field";

        public class InputProperties
        {
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
        }
		
		public override void Sanitize(int version)
        {
            var newCollisionSDF = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionSDF, this, CollisionShapeBase.Type.SignedDistanceField);
            ReplaceModel(newCollisionSDF, this);
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in base.parameters)
                    yield return input;

                VFXExpression transform = null;
                VFXExpression SDF = null;
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                    {
                        transform = input.exp;
                        VFXExpression scale = new VFXExpressionAbs(new VFXExpressionExtractScaleFromMatrix(transform));
                        yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invScale");
                        yield return new VFXNamedExpression(VFXOperatorUtility.IsTRSMatrixZeroScaled(transform), "isZeroScaled");
                        yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(transform), "InvFieldTransform");
                    }

                    if (input.name == "DistanceField")
                        SDF = input.exp;

                }
                var w = new VFXExpressionCastUintToFloat(new VFXExpressionTextureWidth(SDF));
                var h = new VFXExpressionCastUintToFloat(new VFXExpressionTextureHeight(SDF));
                var d = new VFXExpressionCastUintToFloat(new VFXExpressionTextureDepth(SDF));
                var uvStep = VFXOperatorUtility.Reciprocal(new VFXExpressionCombine(w, h, d));
                var maxDim = VFXOperatorUtility.Max3(w, h, d);
                var textureDimScale = uvStep * new VFXExpressionCombine(maxDim, maxDim, maxDim);
                var textureDimInvScale = VFXOperatorUtility.Reciprocal(textureDimScale);
                var stepSize = VFXOperatorUtility.Reciprocal(maxDim);
                yield return new VFXNamedExpression(uvStep, "uvStep");
                yield return new VFXNamedExpression(textureDimScale, "textureDimScale");
                yield return new VFXNamedExpression(textureDimInvScale, "textureDimInvScale");
                yield return new VFXNamedExpression(stepSize, "stepSizeMeter");
            }
        }
    }
}

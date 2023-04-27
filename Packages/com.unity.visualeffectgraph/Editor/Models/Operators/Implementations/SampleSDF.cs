using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleSDF")]
    [VFXInfo(category = "Sampling")]
    class SampleSDF : VFXOperator
    {
        override public string name { get { return "Sample Signed Distance Field"; } }

        public class InputProperties
        {
            [Tooltip("Sets the Signed Distance Field texture to sample from.")]
            public Texture3D texture = null;
            [Tooltip("Sets the oriented box containing the SDF.")]
            public OrientedBox orientedBox = OrientedBox.defaultValue;
            [Tooltip("Sets the position from which to sample.")]
            public Position position = Position.defaultValue;
            [Min(0), Tooltip("Sets the mip level to sample from.")]
            public float mipLevel = 0.0f;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled value from the SDF at the specified position.")]
            public float distance = 0.0f;
            [Tooltip("Outputs the direction pointing to the closest point on the surface, from the specified position.")]
            public Vector3 direction = Vector3.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression inverseTRS = new VFXExpressionInverseTRSMatrix(inputExpression[1]);
            VFXExpression scale = new VFXExpressionExtractScaleFromMatrix(inputExpression[1]);
            VFXExpression uvw = new VFXExpressionTransformPosition(inverseTRS, inputExpression[2]) + VFXValue.Constant(new Vector3(0.5f, 0.5f, 0.5f));
            VFXExpression distanceExpr = new VFXExpressionSampleSDF(inputExpression[0], uvw, scale, inputExpression[3]);
            VFXExpression directionExpr = new VFXExpressionSampleSDFNormal(inputExpression[0], inverseTRS, uvw, inputExpression[3]) * VFXValue.Constant(new Vector3(-1.0f, -1.0f, -1.0f));

            return new[] { distanceExpr, directionExpr };
        }
    }
}

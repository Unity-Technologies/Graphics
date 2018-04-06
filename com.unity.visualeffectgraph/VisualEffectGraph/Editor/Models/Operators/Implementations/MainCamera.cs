using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "BuiltIn")]
    class MainCamera : VFXOperator
    {
        public class OutputProperties
        {
            public CameraType o;
        }

        override public string name { get { return "Main Camera"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression position = new VFXExpressionExtractPositionFromMainCamera();
            VFXExpression angles = new VFXExpressionExtractAnglesFromMainCamera();
            VFXExpression scale = new VFXExpressionExtractScaleFromMainCamera();

            VFXExpression fov = new VFXExpressionExtractFOVFromMainCamera();
            VFXExpression nearPlane = new VFXExpressionExtractNearPlaneFromMainCamera();
            VFXExpression farPlane = new VFXExpressionExtractFarPlaneFromMainCamera();
            VFXExpression aspectRatio = new VFXExpressionExtractAspectRatioFromMainCamera();

            return new[] { new VFXExpressionTRSToMatrix(position, angles, scale), fov, nearPlane, farPlane, aspectRatio };
        }
    }
}

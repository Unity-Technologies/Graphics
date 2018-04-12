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
            VFXExpression matrix = new VFXExpressionExtractMatrixFromMainCamera();
            VFXExpression fov = new VFXExpressionExtractFOVFromMainCamera();
            VFXExpression nearPlane = new VFXExpressionExtractNearPlaneFromMainCamera();
            VFXExpression farPlane = new VFXExpressionExtractFarPlaneFromMainCamera();
            VFXExpression aspectRatio = new VFXExpressionExtractAspectRatioFromMainCamera();
            VFXExpression pixelDimensions = new VFXExpressionExtractPixelDimensionsFromMainCamera();

            return new[] { matrix, fov, nearPlane, farPlane, aspectRatio, pixelDimensions };
        }
    }
}

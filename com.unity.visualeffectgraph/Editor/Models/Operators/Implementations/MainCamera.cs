using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "BuiltIn")]
    class MainCamera : VFXOperator
    {
        public class OutputProperties
        {
            public CameraType o = new CameraType();
        }

        override public string name { get { return "Main Camera"; } }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            if (slot.spaceable && slot.property.type == typeof(CameraType))
                return VFXCoordinateSpace.World;

            return (VFXCoordinateSpace)int.MaxValue;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression matrix = new VFXExpressionExtractMatrixFromMainCamera();
            VFXExpression fov = new VFXExpressionExtractFOVFromMainCamera();
            VFXExpression nearPlane = new VFXExpressionExtractNearPlaneFromMainCamera();
            VFXExpression farPlane = new VFXExpressionExtractFarPlaneFromMainCamera();
            VFXExpression aspectRatio = new VFXExpressionExtractAspectRatioFromMainCamera();
            VFXExpression pixelDimensions = new VFXExpressionExtractPixelDimensionsFromMainCamera();
            VFXExpression depthBuffer = new VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes.Depth);
            VFXExpression colorBuffer = new VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes.Color);

            return new[] { matrix, fov, nearPlane, farPlane, aspectRatio, pixelDimensions, depthBuffer, colorBuffer };
        }
    }
}

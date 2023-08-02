using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-MainCamera")]
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

            return VFXCoordinateSpace.None;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression matrix = new VFXExpressionExtractMatrixFromMainCamera();
            VFXExpression orthographic = new VFXExpressionIsMainCameraOrthographic();
            VFXExpression fov = new VFXExpressionExtractFOVFromMainCamera();
            VFXExpression nearPlane = new VFXExpressionExtractNearPlaneFromMainCamera();
            VFXExpression farPlane = new VFXExpressionExtractFarPlaneFromMainCamera();
            VFXExpression orthographicSize = new VFXExpressionGetOrthographicSizeFromMainCamera();
            VFXExpression aspectRatio = new VFXExpressionExtractAspectRatioFromMainCamera();
            VFXExpression pixelDimensions = new VFXExpressionExtractPixelDimensionsFromMainCamera();
            VFXExpression lensShift = new VFXExpressionExtractLensShiftFromMainCamera();
            VFXExpression depthBuffer = new VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes.Depth);
            VFXExpression colorBuffer = new VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes.Color);
            VFXExpression scaledPixelDimensions = new VFXExpressionExtractScaledPixelDimensionsFromMainCamera();

            return new[] { matrix, orthographic, fov, nearPlane, farPlane, orthographicSize, aspectRatio, pixelDimensions, scaledPixelDimensions, lensShift, depthBuffer, colorBuffer };
        }
    }
}

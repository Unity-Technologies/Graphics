using System.Collections.Generic;
using System.Linq;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    enum CameraMode
    {
        Main,
        Custom,
    }

    struct CameraMatricesExpressions
    {
        public VFXNamedExpression ViewToVFX;
        public VFXNamedExpression VFXToView;
        public VFXNamedExpression ViewToClip;
        public VFXNamedExpression ClipToView;
    }

    static class CameraHelper
    {
        public class CameraProperties
        {
            public CameraType Camera = CameraType.defaultValue;
        }

        public static IEnumerable<VFXNamedExpression> AddCameraExpressions(IEnumerable<VFXNamedExpression> expressions, CameraMode mode)
        {
            if (mode == CameraMode.Main)
            {
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractMatrixFromMainCamera(), "Camera_transform"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractFOVFromMainCamera(), "Camera_fieldOfView"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractNearPlaneFromMainCamera(), "Camera_nearPlane"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractFarPlaneFromMainCamera(), "Camera_farPlane"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractAspectRatioFromMainCamera(), "Camera_aspectRatio"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractPixelDimensionsFromMainCamera(), "Camera_pixelDimensions"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractLensShiftFromMainCamera(), "Camera_lensShift"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes.Depth), "Camera_depthBuffer"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes.Color), "Camera_colorBuffer"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionIsMainCameraOrthographic(), "Camera_orthographic"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionGetOrthographicSizeFromMainCamera(), "Camera_orthographicSize"));
                expressions = expressions.Append(new VFXNamedExpression(new VFXExpressionExtractScaledPixelDimensionsFromMainCamera(), "Camera_scaledPixelDimensions"));
            }

            return expressions;
        }

        public static CameraMatricesExpressions GetMatricesExpressions(IEnumerable<VFXNamedExpression> expressions, VFXCoordinateSpace cameraSpace, VFXCoordinateSpace outputSpace)
        {
            var fov = expressions.First(e => e.name == "Camera_fieldOfView");
            var aspect = expressions.First(e => e.name == "Camera_aspectRatio");
            var near = expressions.First(e => e.name == "Camera_nearPlane");
            var far = expressions.First(e => e.name == "Camera_farPlane");
            var cameraMatrix = expressions.First(e => e.name == "Camera_transform");
            var isOrtho = expressions.First(e => e.name == "Camera_orthographic");
            var orthoSize = expressions.First(e => e.name == "Camera_orthographicSize");
            var lensShift = expressions.First(e => e.name == "Camera_lensShift");

            VFXExpression ViewToVFX = cameraMatrix.exp;

            if (cameraSpace == VFXCoordinateSpace.World && outputSpace == VFXCoordinateSpace.Local)
                ViewToVFX = new VFXExpressionTransformMatrix(VFXBuiltInExpression.WorldToLocal, cameraMatrix.exp);
            else if (cameraSpace == VFXCoordinateSpace.Local && outputSpace == VFXCoordinateSpace.World)
                ViewToVFX = new VFXExpressionTransformMatrix(VFXBuiltInExpression.LocalToWorld, cameraMatrix.exp);

            VFXExpression VFXToView = new VFXExpressionInverseTRSMatrix(ViewToVFX);
            VFXExpression ViewToClip = new VFXExpressionBranch(isOrtho.exp,
                VFXOperatorUtility.GetOrthographicMatrix(orthoSize.exp, aspect.exp, near.exp, far.exp),
                VFXOperatorUtility.GetPerspectiveMatrix(fov.exp, aspect.exp, near.exp, far.exp, lensShift.exp));
            VFXExpression ClipToView = new VFXExpressionInverseMatrix(ViewToClip);

            return new CameraMatricesExpressions()
            {
                ViewToVFX = new VFXNamedExpression(ViewToVFX, "ViewToVFX"),
                VFXToView = new VFXNamedExpression(VFXToView, "VFXToView"),
                ViewToClip = new VFXNamedExpression(ViewToClip, "ViewToClip"),
                ClipToView = new VFXNamedExpression(ClipToView, "ClipToView"),
            };
        }
    }
}

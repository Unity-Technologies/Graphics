using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionExtractPositionFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractPositionFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractPositionFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
            {
                var matrix = Camera.main.cameraToWorldMatrix;
                return VFXValue.Constant<Vector3>(matrix.GetColumn(3));
            }
            else
            {
                return VFXValue.Constant(CameraType.defaultValue.transform.position);
            }
        }
    }

    class VFXExpressionExtractAnglesFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractAnglesFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractAnglesFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
            {
                var matrix = Camera.main.cameraToWorldMatrix;
                matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                return VFXValue.Constant(matrix.rotation.eulerAngles);
            }
            else
            {
                return VFXValue.Constant(CameraType.defaultValue.transform.angles);
            }
        }
    }

    class VFXExpressionExtractScaleFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractScaleFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractScaleFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
            {
                var matrix = Camera.main.cameraToWorldMatrix;
                matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                return VFXValue.Constant(matrix.lossyScale);
            }
            else
            {
                return VFXValue.Constant(CameraType.defaultValue.transform.scale);
            }
        }
    }

    class VFXExpressionExtractFOVFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractFOVFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractFOVFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(Camera.main.fieldOfView * Mathf.Deg2Rad);
            else
                return VFXValue.Constant(CameraType.defaultValue.fieldOfView);
        }
    }

    class VFXExpressionExtractNearPlaneFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractNearPlaneFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractNearPlaneFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(Camera.main.nearClipPlane);
            else
                return VFXValue.Constant(CameraType.defaultValue.nearPlane);
        }
    }

    class VFXExpressionExtractFarPlaneFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractFarPlaneFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractFarPlaneFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(Camera.main.farClipPlane);
            else
                return VFXValue.Constant(CameraType.defaultValue.farPlane);
        }
    }

    class VFXExpressionExtractAspectRatioFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractAspectRatioFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractAspectRatioFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Float;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(Camera.main.aspect);
            else
                return VFXValue.Constant(CameraType.defaultValue.aspectRatio);
        }
    }
}

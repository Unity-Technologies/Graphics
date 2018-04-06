using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionExtractMatrixFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractMatrixFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractMatrixFromMainCamera;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.Matrix4x4;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(Camera.main.cameraToWorldMatrix);
            else
                return VFXValue.Constant(CameraType.defaultValue.transform);
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

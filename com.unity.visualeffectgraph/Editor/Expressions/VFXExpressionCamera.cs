using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

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

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(Camera.main.aspect);
            else
                return VFXValue.Constant(CameraType.defaultValue.aspectRatio);
        }
    }

    class VFXExpressionExtractPixelDimensionsFromMainCamera : VFXExpression
    {
        public VFXExpressionExtractPixelDimensionsFromMainCamera() : base(VFXExpression.Flags.InvalidOnGPU)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractPixelDimensionsFromMainCamera;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (Camera.main != null)
                return VFXValue.Constant(new Vector2(Camera.main.pixelWidth, Camera.main.pixelHeight));
            else
                return VFXValue.Constant(CameraType.defaultValue.pixelDimensions);
        }
    }

    class VFXExpressionGetBufferFromMainCamera : VFXExpression
    {
        public VFXExpressionGetBufferFromMainCamera() : this(VFXCameraBufferTypes.None)
        {}

        public VFXExpressionGetBufferFromMainCamera(VFXCameraBufferTypes bufferType) : base(VFXExpression.Flags.InvalidOnGPU)
        {
            m_BufferType = bufferType;
        }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.GetBufferFromMainCamera; }}
        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents) { return VFXValue.Constant<Texture2DArray>(null); }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionGetBufferFromMainCamera)base.Reduce(reducedParents);
            newExpression.m_BufferType = m_BufferType;
            return newExpression;
        }

        protected override int[] additionnalOperands { get { return new int[] { (int)m_BufferType }; } }
        private VFXCameraBufferTypes m_BufferType;
    }
}

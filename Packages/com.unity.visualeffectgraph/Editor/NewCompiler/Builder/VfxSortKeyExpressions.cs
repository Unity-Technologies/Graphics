using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    // What the shader alone knows about the camera looking at the effect, as leaf expressions. Anything
    // built on them - a distance, a depth - is composed out of the existing operators.

    /// <summary>
    /// A world-space position as the shader compares it to the camera: camera-relative world space.
    /// </summary>
    class VfxCameraRelativePositionExpression : VFXExpression
    {
        public VfxCameraRelativePositionExpression() : this(VFXValue<Vector3>.Default)
        {
        }

        public VfxCameraRelativePositionExpression(VFXExpression positionWS)
            : base(Flags.InvalidOnCPU, new[] { positionWS })
        {
        }

        public sealed override VFXExpressionOperation operation => VFXExpressionOperation.None;
        public sealed override VFXValueType valueType => VFXValueType.Float3;

        public sealed override string GetCodeString(string[] parents) => $"GetCameraRelativePositionWS({parents[0]})";
    }

    /// <summary>The position of the camera currently looking, in camera-relative world space.</summary>
    class VfxCurrentViewPositionExpression : VFXExpression
    {
        public VfxCurrentViewPositionExpression() : base(Flags.InvalidOnCPU)
        {
        }

        public sealed override VFXExpressionOperation operation => VFXExpressionOperation.None;
        public sealed override VFXValueType valueType => VFXValueType.Float3;

        public sealed override string GetCodeString(string[] parents) => "GetCurrentViewPosition()";
    }

    /// <summary>
    /// The view-to-world matrix of the camera currently looking. Its axes are the camera's, so a camera
    /// direction is <see cref="VFXExpressionTransformDirection"/> through it.
    /// </summary>
    class VfxCurrentViewToWorldMatrixExpression : VFXExpression
    {
        public VfxCurrentViewToWorldMatrixExpression() : base(Flags.InvalidOnCPU)
        {
        }

        public sealed override VFXExpressionOperation operation => VFXExpressionOperation.None;
        public sealed override VFXValueType valueType => VFXValueType.Matrix4x4;

        public sealed override string GetCodeString(string[] parents) => "GetViewToWorldMatrix()";
    }
}

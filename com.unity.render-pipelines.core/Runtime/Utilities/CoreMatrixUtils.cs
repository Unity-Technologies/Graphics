using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

    /// <summary>
    /// Set of utility functions for the Core Scriptable Render Pipeline Library related to Matrix operations
    /// </summary>
    public static class CoreMatrixUtils
    {
        /// <summary>
        /// This function provides the equivalent of multiplying matrix parameter inOutMatrix with a translation matrix defined by the parameter translation.
        /// The order of the equivalent multiplication is inOutMatrix * translation.
        /// </summary>
        /// <param name="inOutMatrix">Matrix to multiply with translation.</param>
        /// <param name="translation">Translation component to multiply to the matrix.</param>
        public static void MatrixTimesTranslation(ref Matrix4x4 inOutMatrix, Vector3 translation)
        {
            inOutMatrix.m03 += (inOutMatrix.m00 * translation.x + inOutMatrix.m01 * translation.y + inOutMatrix.m02 * translation.z);
            inOutMatrix.m13 += (inOutMatrix.m10 * translation.x + inOutMatrix.m11 * translation.y + inOutMatrix.m12 * translation.z);
            inOutMatrix.m23 += (inOutMatrix.m20 * translation.x + inOutMatrix.m21 * translation.y + inOutMatrix.m22 * translation.z);
        }

        /// <summary>
        /// This function provides the equivalent of multiplying a translation matrix defined by the parameter translation with the matrix specified by the parameter inOutMatrix.
        /// The order of the equivalent multiplication is translation * inOutMatrix.
        /// </summary>
        /// <param name="inOutMatrix">Matrix to multiply with translation.</param>
        /// <param name="translation">Translation component to multiply to the matrix.</param>
        public static void TranslationTimesMatrix(ref Matrix4x4 inOutMatrix, Vector3 translation)
        {
            inOutMatrix.m00 += translation.x * inOutMatrix.m30;
            inOutMatrix.m01 += translation.x * inOutMatrix.m31;
            inOutMatrix.m02 += translation.x * inOutMatrix.m32;
            inOutMatrix.m03 += translation.x * inOutMatrix.m33;

            inOutMatrix.m10 += translation.y * inOutMatrix.m30;
            inOutMatrix.m11 += translation.y * inOutMatrix.m31;
            inOutMatrix.m12 += translation.y * inOutMatrix.m32;
            inOutMatrix.m13 += translation.y * inOutMatrix.m33;

            inOutMatrix.m20 += translation.z * inOutMatrix.m30;
            inOutMatrix.m21 += translation.z * inOutMatrix.m31;
            inOutMatrix.m22 += translation.z * inOutMatrix.m32;
            inOutMatrix.m23 += translation.z * inOutMatrix.m33;
        }

        /// <summary>
        /// Multiplies a matrix with a perspective matrix. This function is faster than performing the full matrix multiplication.
        /// The operation order is perspective * rhs.
        /// </summary>
        /// <param name="perspective">The perspective matrix to multiply with rhs.</param>
        /// <param name="rhs">A matrix to be multiply to perspective.</param>
        /// <returns>Returns the matrix that is the result of the multiplication.</returns>
        public static Matrix4x4 MultiplyPerspectiveMatrix(Matrix4x4 perspective, Matrix4x4 rhs)
        {
            Matrix4x4 outMat;
            outMat.m00 = perspective.m00 * rhs.m00;
            outMat.m01 = perspective.m00 * rhs.m01;
            outMat.m02 = perspective.m00 * rhs.m02;
            outMat.m03 = perspective.m00 * rhs.m03;

            outMat.m10 = perspective.m11 * rhs.m10;
            outMat.m11 = perspective.m11 * rhs.m11;
            outMat.m12 = perspective.m11 * rhs.m12;
            outMat.m13 = perspective.m11 * rhs.m13;

            outMat.m20 = perspective.m22 * rhs.m20 + perspective.m23 * rhs.m30;
            outMat.m21 = perspective.m22 * rhs.m21 + perspective.m23 * rhs.m31;
            outMat.m22 = perspective.m22 * rhs.m22 + perspective.m23 * rhs.m32;
            outMat.m23 = perspective.m22 * rhs.m23 + perspective.m23 * rhs.m33;

            outMat.m30 = -rhs.m20;
            outMat.m31 = -rhs.m21;
            outMat.m32 = -rhs.m22;
            outMat.m33 = -rhs.m23;

            return outMat;
        }

        // An orthographic projection is centered if (right+left) == 0 and (top+bottom) == 0
        private static Matrix4x4 MultiplyOrthoMatrixCentered(Matrix4x4 ortho, Matrix4x4 rhs)
        {
            Matrix4x4 outMat;
            outMat.m00 = ortho.m00 * rhs.m00;
            outMat.m01 = ortho.m00 * rhs.m01;
            outMat.m02 = ortho.m00 * rhs.m02;
            outMat.m03 = ortho.m00 * rhs.m03;

            outMat.m10 = ortho.m11 * rhs.m10;
            outMat.m11 = ortho.m11 * rhs.m11;
            outMat.m12 = ortho.m11 * rhs.m12;
            outMat.m13 = ortho.m11 * rhs.m13;

            outMat.m20 = ortho.m22 * rhs.m20 + ortho.m23 * rhs.m30;
            outMat.m21 = ortho.m22 * rhs.m21 + ortho.m23 * rhs.m31;
            outMat.m22 = ortho.m22 * rhs.m22 + ortho.m23 * rhs.m32;
            outMat.m23 = ortho.m22 * rhs.m23 + ortho.m23 * rhs.m33;

            outMat.m30 = rhs.m20;
            outMat.m31 = rhs.m21;
            outMat.m32 = rhs.m22;
            outMat.m33 = rhs.m23;

            return outMat;
        }

        // General case has m03 and m13 != 0
        private static Matrix4x4 MultiplyGenericOrthoMatrix(Matrix4x4 ortho, Matrix4x4 rhs)
        {
            Matrix4x4 outMat;
            outMat.m00 = ortho.m00 * rhs.m00 + ortho.m03 * rhs.m30;
            outMat.m01 = ortho.m00 * rhs.m01 + ortho.m03 * rhs.m31;
            outMat.m02 = ortho.m00 * rhs.m02 + ortho.m03 * rhs.m32;
            outMat.m03 = ortho.m00 * rhs.m03 + ortho.m03 * rhs.m33;

            outMat.m10 = ortho.m11 * rhs.m10 + ortho.m13 * rhs.m30;
            outMat.m11 = ortho.m11 * rhs.m11 + ortho.m13 * rhs.m31;
            outMat.m12 = ortho.m11 * rhs.m12 + ortho.m13 * rhs.m32;
            outMat.m13 = ortho.m11 * rhs.m13 + ortho.m13 * rhs.m33;

            outMat.m20 = ortho.m22 * rhs.m20 + ortho.m23 * rhs.m30;
            outMat.m21 = ortho.m22 * rhs.m21 + ortho.m23 * rhs.m31;
            outMat.m22 = ortho.m22 * rhs.m22 + ortho.m23 * rhs.m32;
            outMat.m23 = ortho.m22 * rhs.m23 + ortho.m23 * rhs.m33;

            outMat.m30 = rhs.m20;
            outMat.m31 = rhs.m21;
            outMat.m32 = rhs.m22;
            outMat.m33 = rhs.m23;
            return outMat;
        }

        /// <summary>
        /// Multiplies a matrix with an orthographic matrix. This function is faster than performing the full matrix multiplication.
        /// The operation order is ortho * rhs.
        /// </summary>
        /// <param name="ortho">The ortho matrix to multiply with rhs.</param>
        /// <param name="rhs">A matrix to be multiply to perspective.</param>
        /// <param name="centered">If true, it means that right and left are equivalently distant from center and similarly top/bottom are equivalently distant from center.</param>
        /// <returns>Returns the matrix that is the result of the multiplication.</returns>
        public static Matrix4x4 MultiplyOrthoMatrix(Matrix4x4 ortho, Matrix4x4 rhs, bool centered)
        {
            return centered ? MultiplyGenericOrthoMatrix(ortho, rhs) : MultiplyOrthoMatrixCentered(ortho, rhs);
        }

        /// <summary>
        /// Multiplies a matrix with a projection matrix. This function is faster than performing the full matrix multiplication.
        /// The operation order is projMatrix * rhs.
        /// </summary>
        /// <param name="projMatrix">The projection matrix to multiply with rhs.</param>
        /// <param name="rhs">A matrix to be multiply to perspective.</param>
        /// <param name="orthoCentered">If true, the projection matrix is a centered ( right+left == top+bottom == 0) orthographic projection, otherwise it is a perspective matrix..</param>
        /// <returns>Returns the matrix that is the result of the multiplication.</returns>
        public static Matrix4x4 MultiplyProjectionMatrix(Matrix4x4 projMatrix, Matrix4x4 rhs, bool orthoCentered)
        {
            return orthoCentered
                ? MultiplyOrthoMatrixCentered(projMatrix, rhs)
                : MultiplyPerspectiveMatrix(projMatrix, rhs);
        }
    }
}

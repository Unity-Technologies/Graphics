using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionTRSToMatrix : VFXExpression
    {
        public VFXExpressionTRSToMatrix() : this(new VFXExpression[] { VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<Vector3>.Default }
        )
        {
        }

        public VFXExpressionTRSToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TRSToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var posReduce = constParents[0];
            var rotReduce = constParents[1];
            var scaleReduce = constParents[2];

            var pos = posReduce.Get<Vector3>();
            var rot = rotReduce.Get<Vector3>();
            var scale = scaleReduce.Get<Vector3>();

            var quat = Quaternion.Euler(rot);

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(pos, quat, scale);

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GetTRSMatrix({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionInverseMatrix : VFXExpression
    {
        public VFXExpressionInverseMatrix() : this(VFXValue<Matrix4x4>.Default)
        {}

        public VFXExpressionInverseMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, parent)
        {}

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.InverseMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrix = constParents[0].Get<Matrix4x4>();
            return VFXValue.Constant(matrix.inverse);
        }
    }

    class VFXExpressionTransposeMatrix : VFXExpression
    {
        public VFXExpressionTransposeMatrix() : this(VFXValue<Matrix4x4>.Default)
        {}

        public VFXExpressionTransposeMatrix(VFXExpression parent) : base(Flags.None, parent)
        {}

        public sealed override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransposeMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrix = constParents[0].Get<Matrix4x4>();
            return VFXValue.Constant(matrix.transpose);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("transpose({0})", parents[0]);
        }
    }

    class VFXExpressionInverseTRSMatrix : VFXExpression
    {
        public VFXExpressionInverseTRSMatrix() : this(VFXValue<Matrix4x4>.Default)
        {}

        public VFXExpressionInverseTRSMatrix(VFXExpression parent) : base(VFXExpression.Flags.None, parent)
        {}

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.InverseTRSMatrix;
            }
        }

        // Invert 3D transformation matrix (not perspective). Adapted from graphics gems 2.
        // Inverts upper left by calculating its determinant and multiplying it to the symmetric
        // adjust matrix of each element. Finally deals with the translation by transforming the
        // original translation using by the calculated inverse.
        //https://github.com/erich666/GraphicsGems/blob/master/gemsii/inverse.c
        static bool inputvertMatrix4x4_General3D(Matrix4x4 input, ref Matrix4x4 output) //*WIP* This function is not exposed, we will add an helper in Matrix4 bindings
        {
#if UNITY_2019_2_OR_NEWER
            return Matrix4x4.Inverse3DAffine(input, ref output);
#else
            output = Matrix4x4.identity;

            float pos, neg, t;
            float det;

            // Calculate the determinant of upper left 3x3 sub-matrix and
            // determine if the matrix is singular.
            pos = neg = 0.0f;
            t =  input[0, 0] * input[1, 1] * input[2, 2];
            if (t >= 0.0f)
                pos += t;
            else
                neg += t;

            t =  input[1, 0] * input[2, 1] * input[0, 2];
            if (t >= 0.0f)
                pos += t;
            else
                neg += t;

            t =  input[2, 0] * input[0, 1] * input[1, 2];
            if (t >= 0.0f)
                pos += t;
            else
                neg += t;

            t = -input[2, 0] * input[1, 1] * input[0, 2];
            if (t >= 0.0f)
                pos += t;
            else
                neg += t;

            t = -input[1, 0] * input[0, 1] * input[2, 2];
            if (t >= 0.0f)
                pos += t;
            else
                neg += t;

            t = -input[0, 0] * input[2, 1] * input[1, 2];
            if (t >= 0.0f)
                pos += t;
            else
                neg += t;

            det = pos + neg;

            if (det * det < 1e-25f)
                return false;

            det = 1.0f / det;
            output[0, 0] = (input[1, 1] * input[2, 2] - input[2, 1] * input[1, 2]) * det;
            output[0, 1] = -(input[0, 1] * input[2, 2] - input[2, 1] * input[0, 2]) * det;
            output[0, 2] = (input[0, 1] * input[1, 2] - input[1, 1] * input[0, 2]) * det;
            output[1, 0] = -(input[1, 0] * input[2, 2] - input[2, 0] * input[1, 2]) * det;
            output[1, 1] = (input[0, 0] * input[2, 2] - input[2, 0] * input[0, 2]) * det;
            output[1, 2] = -(input[0, 0] * input[1, 2] - input[1, 0] * input[0, 2]) * det;
            output[2, 0] = (input[1, 0] * input[2, 1] - input[2, 0] * input[1, 1]) * det;
            output[2, 1] = -(input[0, 0] * input[2, 1] - input[2, 0] * input[0, 1]) * det;
            output[2, 2] = (input[0, 0] * input[1, 1] - input[1, 0] * input[0, 1]) * det;

            // Do the translation part
            output[0, 3] = -(input[0, 3] * output[0, 0] + input[1, 3] * output[0, 1] + input[2, 3] * output[0, 2]);
            output[1, 3] = -(input[0, 3] * output[1, 0] + input[1, 3] * output[1, 1] + input[2, 3] * output[1, 2]);
            output[2, 3] = -(input[0, 3] * output[2, 0] + input[1, 3] * output[2, 1] + input[2, 3] * output[2, 2]);

            output[3, 0] = 0.0f;
            output[3, 1] = 0.0f;
            output[3, 2] = 0.0f;
            output[3, 3] = 1.0f;

            return true;
#endif
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrix = constParents[0].Get<Matrix4x4>();

            var result = Matrix4x4.identity;
            if (!inputvertMatrix4x4_General3D(matrix, ref result))
            {
                throw new InvalidOperationException("VFXExpressionInverseTRSMatrix used on a not TRS Matrix");
            }
            return VFXValue.Constant(result);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXInverseTRSMatrix({0})", parents[0]);
        }
    }

    class VFXExpressionExtractPositionFromMatrix : VFXExpression
    {
        public VFXExpressionExtractPositionFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractPositionFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractPositionFromMatrix;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionTRSToMatrix)
                return parent.parents[0];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();

            return VFXValue.Constant<Vector3>(matrix.GetColumn(3));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}[3].xyz", parents[0]);
        }
    }

    class VFXExpressionExtractAnglesFromMatrix : VFXExpression
    {
        public VFXExpressionExtractAnglesFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractAnglesFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractAnglesFromMatrix;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionTRSToMatrix)
                return parent.parents[1];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();
            matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            return VFXValue.Constant(matrix.rotation.eulerAngles);
        }
    }

    class VFXExpressionExtractScaleFromMatrix : VFXExpression
    {
        public VFXExpressionExtractScaleFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractScaleFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractScaleFromMatrix;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionTRSToMatrix)
                return parent.parents[2];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();
            matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            return VFXValue.Constant(matrix.lossyScale);
        }
    }

    class VFXExpressionTransformMatrix : VFXExpression
    {
        public VFXExpressionTransformMatrix() : this(VFXValue<Matrix4x4>.Default, VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionTransformMatrix(VFXExpression left, VFXExpression right) : base(VFXExpression.Flags.None, new VFXExpression[] { left, right })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransformMatrix;
            }
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var left = constParents[0].Get<Matrix4x4>();
            var right = constParents[1].Get<Matrix4x4>();
            return VFXValue.Constant(left * right);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformPosition : VFXExpression
    {
        public VFXExpressionTransformPosition() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformPosition(VFXExpression matrix, VFXExpression position) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, position })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransformPos;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var position = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyPoint(position));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, float4({1}, 1.0)).xyz", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformVector : VFXExpression
    {
        public VFXExpressionTransformVector() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformVector(VFXExpression matrix, VFXExpression vector) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, vector })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransformVec;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vector = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyVector(vector));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul((float3x3){0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformVector4 : VFXExpression
    {
        public VFXExpressionTransformVector4()
            : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector4>.Default)
        {
        }

        public VFXExpressionTransformVector4(VFXExpression matrix, VFXExpression position)
            : base(VFXExpression.Flags.InvalidOnCPU, new VFXExpression[] { matrix, position }) // TODO add CPU implementation in C++
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.None;
            }
        }

        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var position = VFXValue.Constant<Vector4>(positionReduce.Get<Vector4>());

            var dstX = VFXOperatorUtility.Dot(VFXValue.Constant<Vector4>(matrix.GetRow((0))), position);
            var dstY = VFXOperatorUtility.Dot(VFXValue.Constant<Vector4>(matrix.GetRow((1))), position);
            var dstZ = VFXOperatorUtility.Dot(VFXValue.Constant<Vector4>(matrix.GetRow((2))), position);
            var dstW = VFXOperatorUtility.Dot(VFXValue.Constant<Vector4>(matrix.GetRow((3))), position);

            return new VFXExpressionCombine(dstX, dstY, dstZ, dstW);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformDirection : VFXExpression
    {
        public VFXExpressionTransformDirection() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformDirection(VFXExpression matrix, VFXExpression vector) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, vector })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransformDir;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vector = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyVector(vector).normalized);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("normalize(mul((float3x3){0}, {1}))", parents[0], parents[1]);
        }
    }

    class VFXExpressionVector3sToMatrix : VFXExpression
    {
        public VFXExpressionVector3sToMatrix() : this(new VFXExpression[] { new VFXValue<Vector3>(Vector3.right), new VFXValue<Vector3>(Vector3.up), new VFXValue<Vector3>(Vector3.forward), VFXValue<Vector3>.Default }
        )
        {
        }

        public VFXExpressionVector3sToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.Vector3sToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var xReduce = constParents[0];
            var yReduce = constParents[1];
            var zReduce = constParents[2];
            var wReduce = constParents[3];

            var x = xReduce.Get<Vector3>();
            var y = yReduce.Get<Vector3>();
            var z = zReduce.Get<Vector3>();
            var w = wReduce.Get<Vector3>();

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetColumn(0, new Vector4(x.x, x.y, x.z, 0.0f));
            matrix.SetColumn(1, new Vector4(y.x, y.y, y.z, 0.0f));
            matrix.SetColumn(2, new Vector4(z.x, z.y, z.z, 0.0f));
            matrix.SetColumn(3, new Vector4(w.x, w.y, w.z, 1.0f));

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("float4x4(float4({0}, 0.0), float4({1}, 0.0), float4({2}, 0.0), float4({3}, 1.0));", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionVector4sToMatrix : VFXExpression
    {
        public VFXExpressionVector4sToMatrix() : this(new VFXExpression[] { new VFXValue<Vector4>(new Vector4(1, 0, 0, 0)), new VFXValue<Vector4>(new Vector4(0, 1, 0, 0)), new VFXValue<Vector4>(new Vector4(0, 0, 1, 0)), new VFXValue<Vector4>(new Vector4(0, 0, 0, 1)) }
        )
        {
        }

        public VFXExpressionVector4sToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.Vector4sToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var xReduce = constParents[0];
            var yReduce = constParents[1];
            var zReduce = constParents[2];
            var wReduce = constParents[3];

            var x = xReduce.Get<Vector4>();
            var y = yReduce.Get<Vector4>();
            var z = zReduce.Get<Vector4>();
            var w = wReduce.Get<Vector4>();

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetColumn(0, x);
            matrix.SetColumn(1, y);
            matrix.SetColumn(2, z);
            matrix.SetColumn(3, w);

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("float4x4({0}, {1}, {2}, {3});", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionMatrixToVector3s : VFXExpression
    {
        public VFXExpressionMatrixToVector3s() : this(new VFXExpression[] { VFXValue<Matrix4x4>.Default, VFXValue.Constant<int>(0) } // TODO row index should not be an expression!
        )
        {
        }

        public VFXExpressionMatrixToVector3s(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.MatrixToVector3s;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matReduce = constParents[0];
            var axisReduce = constParents[1];

            var mat = matReduce.Get<Matrix4x4>();
            var axis = axisReduce.Get<int>();

            return VFXValue.Constant<Vector3>(mat.GetColumn(axis));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}[{1}].xyz", parents[0], parents[1]);
        }
    }

    class VFXExpressionMatrixToVector4s : VFXExpression
    {
        public VFXExpressionMatrixToVector4s() : this(new VFXExpression[] { VFXValue<Matrix4x4>.Default, VFXValue.Constant<int>(0) } // TODO row index should not be an expression!
        )
        {
        }

        public VFXExpressionMatrixToVector4s(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.MatrixToVector4s;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matReduce = constParents[0];
            var axisReduce = constParents[1];

            var mat = matReduce.Get<Matrix4x4>();
            var axis = axisReduce.Get<int>();

            return VFXValue.Constant(mat.GetColumn(axis));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}[{1}]", parents[0], parents[1]);
        }
    }
}

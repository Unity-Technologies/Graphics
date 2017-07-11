using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXExpressionFloatOperation : VFXExpression
    {
        protected VFXExpressionFloatOperation(VFXExpression[] parents)
            : base(Flags.None, parents)
        {
            m_AdditionalParameters = new int[] {};
        }

        static private float[] ToFloatArray(float input) { return new float[] { input }; }
        static private float[] ToFloatArray(Vector2 input) { return new float[] { input.x, input.y }; }
        static private float[] ToFloatArray(Vector3 input) { return new float[] { input.x, input.y, input.z }; }
        static private float[] ToFloatArray(Vector4 input) { return new float[] { input.x, input.y, input.z, input.w }; }
        static protected float[] ToFloatArray(VFXExpression input)
        {
            switch (input.ValueType)
            {
                case VFXValueType.kFloat: return ToFloatArray(input.Get<float>());
                case VFXValueType.kFloat2: return ToFloatArray(input.Get<Vector2>());
                case VFXValueType.kFloat3: return ToFloatArray(input.Get<Vector3>());
                case VFXValueType.kFloat4: return ToFloatArray(input.Get<Vector4>());
            }
            return null;
        }

        protected VFXExpression ToFloatN(float[] input, VFXValue.Mode mode)
        {
            switch (input.Length)
            {
                case 1: return new VFXValue<float>(input[0], mode);
                case 2: return new VFXValue<Vector2>(new Vector2(input[0], input[1]), mode);
                case 3: return new VFXValue<Vector3>(new Vector3(input[0], input[1], input[2]), mode);
                case 4: return new VFXValue<Vector4>(new Vector4(input[0], input[1], input[2], input[3]), mode);
            }
            return null;
        }

        sealed public override VFXValueType ValueType { get { return m_ValueType; } }
        sealed public override VFXExpressionOp Operation { get { return m_Operation; } }
        sealed public override int[] AdditionalParameters { get { return m_AdditionalParameters; } }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionFloatOperation)CreateNewInstance();
            newExpression.Initialize(Flags.None, reducedParents);
            newExpression.m_AdditionalParameters = m_AdditionalParameters.Select(o => o).ToArray();
            newExpression.m_Operation = m_Operation;
            newExpression.m_ValueType = m_ValueType;
            return newExpression;
        }

        protected int[] m_AdditionalParameters;
        protected VFXExpressionOp m_Operation;
        protected VFXValueType m_ValueType;
    }

    abstract class VFXExpressionUnaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionUnaryFloatOperation(VFXExpression parent, VFXExpressionOp operation) : base(new VFXExpression[1] { parent })
        {
            if (!IsFloatValueType(parent.ValueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryFloatOperation");
            }

            m_ValueType = parent.ValueType;
            m_AdditionalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var source = ToFloatArray(reducedParents[0]);
            var result = new float[source.Length];
            for (int iChannel = 0; iChannel < source.Length; ++iChannel)
            {
                result[iChannel] = ProcessUnaryOperation(source[iChannel]);
            }
            return ToFloatN(result, VFXValue.Mode.Constant);
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return GetUnaryOperationCode(parents[0]);
        }

        abstract protected float ProcessUnaryOperation(float input);

        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionBinaryFloatOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOp operation)
            : base(new VFXExpression[2] { parentLeft, parentRight })
        {
            if (!IsFloatValueType(parentLeft.ValueType) || !IsFloatValueType(parentRight.ValueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryFloatOperation (not float type)");
            }

            if (parentRight.ValueType != parentLeft.ValueType)
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryFloatOperation (incompatible float type)");
            }

            m_ValueType = parentLeft.ValueType;
            m_AdditionalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var parentLeft = reducedParents[0];
            var parentRight = reducedParents[1];

            float[] sourceLeft = ToFloatArray(parentLeft);
            float[] sourceRight = ToFloatArray(parentRight);

            var result = new float[sourceLeft.Length];
            for (int iChannel = 0; iChannel < sourceLeft.Length; ++iChannel)
            {
                result[iChannel] = ProcessBinaryOperation(sourceLeft[iChannel], sourceRight[iChannel]);
            }

            return ToFloatN(result, VFXValue.Mode.Constant);
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return GetBinaryOperationCode(parents[0], parents[1]);
        }

        protected abstract float ProcessBinaryOperation(float left, float right);
        protected abstract string GetBinaryOperationCode(string a, string b);
    }

    abstract class VFXExpressionTernaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionTernaryFloatOperation(VFXExpression a, VFXExpression b, VFXExpression c, VFXExpressionOp operation)
            : base(new VFXExpression[3] { a, b, c })
        {
            if (!IsFloatValueType(a.ValueType)
                || !IsFloatValueType(b.ValueType)
                || !IsFloatValueType(c.ValueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionTernaryFloatOperation (not float type)");
            }

            if (a.ValueType != b.ValueType || b.ValueType != c.ValueType)
            {
                throw new ArgumentException("Incorrect VFXExpressionTernaryFloatOperation (incompatible float type)");
            }

            m_ValueType = a.ValueType;
            m_AdditionalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var a = reducedParents[0];
            var b = reducedParents[1];
            var c = reducedParents[2];

            float[] source_a = ToFloatArray(a);
            float[] source_b = ToFloatArray(b);
            float[] source_c = ToFloatArray(c);

            var result = new float[source_a.Length];
            for (int iChannel = 0; iChannel < source_a.Length; ++iChannel)
            {
                result[iChannel] = ProcessTernaryOperation(source_a[iChannel], source_b[iChannel], source_c[iChannel]);
            }

            return ToFloatN(result, VFXValue.Mode.Constant);
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return GetTernaryOperationCode(parents[0], parents[1], parents[2]);
        }

        protected abstract float ProcessTernaryOperation(float a, float b, float c);
        protected abstract string GetTernaryOperationCode(string a, string b, string c);
    }
}

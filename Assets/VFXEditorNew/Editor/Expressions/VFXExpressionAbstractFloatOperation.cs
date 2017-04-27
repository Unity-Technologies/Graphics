using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXExpressionFloatOperation : VFXExpression
    {
        protected VFXExpressionFloatOperation()
        {
            m_Flags = Flags.ValidOnCPU | Flags.ValidOnGPU;
            m_Parents = new VFXExpression[] { };
            m_AdditionnalParameters = new int[] { };
        }

        static private float[] ToFloatArray(float input) { return new float[] { input }; }
        static private float[] ToFloatArray(Vector2 input) { return new float[] { input.x, input.y }; }
        static private float[] ToFloatArray(Vector3 input) { return new float[] { input.x, input.y, input.z }; }
        static private float[] ToFloatArray(Vector4 input) { return new float[] { input.x, input.y, input.z, input.w }; }
        static protected float[] ToFloatArray(VFXExpression input)
        {
            switch (input.ValueType)
            {
                case VFXValueType.kFloat: return ToFloatArray(input.GetContent<float>());
                case VFXValueType.kFloat2: return ToFloatArray(input.GetContent<Vector2>());
                case VFXValueType.kFloat3: return ToFloatArray(input.GetContent<Vector3>());
                case VFXValueType.kFloat4: return ToFloatArray(input.GetContent<Vector4>());
            }
            return null;
        }

        protected VFXExpression ToFloatN(float[] input)
        {
            switch (input.Length)
            {
                case 1: return new VFXValueFloat(input[0], true);
                case 2: return new VFXValueFloat2(new Vector2(input[0], input[1]), true);
                case 3: return new VFXValueFloat3(new Vector3(input[0], input[1], input[2]), true);
                case 4: return new VFXValueFloat4(new Vector4(input[0], input[1], input[2], input[3]), true);
            }
            return null;
        }

        sealed public override VFXValueType ValueType { get { return m_ValueType; } }
        sealed public override VFXExpressionOp Operation { get { return m_Operation; } }
        sealed public override VFXExpression[] Parents { get { return m_Parents; } }
        sealed public override int[] AdditionnalParameters { get { return m_AdditionnalParameters; } }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionFloatOperation)CreateNewInstance();
            newExpression.m_AdditionnalParameters = m_AdditionnalParameters.Select(o => o).ToArray();
            newExpression.m_Parents = reducedParents;
            newExpression.m_Operation = m_Operation;
            newExpression.m_ValueType = m_ValueType;
            return newExpression;
        }

        protected VFXExpression[] m_Parents;
        protected int[] m_AdditionnalParameters;
        protected VFXExpressionOp m_Operation;
        protected VFXValueType m_ValueType;
    }

    abstract class VFXExpressionUnaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionUnaryFloatOperation(VFXExpression parent, VFXExpressionOp operation)
        {
            if (!IsFloatValueType(parent.ValueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryFloatOperation");
            }

            m_ValueType = parent.ValueType;
            m_AdditionnalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Parents = new VFXExpression[] { parent };
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
            return ToFloatN(result);
        }

        sealed public override string GetOperationCodeContent()
        {
            return GetUnaryOperationCode(ParentsCodeName[0]);
        }

        abstract protected float ProcessUnaryOperation(float input);

        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionBinaryFloatOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOp operation)
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
            m_AdditionnalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Parents = new VFXExpression[] { parentLeft, parentRight };
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

            return ToFloatN(result);
        }

        sealed public override string GetOperationCodeContent()
        {
            return GetBinaryOperationCode(ParentsCodeName[0], ParentsCodeName[1]);
        }

        protected abstract float ProcessBinaryOperation(float left, float right);
        protected abstract string GetBinaryOperationCode(string a, string b);
    }

    abstract class VFXExpressionTernaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionTernaryFloatOperation(VFXExpression a, VFXExpression b, VFXExpression c, VFXExpressionOp operation)
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
            m_AdditionnalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Parents = new VFXExpression[] { a, b, c };
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

            return ToFloatN(result);
        }

        sealed public override string GetOperationCodeContent()
        {
            return GetTernaryOperationCode(ParentsCodeName[0], ParentsCodeName[1], ParentsCodeName[2]);
        }

        protected abstract float ProcessTernaryOperation(float a, float b, float c);
        protected abstract string GetTernaryOperationCode(string a, string b, string c);
    }
}
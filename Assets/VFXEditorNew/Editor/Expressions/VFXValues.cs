using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXValue : VFXExpression
    {
        protected VFXValue()
        {
            m_Flags |= Flags.Value | Flags.ValidOnGPU | Flags.ValidOnCPU;
        }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXValueOp; } }

        public override VFXExpression Reduce(VFXExpressionContext context)
        {
            return this;
        }
    }

    abstract class VFXValue<T> : VFXValue
    {
        public VFXValue(T content = default(T))
        {
            m_Content = content;
        }

        protected T m_Content;
        public T Content
        {
            get { return m_Content; }
        }

        //TODO : Hash & Equal behavior
        //Hack (à changer)
        private static VFXValueType GetValueType(Type type)
        {
            if (type == typeof(float))
            {
                return VFXValueType.kFloat;
            }
            else if(type == typeof(Vector2))
            {
                return VFXValueType.kFloat2;
            }
            else if (type == typeof(Vector3))
            {
                return VFXValueType.kFloat3;
            }
            else if (type == typeof(Vector4))
            {
                return VFXValueType.kFloat4;
            }

            return VFXValueType.kNone;
        }

        static private readonly VFXValueType s_ValueType = GetValueType(typeof(T));
        sealed public override VFXValueType ValueType
        {
            get
            {
                return s_ValueType;
            }
        }
    }
    abstract class VFXValueConstable<T> : VFXValue<T>
    {
        protected VFXValueConstable(T value = default(T), bool isConst = true)
            : base(value)
        {
            if(isConst)
            {
                m_Flags |= Flags.Constant;
            }
        }
    }

    class VFXValueFloat : VFXValueConstable<float> { public VFXValueFloat(float value, bool isConst) : base(value, isConst) { } }
    class VFXValueFloat2 : VFXValueConstable<Vector2> { public VFXValueFloat2(Vector2 value, bool isConst) : base(value, isConst) { } }
    class VFXValueFloat3 : VFXValueConstable<Vector3> { public VFXValueFloat3(Vector3 value, bool isConst) : base(value, isConst) { } }
    class VFXValueFloat4 : VFXValueConstable<Vector4> { public VFXValueFloat4(Vector4 value, bool isConst) : base(value, isConst) { } }

    abstract class VFXExpressionFloatOperation : VFXExpression
    {
        protected VFXExpressionFloatOperation()
        {
            m_Flags = Flags.ValidOnCPU | Flags.ValidOnGPU;
            m_Parents = new VFXExpression[] { };
            m_AdditionnalParameters = new int[] { };
        }
        protected static bool IsFloatValueType(VFXValueType valueType)
        {
            return      valueType == VFXValueType.kFloat
                    ||  valueType == VFXValueType.kFloat2
                    ||  valueType == VFXValueType.kFloat3
                    ||  valueType == VFXValueType.kFloat4;
        }
        static private float[] ToFloatArray(float input) { return new float[] { input }; }
        static private float[] ToFloatArray(Vector2 input) { return new float[] { input.x, input.y }; }
        static private float[] ToFloatArray(Vector3 input) { return new float[] { input.x, input.y, input.z }; }
        static private float[] ToFloatArray(Vector4 input) { return new float[] { input.x, input.y, input.z, input.w }; }
        static protected float[] ToFloatArray(VFXExpression input)
        {
            switch(input.ValueType)
            {
                case VFXValueType.kFloat: return ToFloatArray((input as VFXValueFloat).Content);
                case VFXValueType.kFloat2: return ToFloatArray((input as VFXValueFloat2).Content);
                case VFXValueType.kFloat3: return ToFloatArray((input as VFXValueFloat3).Content);
                case VFXValueType.kFloat4: return ToFloatArray((input as VFXValueFloat4).Content);
            }
            return null;
        }

        static protected VFXExpression CastFloat(VFXExpression from, VFXValueType toValueType, float defautValue = 0.0f)
        {
            if (!IsFloatValueType(from.ValueType) || !IsFloatValueType(toValueType))
            {
                Debug.Log("Invalid CastFloat");
                return null;
            }

            if (from.ValueType == toValueType)
            {
                Debug.Log("Incoherent CastFloat");
                return null;
            }

            var fromValueType = from.ValueType;
            var fromValueTypeSize = TypeToSize(fromValueType);
            var toValueTypeSize = TypeToSize(toValueType);

            var inputComponent = new VFXExpression[fromValueTypeSize];
            var outputComponent = new VFXExpression[toValueTypeSize];

            if (inputComponent.Length == 1)
            {
                inputComponent[0] = from;
            }
            else
            {
                for (int iChannel = 0; iChannel < fromValueTypeSize; ++iChannel)
                {
                    inputComponent[iChannel] = new VFXExpressionExtractComponent(from, iChannel);
                }
            }

            for (int iChannel = 0; iChannel < toValueTypeSize; ++iChannel)
            {
                if (iChannel < fromValueTypeSize)
                {
                    outputComponent[iChannel] = inputComponent[iChannel];
                }
                else if (fromValueTypeSize == 1)
                {
                    //Manage same logic behavior for float => floatN in HLSL
                    outputComponent[iChannel] = inputComponent[0];
                }
                else
                {
                    outputComponent[iChannel] = new VFXValueFloat(defautValue, true);
                }
            }

            if (toValueTypeSize == 1)
            {
                return outputComponent[0];
            }

            var combine = new VFXExpressionCombine(outputComponent);
            return combine;
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

        sealed public override VFXExpression Reduce(VFXExpressionContext context)
        {
            if (context.Option == VFXExpressionContext.ReductionOption.ConstantFolding)
            {
                var parentExpression = m_Parents.Select(o => o.Reduce(context));
                if (parentExpression.All(o => o.Is(Flags.Value) && o.Is(Flags.Constant)))
                {
                    return ExecuteConstantOperation(parentExpression.ToArray());
                }
            }

            return this;
        }

        abstract protected VFXExpression ExecuteConstantOperation(VFXExpression[] reducedParents);

        protected VFXExpression[] m_Parents;
        protected int[] m_AdditionnalParameters;
        protected VFXExpressionOp m_Operation;
        protected VFXValueType m_ValueType;
    }


    class VFXExpressionCombine : VFXExpressionFloatOperation
    {
        public VFXExpressionCombine(VFXExpression[] parents)
        {
            if (parents.Length <= 1 || parents.Length > 4 || parents.Any(o => !IsFloatValueType(o.ValueType)))
            {
                Debug.LogError("Incorrect VFXExpressionCombine");
                return;
            }

            m_Parents = parents;
            switch(m_Parents.Length)
            {
                case 2:
                    m_Operation = VFXExpressionOp.kVFXCombine2fOp;
                    m_ValueType = VFXValueType.kFloat2;
                    break;
                case 3:
                    m_Operation = VFXExpressionOp.kVFXCombine3fOp;
                    m_ValueType = VFXValueType.kFloat3;
                    break;
                case 4:
                    m_Operation = VFXExpressionOp.kVFXCombine4fOp;
                    m_ValueType = VFXValueType.kFloat4;
                    break;
            }
        }

        sealed protected override VFXExpression ExecuteConstantOperation(VFXExpression[] reducedParents)
        {
            var constParentFloat = reducedParents.Cast<VFXValueFloat>().Select(o => o.Content).ToArray();
            if (constParentFloat.Length != m_Parents.Length)
            {
                Debug.LogError("Incorrect VFXExpressionCombine.ExecuteConstantOperation");
                return null;
            }

            switch (m_Parents.Length)
            {
                case 2: return new VFXValueFloat2(new Vector2(constParentFloat[0], constParentFloat[1]), true);
                case 3: return new VFXValueFloat3(new Vector3(constParentFloat[0], constParentFloat[1], constParentFloat[2]), true);
                case 4: return new VFXValueFloat4(new Vector4(constParentFloat[0], constParentFloat[1], constParentFloat[2], constParentFloat[3]), true);
            }
            return null;
        }

        sealed protected override string GetOperationCodeContent()
        {
            return string.Format("return {0}({1});", TypeToCode(ValueType), Parents.Select((o, i) => ParentsCodeName[i]).Aggregate((a, b) => string.Format("{0}, {1}", a, b)));
        }
    }

    class VFXExpressionExtractComponent : VFXExpressionFloatOperation
    {
        public VFXExpressionExtractComponent(VFXExpression parent, int iChannel)
        {
            if (parent.ValueType == VFXValueType.kFloat || !IsFloatValueType(parent.ValueType))
            {
                Debug.LogError("Incorrect VFXExpressionExtractComponent");
            }

            m_Parents = new VFXExpression[] { parent };
            m_Operation = VFXExpressionOp.kVFXExtractComponentOp;
            m_AdditionnalParameters = new int[] { TypeToSize(parent.ValueType), iChannel };
            m_ValueType = VFXValueType.kFloat;
        }

        static private float GetChannel(Vector2 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        static private float GetChannel(Vector3 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
                case 2: return input.z;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        static private float GetChannel(Vector4 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
                case 2: return input.z;
                case 3: return input.w;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        sealed protected override VFXExpression ExecuteConstantOperation(VFXExpression[] reducedParents)
        {
            float readValue = 0.0f;
            var iChannel = m_AdditionnalParameters[1];
            var parent = reducedParents[0];
            switch(reducedParents[0].ValueType)
            {
                case VFXValueType.kFloat : readValue = (parent as VFXValueFloat).Content; break;
                case VFXValueType.kFloat2: readValue = GetChannel((parent as VFXValueFloat2).Content, iChannel); break;
                case VFXValueType.kFloat3: readValue = GetChannel((parent as VFXValueFloat3).Content, iChannel); break;
                case VFXValueType.kFloat4: readValue = GetChannel((parent as VFXValueFloat4).Content, iChannel); break;
            }
            return new VFXValueFloat(readValue, true);
        }

        sealed protected override string GetOperationCodeContent()
        {
            return string.Format("return {0}[{1}];", ParentsCodeName[0], AdditionnalParameters[1]);
        }
    }

    abstract class VFXExpressionUnaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionUnaryFloatOperation(VFXExpression parent, VFXExpressionOp operation)
        {
            if (!IsFloatValueType(parent.ValueType))
            {
                Debug.LogError("Incorrect VFXExpressionUnaryFloatOperation");
                return;
            }

            m_ValueType = parent.ValueType;
            m_AdditionnalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Parents = new VFXExpression[] { parent };
            m_Operation = operation;
        }

        sealed protected override VFXExpression ExecuteConstantOperation(VFXExpression[] reducedParents)
        {
            var source = ToFloatArray(reducedParents[0]);
            var result = new float[source.Length];
            for (int iChannel = 0; iChannel < source.Length; ++iChannel)
            {
                result[iChannel] = ProcessUnaryOperation(source[iChannel]);
            }
            return ToFloatN(result);
        }

        sealed protected override string GetOperationCodeContent()
        {
            return GetUnaryOperationCode(ParentsCodeName[0]);
        }

        abstract protected float ProcessUnaryOperation(float input);

        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionBinaryFloatOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOp operation, float identityValue = 0.0f)
        {
            if (!IsFloatValueType(parentLeft.ValueType) || !IsFloatValueType(parentRight.ValueType))
            {
                Debug.LogError("Incorrect VFXExpressionBinaryFloatOperation");
                return;
            }

            if (parentRight.ValueType != parentLeft.ValueType)
            {
                if (TypeToSize(parentRight.ValueType) > TypeToSize(parentLeft.ValueType))
                {
                    parentLeft = CastFloat(parentLeft, parentRight.ValueType, identityValue);
                }
                else
                {
                    parentRight = CastFloat(parentRight, parentLeft.ValueType, identityValue);
                }
            }
            m_ValueType = parentLeft.ValueType;
            m_AdditionnalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Parents = new VFXExpression[] { parentLeft, parentRight };
            m_Operation = operation;
        }

        sealed protected override VFXExpression ExecuteConstantOperation(VFXExpression[] reducedParents)
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

        sealed protected override string GetOperationCodeContent()
        {
            return GetBinaryOperationCode(ParentsCodeName[0], ParentsCodeName[1]);
        }

        protected abstract float ProcessBinaryOperation(float left, float right);
        protected abstract string GetBinaryOperationCode(string a, string b);
    }

    abstract class VFXExpressionTernaryFloatOperation : VFXExpressionFloatOperation
    {
        protected VFXExpressionTernaryFloatOperation(VFXExpression a, VFXExpression b, VFXExpression c, VFXExpressionOp operation, float identityValue = 0.0f)
        {
            if (    !IsFloatValueType(a.ValueType) 
                ||  !IsFloatValueType(b.ValueType)
                ||  !IsFloatValueType(c.ValueType))
            {
                Debug.LogError("Incorrect VFXExpressionTernaryFloatOperation");
                return;
            }

            if (a.ValueType != b.ValueType || b.ValueType != c.ValueType)
            {
                var maxValueType = a.ValueType;
                if (TypeToSize(maxValueType) < TypeToSize(b.ValueType))
                {
                    maxValueType = b.ValueType;
                }
                if (TypeToSize(maxValueType) < TypeToSize(c.ValueType))
                {
                    maxValueType = c.ValueType;
                }

                if (a.ValueType != maxValueType)
                {
                    a = CastFloat(a, maxValueType, identityValue);
                }

                if (b.ValueType != maxValueType)
                {
                    b = CastFloat(b, maxValueType, identityValue);
                }

                if (c.ValueType != maxValueType)
                {
                    c = CastFloat(c, maxValueType, identityValue);
                }
            }

            m_ValueType = a.ValueType;
            m_AdditionnalParameters = new int[] { TypeToSize(m_ValueType) };
            m_Parents = new VFXExpression[] { a, b, c };
            m_Operation = operation;
        }
        sealed protected override VFXExpression ExecuteConstantOperation(VFXExpression[] reducedParents)
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
                result[iChannel] = ProcessTernaryOperation(source_a[iChannel], source_a[iChannel], source_c[iChannel]);
            }

            return ToFloatN(result);
        }

        sealed protected override string GetOperationCodeContent()
        {
            return GetTernaryOperationCode(ParentsCodeName[0], ParentsCodeName[1], ParentsCodeName[2]);
        }

        protected abstract float ProcessTernaryOperation(float a, float b, float c);
        protected abstract string GetTernaryOperationCode(string a, string b, string c);
    }

    class VFXExpressionSin : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionSin(VFXExpression parent) : base (parent, VFXExpressionOp.kVFXSinOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("return sin({0});", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Sin(input);
        }
    }

    class VFXExpressionAdd : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionAdd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXAddOp, 0.0f)
        {
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("return {0} + {1};", left, right);
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left + right;
        }
    }

    class VFXExpressionMul : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionMul(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXMulOp, 1.0f)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left * right;
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("return {0} * {1};", left, right);
        }
    }

    class VFXExpressionSubtract : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionSubtract(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXSubtractOp, 0.0f)
        {
        }
        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left - right;
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("return {0} - {1};", left, right);
        }
    }

    class VFXExpressionLerp : VFXExpressionTernaryFloatOperation
    {
        public VFXExpressionLerp(VFXExpression x, VFXExpression y, VFXExpression s) : base(x, y, s, VFXExpressionOp.kVFXLerpOp)
        {
        }

        sealed protected override float ProcessTernaryOperation(float x, float y, float s)
        {
            return Mathf.Lerp(x, y, s);
        }

        sealed protected override string GetTernaryOperationCode(string x, string y, string s)
        {
            return string.Format("return lerp({0}, {1}, {2});", x, y, s);
        }
    }
}
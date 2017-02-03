using System;
using UnityEngine;
namespace UnityEditor.VFX
{
    class VFXExpressionExtractComponent : VFXExpressionFloatOperation
    {
        public VFXExpressionExtractComponent() : this(VFXValueFloat4.Default, 0) { }

        public VFXExpressionExtractComponent(VFXExpression parent, int iChannel)
        {
            if (parent.ValueType == VFXValueType.kFloat || !IsFloatValueType(parent.ValueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionExtractComponent");
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

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            float readValue = 0.0f;
            var iChannel = m_AdditionnalParameters[1];
            var parent = reducedParents[0];
            switch (reducedParents[0].ValueType)
            {
                case VFXValueType.kFloat: readValue = parent.GetContent<float>(); break;
                case VFXValueType.kFloat2: readValue = GetChannel(parent.GetContent<Vector2>(), iChannel); break;
                case VFXValueType.kFloat3: readValue = GetChannel(parent.GetContent<Vector3>(), iChannel); break;
                case VFXValueType.kFloat4: readValue = GetChannel(parent.GetContent<Vector4>(), iChannel); break;
            }
            return new VFXValueFloat(readValue, true);
        }

        sealed protected override string GetOperationCodeContent()
        {
            return string.Format("return {0}[{1}];", ParentsCodeName[0], AdditionnalParameters[1]);
        }
    }
}



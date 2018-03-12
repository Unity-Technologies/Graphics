using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;


namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorMultiplyNew : VFXOperatorFloatUnifiedWithVariadicOutputNew
    {
        [Serializable]
        public struct OperandInfo
        {
            public string name;
            public VFXValueType type;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        OperandInfo[] m_Operands;


        public void AddOperand()
        {
            int oldCount = m_Operands.Length;

            OperandInfo[] infos = new OperandInfo[oldCount + 1];

            Array.Copy(m_Operands, infos, oldCount);

            infos[oldCount] = GetDefaultInfo(oldCount);
            m_Operands = infos;

            Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

        public void RemoveOperand(int index)
        {
            int oldCount = m_Operands.Length;

            OperandInfo[] infos = new OperandInfo[oldCount - 1];

            Array.Copy(m_Operands, infos, index);
            Array.Copy(m_Operands, index + 1, infos, index, oldCount - index - 1);
            m_Operands = infos;

            Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

        public int operandCount
        {
            get { return m_Operands != null ? m_Operands.Length : 0; }
        }

        public string GetOperandName(int index)
        {
            return m_Operands[index].name;
        }

        public void SetOperandName(int index, string name)
        {
            m_Operands[index].name = name;

            Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

        public VFXValueType GetOperandType(int index)
        {
            return m_Operands[index].type;
        }

        public void SetOperandType(int index, VFXValueType type)
        {
            m_Operands[index].type = type;

            VFXValueType vectorType = VFXValueType.None;

            switch (type)
            {
                case VFXValueType.Float2:
                case VFXValueType.Float3:
                case VFXValueType.Float4:
                    vectorType = type;
                    break;
                default:
                    break;
            }

            if (vectorType != VFXValueType.None)
            {
                for (int i = 0; i < m_Operands.Length; ++i)
                {
                    if (i != index)
                    {
                        switch (m_Operands[i].type)
                        {
                            case VFXValueType.Float2:
                            case VFXValueType.Float3:
                            case VFXValueType.Float4:
                                m_Operands[i].type = vectorType;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_Operands != null)
            {
                VFXValueType vectorType = VFXValueType.None;
                for (int i = 0; i < m_Operands.Length; ++i)
                {
                    if (string.IsNullOrEmpty(m_Operands[i].name))
                    {
                        m_Operands[i].name = GetDefaultName(i);
                    }

                    switch (m_Operands[i].type)
                    {
                        case VFXValueType.Float:
                            break;
                        case VFXValueType.Float2:
                        case VFXValueType.Float3:
                        case VFXValueType.Float4:
                        {
                            if (vectorType != VFXValueType.None)
                            {
                                if (vectorType != m_Operands[i].type)
                                {
                                    m_Operands[i].type = vectorType;
                                }
                            }
                            else
                            {
                                vectorType = m_Operands[i].type;
                            }
                        }
                        break;
                        default:
                            m_Operands[i].type = VFXValueType.Float;
                            break;
                    }
                }
            }
        }

        public void OperandMoved(int movedIndex, int targetIndex)
        {
            if (movedIndex == targetIndex) return;

            OperandInfo[] newOperands = new OperandInfo[m_Operands.Length];

            if (movedIndex < targetIndex)
            {
                Array.Copy(m_Operands, newOperands, movedIndex);
                Array.Copy(m_Operands, movedIndex + 1, newOperands, movedIndex, targetIndex - movedIndex);
                newOperands[targetIndex] = m_Operands[movedIndex];
                Array.Copy(m_Operands, targetIndex + 1, newOperands, targetIndex + 1, m_Operands.Length - targetIndex - 1);
            }
            else
            {
                Array.Copy(m_Operands, newOperands, targetIndex);
                newOperands[targetIndex] = m_Operands[movedIndex];
                Array.Copy(m_Operands, targetIndex, newOperands, targetIndex + 1, movedIndex - targetIndex);
                Array.Copy(m_Operands, movedIndex + 1, newOperands, movedIndex + 1, m_Operands.Length - movedIndex - 1);
            }

            m_Operands = newOperands;

            //Move the slots ahead of time sot that the SyncSlot does not result in links lost.
            MoveSlots(VFXSlot.Direction.kInput, movedIndex, targetIndex);

            Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

        override public string name { get { return "MultiplyNew"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] * inputExpression[1] };
        }

        string GetDefaultName(int index)
        {
            return string.Format("operand{0}", index);
        }

        public OperandInfo GetDefaultInfo(int index)
        {
            return new OperandInfo() {name = GetDefaultName(index), type = VFXValueType.Float};
        }

        VFXValueType[] m_ValidTypes = new VFXValueType[]
        {
            VFXValueType.Float,
            VFXValueType.Float2,
            VFXValueType.Float3,
            VFXValueType.Float4,
        };

        public IEnumerable<VFXValueType> validTypes
        {
            get { return m_ValidTypes; }
        }


        object GetDefaultValueForType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float:
                    return 1.0f;
                case VFXValueType.Float2:
                    return Vector2.one;
                case VFXValueType.Float3:
                    return Vector3.one;
                case VFXValueType.Float4:
                    return Vector4.one;
                case VFXValueType.Int32:
                    return 1;
                case VFXValueType.Uint32:
                    return (uint)1;
            }
            return null;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_Operands == null)
                {
                    m_Operands = new OperandInfo[2];
                    for (int i = 0; i < m_Operands.Length; ++i)
                    {
                        m_Operands[i].name = GetDefaultName(i);
                        m_Operands[i].type = VFXValueType.Float;
                    }
                }

                {
                    for (int i = 0; i < m_Operands.Length; ++i)
                    {
                        if (string.IsNullOrEmpty(m_Operands[i].name))
                        {
                            m_Operands[i].name = GetDefaultName(i);
                        }
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(m_Operands[i].type), m_Operands[i].name), GetDefaultValueForType(m_Operands[i].type));
                    }
                }
            }
        }

        public override void UpdateOutputExpressions()
        {
            var inputExpression = GetInputExpressions();
            //Process aggregate two by two element until result
            var outputExpression = new Stack<VFXExpression>(inputExpression.Reverse());
            while (outputExpression.Count > 1)
            {
                var a = outputExpression.Pop();
                var b = outputExpression.Pop();
                var compose = BuildExpression(new[] { a, b })[0];
                outputExpression.Push(compose);
            }
            SetOutputExpressions(outputExpression.ToArray());
        }
    }
}

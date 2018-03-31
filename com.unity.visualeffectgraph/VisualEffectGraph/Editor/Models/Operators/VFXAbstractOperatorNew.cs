using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXOperatorDynamicOperand : VFXOperator
    {
        public sealed override void OnEnable()
        {
            /*TODOPAUL Should not be sealed at this stage */
            base.OnEnable();
        }

        public abstract IEnumerable<VFXValueType> validTypes { get; }

        protected abstract VFXValueType defaultValueType { get; }

        protected abstract object GetDefaultValueForType(VFXValueType type);

        public abstract void Initialize(); //This function has to be called after a fresh new (!= Serialization)
    }

    abstract class VFXOperatorNumericNew : VFXOperatorDynamicOperand
    {
        //output depends on all input type applying float > uint > int > bool (http://c0x.coding-guidelines.com/6.3.1.8.html)
        private static readonly Type[] kExpectedTypeOrdering = new[]
        {
            typeof(Vector4),
            typeof(Vector3),
            typeof(Vector2),
            typeof(float),
            typeof(uint),
            typeof(int),
        };

        private static readonly VFXValueType[] kValidType = kExpectedTypeOrdering.Select(o => VFXExpression.GetVFXValueTypeFromType(o)).ToArray();
        private static readonly VFXValueType[] kValidTypeWithoutInteger = kValidType.Except(new[] { VFXValueType.Uint32, VFXValueType.Int32 }).ToArray();


        protected sealed override VFXValueType defaultValueType
        {
            get
            {
                return VFXValueType.Float;
            }
        }

        protected virtual bool allowInteger { get { return true; } }

        public sealed override IEnumerable<VFXValueType> validTypes
        {
            get { return allowInteger ? kValidType : kValidTypeWithoutInteger; }
        }

        protected virtual Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            if (!inputTypes.Any())
                return VFXExpression.TypeToType(defaultValueType);

            var minIndex = inputTypes.Select(o => Array.IndexOf(kExpectedTypeOrdering, o)).Min();
            return kExpectedTypeOrdering[minIndex];
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                const string outputName = "o";
                var slotType = GetExpectedOutputTypeOfOperation(inputSlots.Select(o => o.property.type));
                if (slotType != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, outputName));
            }
        }
    }

    abstract class VXOperatorNumericUniformNew : VFXOperatorNumericNew
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        VFXValueType m_Type;
    }

    abstract class VFXOperatorNumericCascadedUnifiedNew : VFXOperatorNumericNew
    {
        [Serializable]
        public struct Operand
        {
            public string name;
            public VFXValueType type;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        Operand[] m_Operands;

        public sealed override void Initialize()
        {
            m_Operands = new Operand[] {};
            AddOperand();
            AddOperand();
        }

        protected string GetDefaultName(int index)
        {
            return VFXCodeGeneratorHelper.GeneratePrefix((uint)index);
        }

        protected Operand GetDefaultOperand(int index)
        {
            return new Operand() { name = GetDefaultName(index), type = defaultValueType };
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_Operands != null)
                    foreach (var operand in m_Operands)
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(operand.type), operand.name), GetDefaultValueForType(operand.type));
            }
        }

        public void AddOperand()
        {
            int oldCount = m_Operands.Length;
            var infos = new Operand[oldCount + 1];

            Array.Copy(m_Operands, infos, oldCount);
            infos[oldCount] = GetDefaultOperand(oldCount);
            m_Operands = infos;

            Invalidate(InvalidationCause.kSettingChanged);
        }

        public void RemoveOperand(int index)
        {
            int oldCount = m_Operands.Length;
            Operand[] infos = new Operand[oldCount - 1];

            Array.Copy(m_Operands, infos, index);
            Array.Copy(m_Operands, index + 1, infos, index, oldCount - index - 1);
            m_Operands = infos;

            Invalidate(InvalidationCause.kSettingChanged);
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
            Invalidate(InvalidationCause.kSettingChanged);
        }

        public VFXValueType GetOperandType(int index)
        {
            return m_Operands[index].type;
        }

        public void SetOperandType(int index, VFXValueType type)
        {
            m_Operands[index].type = type;
            Invalidate(InvalidationCause.kSettingChanged);
        }

        public void OperandMoved(int movedIndex, int targetIndex)
        {
            if (movedIndex == targetIndex) return;

            var newOperands = new Operand[m_Operands.Length];

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

            Invalidate(InvalidationCause.kSettingChanged);
        }

        public sealed override void UpdateOutputExpressions()
        {
            var inputExpression = GetInputExpressions();

            /* Temporary int/uint casting (another branch to handle these operation is in review */

            //Unify behavior
            inputExpression = VFXOperatorUtility.UpcastAllFloatN(inputExpression, defaultValueFloat);

            //Process aggregate two by two element until result
            var outputExpression = new Stack<VFXExpression>(inputExpression.Reverse());
            while (outputExpression.Count > 1)
            {
                var a = outputExpression.Pop();
                var b = outputExpression.Pop();
                var compose = BuildExpression(new[] { a, b })[0];
                outputExpression.Push(compose);
            }

            /* Temporary int/uint casting (another branch to handle these operation is in review */
            //TODOPAUL

            SetOutputExpressions(outputExpression.ToArray());
        }

        protected sealed override object GetDefaultValueForType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float:
                    return defaultValueFloat;
                case VFXValueType.Float2:
                    return Vector2.one * defaultValueFloat;
                case VFXValueType.Float3:
                    return Vector3.one * defaultValueFloat;
                case VFXValueType.Float4:
                    return Vector4.one * defaultValueFloat;
                case VFXValueType.Int32:
                    return defaultValueInt;
                case VFXValueType.Uint32:
                    return (uint)defaultValueUint;
            }
            return null;
        }

        protected abstract double defaultValueDouble { get; }
        protected virtual float defaultValueFloat { get { return (float)defaultValueDouble; } }
        protected virtual int defaultValueInt { get { return (int)defaultValueDouble; } }
        protected virtual uint defaultValueUint { get { return (uint)defaultValueDouble; } }
    }
}

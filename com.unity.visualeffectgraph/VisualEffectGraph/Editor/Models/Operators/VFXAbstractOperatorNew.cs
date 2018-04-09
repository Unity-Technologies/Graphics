using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXOperatorDynamicOperand : VFXOperator
    {
        /* Global rules */
        //output depends on all input type applying float > uint > int > bool (http://c0x.coding-guidelines.com/6.3.1.8.html)
        public static readonly Type[] kExpectedTypeOrdering = new[]
        {
            typeof(Vector4),
            typeof(Vector3),
            typeof(Vector2),
            typeof(float),
            typeof(uint),
            typeof(int),
        };

        public sealed override void OnEnable()
        {
            base.OnEnable();
        }

        public abstract IEnumerable<Type> validTypes { get; }

        protected abstract Type defaultValueType { get; }

        protected virtual object GetDefaultValueForType(Type type)
        {
            return VFXTypeExtension.GetDefaultField(type);
        }
    }

    abstract class VFXOperatorNumericNew : VFXOperatorDynamicOperand
    {
        private static readonly Type[] kValidTypeWithoutInteger = kExpectedTypeOrdering.Except(new[] { typeof(uint), typeof(int) }).ToArray();

        protected sealed override Type defaultValueType
        {
            get
            {
                return typeof(float);
            }
        }

        protected virtual bool allowInteger { get { return true; } }

        public sealed override IEnumerable<Type> validTypes
        {
            get { return allowInteger ? kExpectedTypeOrdering : kValidTypeWithoutInteger; }
        }

        protected static IEnumerable<VFXExpression> Temp_CastToFloat(IEnumerable<VFXExpression> expressions)
        {
            return expressions.Select(o =>
                {
                    if (o.valueType == VFXValueType.Int32)
                        return new VFXExpressionCastIntToFloat(o) as VFXExpression;
                    if (o.valueType == VFXValueType.Uint32)
                        return new VFXExpressionCastUintToFloat(o) as VFXExpression;
                    return o;
                });
        }

        protected static IEnumerable<VFXExpression> Temp_CastToTarget(IEnumerable<VFXExpression> expression, IEnumerable<VFXPropertyWithValue> targetSlot)
        {
            if (expression.Count() != targetSlot.Count())
                throw new NotImplementedException();

            var itExpression = expression.GetEnumerator();
            var itSlot = targetSlot.GetEnumerator();
            while (itExpression.MoveNext() && itSlot.MoveNext())
            {
                if (itSlot.Current.property.type == typeof(int))
                    yield return new VFXExpressionCastFloatToInt(itExpression.Current);
                else if (itSlot.Current.property.type == typeof(uint))
                    yield return new VFXExpressionCastFloatToUint(itExpression.Current);
                else
                    yield return itExpression.Current;
            }
        }

        protected virtual Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            if (!inputTypes.Any())
                return defaultValueType;

            var minIndex = inputTypes.Select(o => Array.IndexOf(kExpectedTypeOrdering, o)).Min();
            return kExpectedTypeOrdering[minIndex];
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (base.outputProperties.Any())
                {
                    //Behavior when "OutputProperties" as been declared (length, dot, square length...)
                    foreach (var outputProperty in base.outputProperties)
                        yield return outputProperty;
                }
                else
                {
                    //Most common behavior : output of an operation depend of input type
                    const string outputName = "o";
                    var slotType = GetExpectedOutputTypeOfOperation(inputSlots.Select(o => o.property.type));
                    if (slotType != null)
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, outputName));
                }
            }
        }

        protected sealed override object GetDefaultValueForType(Type vtype)
        {
            var type = VFXExpression.GetVFXValueTypeFromType(vtype);
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
                    return defaultValueUint;
            }
            return null;
        }

        protected abstract double defaultValueDouble { get; }
        protected virtual float defaultValueFloat { get { return (float)defaultValueDouble; } }
        protected virtual int defaultValueInt { get { return (int)defaultValueDouble; } }
        protected virtual uint defaultValueUint { get { return (uint)defaultValueDouble; } }
    }

    interface IVFXOperatorUniform
    {
        Type GetOperandType();
        void SetOperandType(Type type);
    }

    abstract class VFXOperatorNumericUniformNew : VFXOperatorNumericNew, IVFXOperatorUniform
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        SerializableType m_Type;

        protected override double defaultValueDouble //Most common case for this kind of operator (still overridable)
        {
            get
            {
                return 0.0;
            }
        }

        public Type GetOperandType()
        {
            return m_Type;
        }

        public void SetOperandType(Type type)
        {
            if (!validTypes.Contains(type))
                throw new InvalidOperationException();

            m_Type = type;
            Invalidate(InvalidationCause.kSettingChanged);
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseInputProperties = base.inputProperties;
                if (m_Type == null) // Lazy init at this stage is suitable because inputProperties access is done with SyncSlot
                {
                    var typeEnumeration = baseInputProperties.Select(o => o.property.type);
                    if (typeEnumeration.Any(o => !validTypes.Contains(o)))
                        throw new InvalidOperationException("Forbidden type");

                    if (typeEnumeration.Distinct().Count() != 1)
                        throw new InvalidOperationException("Uniform type expected");

                    m_Type = typeEnumeration.First();
                }

                foreach (var property in baseInputProperties)
                {
                    if (property.property.type == (Type)m_Type)
                        yield return property;
                    else
                        yield return new VFXPropertyWithValue(new VFXProperty((Type)m_Type, property.property.name), GetDefaultValueForType(m_Type));
                }
            }
        }

        public sealed override void UpdateOutputExpressions()
        {
            var inputExpression = GetInputExpressions();

            /* Temporary : int/uint casting (another branch to handle these operation is in review */
            inputExpression = Temp_CastToFloat(inputExpression);

            var outputExpression = BuildExpression(inputExpression.ToArray());

            /* Temporary int/uint casting (another branch to handle these operation is in review */
            var outputExpressionFixed = Temp_CastToTarget(outputExpression, outputProperties);
            SetOutputExpressions(outputExpressionFixed.ToArray());
        }
    }

    interface IVFXOperatorNumericUnifiedNew
    {
        int operandCount {get; }
        Type GetOperandType(int index);
        void SetOperandType(int index, Type type);
    }

    abstract class VFXOperatorNumericUnifiedNew : VFXOperatorNumericNew, IVFXOperatorNumericUnifiedNew
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        SerializableType[] m_Type;

        protected override double defaultValueDouble //Most common case for this kind of operator (still overridable)
        {
            get
            {
                return 0.0;
            }
        }
        public int operandCount
        {
            get {return m_Type.Length; }
        }
        public Type GetOperandType(int index)
        {
            return m_Type[index];
        }

        public void SetOperandType(int index, Type type)
        {
            if (!validTypes.Contains(type))
                throw new InvalidOperationException();

            m_Type[index] = type;
            Invalidate(InvalidationCause.kSettingChanged);
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseType = base.inputProperties;
                if (m_Type == null) // Lazy init at this stage is suitable because inputProperties access is done with SyncSlot
                {
                    var typeArray = baseType.Select(o => o.property.type).ToArray();
                    if (typeArray.Any(o => !validTypes.Contains(o)))
                        throw new InvalidOperationException("Forbidden type");

                    m_Type = typeArray.Select(o => new SerializableType(o)).ToArray();
                }

                if (baseType.Count() != m_Type.Length)
                    throw new InvalidOperationException();

                var itSlot = baseType.GetEnumerator();
                var itType = m_Type.Cast<SerializableType>().GetEnumerator();
                while (itSlot.MoveNext() && itType.MoveNext())
                {
                    if (itSlot.Current.property.type == (Type)itType.Current)
                        yield return itSlot.Current;
                    else
                        yield return new VFXPropertyWithValue(new VFXProperty((Type)itType.Current, itSlot.Current.property.name), GetDefaultValueForType(itType.Current));
                }
            }
        }

        public sealed override void UpdateOutputExpressions()
        {
            var inputExpression = GetInputExpressions();

            /* Temporary : int/uint casting (another branch to handle these operation is in review */
            inputExpression = Temp_CastToFloat(inputExpression);
            //Unify behavior (actually, also temporary, since it should handle int to float conversion in some cases)
            inputExpression = VFXOperatorUtility.UpcastAllFloatN(inputExpression, defaultValueFloat);

            var outputExpression = BuildExpression(inputExpression.ToArray());

            /* Temporary int/uint casting (another branch to handle these operation is in review */
            var outputExpressionFixed = Temp_CastToTarget(outputExpression, outputProperties);
            SetOutputExpressions(outputExpressionFixed.ToArray());
        }
    }

    abstract class VFXOperatorNumericCascadedUnifiedNew : VFXOperatorNumericNew, IVFXOperatorNumericUnifiedNew
    {
        [Serializable]
        public struct Operand
        {
            public string name;
            public SerializableType type;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        Operand[] m_Operands;

        protected string GetDefaultName(int index)
        {
            return VFXCodeGeneratorHelper.GeneratePrefix((uint)index);
        }

        protected Operand GetDefaultOperand(int index, Type type = null)
        {
            return new Operand() { name = GetDefaultName(index), type = type != null ? type : defaultValueType };
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_Operands == null) //Lazy init at this stage is suitable because inputProperties access is done with SyncSlot
                {
                    m_Operands = new Operand[] { GetDefaultOperand(0), GetDefaultOperand(1) };
                }

                foreach (var operand in m_Operands)
                    yield return new VFXPropertyWithValue(new VFXProperty(operand.type, operand.name), GetDefaultValueForType(operand.type));
            }
        }

        public void AddOperand(Type type = null)
        {
            int oldCount = m_Operands.Length;
            var infos = new Operand[oldCount + 1];

            Array.Copy(m_Operands, infos, oldCount);
            infos[oldCount] = GetDefaultOperand(oldCount, type);
            m_Operands = infos;

            Invalidate(InvalidationCause.kSettingChanged);
        }

        public void RemoveOperand(int index)
        {
            OperandMoved(index, operandCount - 1);
            RemoveOperand();
        }

        public void RemoveOperand()
        {
            if (m_Operands.Length == 0)
                return;

            int oldCount = m_Operands.Length;
            var infos = new Operand[oldCount - 1];

            Array.Copy(m_Operands, infos, oldCount - 1);
            m_Operands = infos;

            inputSlots.Last().UnlinkAll(true);
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

        public Type GetOperandType(int index)
        {
            return m_Operands[index].type;
        }

        public void SetOperandType(int index, Type type)
        {
            if (!validTypes.Contains(type))
                throw new InvalidOperationException();

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

        public override void UpdateOutputExpressions()
        {
            var inputExpression = GetInputExpressions();

            /* Temporary : int/uint casting (another branch to handle these operation is in review */
            inputExpression = Temp_CastToFloat(inputExpression);
            //Unify behavior (actually, also temporary, since it should handle int to float conversion in some cases)
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
            var outputExpressionFixed = Temp_CastToTarget(outputExpression, outputProperties);

            SetOutputExpressions(outputExpressionFixed.ToArray());
        }
    }
}

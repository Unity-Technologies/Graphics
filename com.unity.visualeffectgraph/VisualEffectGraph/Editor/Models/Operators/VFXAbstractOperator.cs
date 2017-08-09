using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    struct FloatN
    {
        public FloatN(float a) : this(new[] { a })
        {
        }
        public FloatN(Vector2 a) : this(new[] { a.x, a.y })
        {
        }
        public FloatN(Vector3 a) : this(new[] { a.x, a.y, a.z })
        {
        }
        public FloatN(Vector4 a) : this(new[] { a.x, a.y, a.z, a.w })
        {
        }
        public FloatN(float[] currentValues = null)
        {
            m_Components = currentValues;
        }

        public float this[int i]
        {
            get
            {
                if (realSize == 1)
                {
                    return m_Components[0];
                }
                return realSize > i ? m_Components[i] : 0.0f;
            }
            set
            {
                if (realSize > i)
                {
                    m_Components[i] = value;
                }
            }
        }

        public float x { get { return this[0]; } set { this[0] = value; } }
        public float y { get { return this[1]; } set { this[1] = value; } }
        public float z { get { return this[2]; } set { this[2] = value; } }
        public float w { get { return this[3]; } set { this[3] = value; } }
        public int realSize { get { return m_Components == null ? 0 : m_Components.Length; } }

        public static implicit operator FloatN(float value)
        {
            return new FloatN(new[] { value });
        }

        public static implicit operator FloatN(Vector2 value)
        {
            return new FloatN(new[] { value.x, value.y });
        }

        public static implicit operator FloatN(Vector3 value)
        {
            return new FloatN(new[] { value.x, value.y, value.z });
        }

        public static implicit operator FloatN(Vector4 value)
        {
            return new FloatN(new[] { value.x, value.y, value.z, value.w });
        }

        public static implicit operator float(FloatN value)
        {
            return value.x;
        }

        public static implicit operator Vector2(FloatN value)
        {
            return new Vector2(value.x, value.y);
        }

        public static implicit operator Vector3(FloatN value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        public static implicit operator Vector4(FloatN value)
        {
            return new Vector4(value.x, value.y, value.z, value.w);
        }

        public VFXValue ToVFXValue(VFXValue.Mode mode)
        {
            switch (realSize)
            {
                case 1: return new VFXValue<float>(this, mode);
                case 2: return new VFXValue<Vector2>(this, mode);
                case 3: return new VFXValue<Vector3>(this, mode);
                case 4: return new VFXValue<Vector4>(this, mode);
            }
            return null;
        }

        [SerializeField]
        private float[] m_Components;
    }

    abstract class VFXOperatorFloatUnified : VFXOperator
    {
        private float m_FallbackValue = 0.0f;

        protected VFXOperatorFloatUnified()
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (inputSlots.Any(s => s.property.type != typeof(FloatN)))
            {
                throw new Exception(string.Format("VFXOperatorFloatUnified except only FloatN as input : {0}", GetType()));
            }

            var propertyType = GetType().GetNestedType(GetInputPropertiesTypeName());
            if (propertyType != null)
            {
                var fields = propertyType.GetFields().Where(o => o.IsStatic && o.Name == "FallbackValue");
                var field = fields.FirstOrDefault(o => o.FieldType == typeof(float));
                if (field != null)
                {
                    m_FallbackValue = (float)field.GetValue(null);
                }
            }
        }

        //Convert automatically input expression with diverging floatN size to floatMax
        sealed override protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            var inputExpression = base.GetInputExpressions();
            return VFXOperatorUtility.UnifyFloatLevel(inputExpression, m_FallbackValue);
        }
    }

    abstract class VFXOperatorUnaryFloatOperation : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN input = new FloatN(new[] { 0.0f });
        }
    }

    abstract class VFXOperatorBinaryFloatCascadableOperation : VFXOperatorFloatUnified
    {
        sealed protected override void OnInputConnectionsChanged()
        {
            //Remove useless unplugged slot (ensuring there is at least 2 slots)
            var currentSlots = inputSlots.ToList();
            var uselessSlots = new Stack<VFXSlot>(currentSlots.Where((s, i) => i >= 2 && !s.HasLink()));
            foreach (var slot in uselessSlots)
            {
                currentSlots.Remove(slot);
            }

            if (currentSlots.All(s => s.HasLink()))
            {
                if (uselessSlots.Count == 0)
                {
                    AddSlot(VFXSlot.Create(new VFXProperty(typeof(FloatN), "Empty"), VFXSlot.Direction.kInput));
                }
                else
                {
                    uselessSlots.Pop();
                }
            }

            //Update deprecated Slot
            foreach (var slot in uselessSlots)
            {
                RemoveSlot(slot);
            }

            UpdateOutputs();
        }

        public override void UpdateOutputs()
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
            SetOuputSlotFromExpression(outputExpression);
        }
    }

    abstract class VFXOperatorBinaryFloatOperationOne : VFXOperatorBinaryFloatCascadableOperation
    {
        public class InputProperties
        {
            static public float FallbackValue = 1.0f;
            public FloatN right = FallbackValue;
            public FloatN left = FallbackValue;
        }
    }

    abstract class VFXOperatorBinaryFloatOperationZero : VFXOperatorBinaryFloatCascadableOperation
    {
        public class InputProperties
        {
            static public float FallbackValue = 0.0f;
            public FloatN right = FallbackValue;
            public FloatN left = FallbackValue;
        }
    }
}

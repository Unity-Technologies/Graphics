using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXOperatorDesc
    {
        [Flags]
        public enum Flags
        {
            None = 0,
            kBasicFloatOperation = 1 << 0,  //Automatically cast to biggest floatN format (sin(float3) => return float3)
            kCascadable = 1 << 1,           //allow implicit stacking (add, mul, substract, ...)

            kUnaryFloatOperator = kBasicFloatOperation,
            kBinaryFloatOperator = kBasicFloatOperation | kCascadable,
            kTernaryFloatOperator = kBasicFloatOperation,
        }

        public string name { get; private set; }

        public Flags m_Flags;
        public bool cascadable { get { return (m_Flags & Flags.kCascadable) != 0; } }

        protected VFXOperatorDesc(string _name, Flags flags)
        {
            name = _name;
            m_Flags = flags;
        }

        abstract public VFXExpression[] BuildExpression(VFXExpression[] inputExpression);
        public System.Type GetPropertiesType()
        {
            return GetType().GetNestedType("Properties");
        }
    }

    abstract class VFXOperatorUnaryloatOperation : VFXOperatorDesc
    {
        public class Properties
        {
            public float input = 0.0f;
        }

        protected VFXOperatorUnaryloatOperation(string name) : base(name, Flags.kUnaryFloatOperator)
        {
        }
    }

    abstract class VFXOperatorBinaryFloatOperation : VFXOperatorDesc
    {
        protected VFXOperatorBinaryFloatOperation(string name) : base(name, Flags.kBinaryFloatOperator)
        {
        }
    }

    abstract class VFXOperatorBinaryFloatOperationOne : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 1.0f;
            public float left = 1.0f;
        }
        protected VFXOperatorBinaryFloatOperationOne(string name) : base(name)
        { }
    }

    abstract class VFXOperatorBinaryFloatOperationZero : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 0.0f;
            public float left = 0.0f;
        }
        protected VFXOperatorBinaryFloatOperationZero(string name) : base(name)
        { }
    }

    [VFXInfo]
    class VFXOperatorSin : VFXOperatorUnaryloatOperation
    {
        public VFXOperatorSin() : base("Sin")
        {
        }

        public override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }


    [VFXInfo]
    class VFXOperatorAdd : VFXOperatorBinaryFloatOperationZero
    {
        public VFXOperatorAdd() : base("Add")
        {
        }

        public override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new [] { new VFXExpressionAdd(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSubstract : VFXOperatorBinaryFloatOperationZero
    {
        public VFXOperatorSubstract() : base("Substract")
        {
        }

        public override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSubtract(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorMul : VFXOperatorBinaryFloatOperationOne
    {
        public VFXOperatorMul() : base("Mul")
        {
        }

        public override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMul(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSampleCurve : VFXOperatorDesc
    {
        public class Properties
        {
            public float time = 0.0f;
            public AnimationCurve curve = null;
        }

        public override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleCurve(inputExpression[0], inputExpression[1]) };
        }

        public VFXOperatorSampleCurve() : base("SampleCurve", Flags.None)
        {
        }
    }
}
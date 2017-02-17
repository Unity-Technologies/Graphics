using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXOperatorUnaryFloatOperation : VFXOperator
    {
        public class Properties
        {
            public float input = 0.0f;
        }

        protected override ModeFlags Flags { get { return ModeFlags.kUnaryFloatOperator; } }
    }

    abstract class VFXOperatorBinaryFloatOperation : VFXOperator
    {
        protected override ModeFlags Flags { get { return ModeFlags.kBinaryFloatOperator; } }
    }

    abstract class VFXOperatorBinaryFloatOperationOne : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 1.0f;
            public float left = 1.0f;
        }
    }

    abstract class VFXOperatorBinaryFloatOperationZero : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 0.0f;
            public float left = 0.0f;
        }
    }

    [VFXInfo]
    class VFXOperatorSin : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sin"; }}

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorAdd : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Add"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAdd(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSubtract : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Subtract"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSubtract(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorMul : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Mul"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMul(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSampleCurve : VFXOperator
    {
        override public string name { get { return "SampleCurve"; } }

        protected override ModeFlags Flags { get { return ModeFlags.None; } }

        public class Properties
        {
            public float time = 0.0f;
            public AnimationCurve curve = new AnimationCurve();
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleCurve(inputExpression[0], inputExpression[1]) };
        }
    }
}
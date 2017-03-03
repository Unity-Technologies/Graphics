using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
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
    class VFXOperatorDivide : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Divide"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionDivide(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorFmod : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN right = new FloatN(new[] { 1.0f });
            public FloatN left = new FloatN(new[] { 1.0f });
        }

        override public string name { get { return "Fmod"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (inputExpression.Length != 2)
            {
                return new VFXExpression[] { };
            }

            var div = new VFXExpressionDivide(inputExpression[0], inputExpression[1]);
            return new[] { VFXOperatorUtility.Frac(div) };
        }
    }

    [VFXInfo]
    class VFXOperatorMin : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Min"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMin(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorMax : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Max"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMax(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorPow : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Pow"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionPow(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorDot : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            public FloatN right = Vector3.zero;
            public FloatN left = Vector3.zero;
        }

        override public string name { get { return "Dot"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (inputExpression.Length != 2)
            {
                return new VFXExpression[] { };
            }

            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorCross : VFXOperator
    {
        public class InputProperties
        {
            public Vector3 left = Vector3.right;
            public Vector3 right = Vector3.up;
        }

        override public string name { get { return "Cross"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (inputExpression.Length != 2)
            {
                return new VFXExpression[] { };
            }

            var lhs = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();
            var rhs = VFXOperatorUtility.ExtractComponents(inputExpression[1]).ToArray();
            if (lhs.Length != 3 || rhs.Length != 3)
            {
                return new VFXExpression[] { };
            }

            Func<VFXExpression, VFXExpression, VFXExpression, VFXExpression, VFXExpression> ab_Minus_cd = delegate (VFXExpression a, VFXExpression b, VFXExpression c, VFXExpression d)
            {
                var ab = new VFXExpressionMul(a, b);
                var cd = new VFXExpressionMul(c, d);
                return new VFXExpressionSubtract(ab, cd);
            };
            
            return new[] { new VFXExpressionCombine(new []
            {
                ab_Minus_cd(lhs[1], rhs[2], lhs[2], rhs[1]),
                ab_Minus_cd(lhs[2], rhs[0], lhs[0], rhs[2]),
                ab_Minus_cd(lhs[0], rhs[1], lhs[1], rhs[0]),
            })};
        }
    }
}
 
 
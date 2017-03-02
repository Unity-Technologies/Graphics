using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorAbs : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Abs"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorComponentMask : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "ComponentMask"; } }

        public class Settings
        {
            public string mask = "zyx";
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var currentSettings = settings as Settings;

            var mask = currentSettings.mask;
            var inputComponents = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();

            var componentStack = new Stack<VFXExpression>();
            for (int iComponent = 0; iComponent < mask.Length; iComponent++)
            {
                var iChannelIndex = -1;
                switch (mask[iComponent])
                {
                    case 'x': case 'r': iChannelIndex = 0; break;
                    case 'y': case 'g': iChannelIndex = 1; break;
                    case 'z': case 'b': iChannelIndex = 2; break;
                    case 'w': case 'a': iChannelIndex = 3; break;
                    default: throw new Exception("unexpected component name");
                }

                if (iChannelIndex < inputComponents.Length)
                {
                    componentStack.Push(inputComponents[iChannelIndex]);
                }
                else
                {
                    componentStack.Push(new VFXValueFloat(0.0f, true));
                }
            }

            VFXExpression finalExpression = null;
            if (componentStack.Count == 1)
            {
                finalExpression = componentStack.Pop();
            }
            else
            {
                finalExpression = new VFXExpressionCombine(componentStack.ToArray());
            }
            return new[] { finalExpression };
        }
    }

    [VFXInfo]
    class VFXOperatorCeil : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceil"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var one = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(inputExpression[0].ValueType)];
            var sum = new VFXExpressionAdd(inputExpression[0], one);
            return new[] { new VFXExpressionFloor(sum) };
        }
    }

    [VFXInfo]
    class VFXOperatorFloor : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Floor"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionFloor(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorRound : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Round"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var half = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(inputExpression[0].ValueType)];
            var sum = new VFXExpressionAdd(inputExpression[0], half);
            return new[] { new VFXExpressionFloor(sum) };
        }
    }

    [VFXInfo]
    class VFXOperatorFrac : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Frac"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Frac(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSaturate : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Saturate"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var size = VFXExpression.TypeToSize(inputExpression[0].ValueType);
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], VFXOperatorUtility.ZeroExpression[size], VFXOperatorUtility.OneExpression[size]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSin : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sin"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorCos : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Cos"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorOneMinus : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "OneMinus"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(input.ValueType)];
            return new[] { new VFXExpressionSubtract(one, input) };
        }
    }

    [VFXInfo]
    class VFXOperatorSqrt : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sqrt"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Sqrt(inputExpression[0])};
        }
    }

    [VFXInfo]
    class VFXOperatorLength : VFXOperatorFloatUnified
    {
        public class Properties
        {
            public FloatN input = Vector3.one;
        }

        override public string name { get { return "Length"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorNormalize : VFXOperatorFloatUnified
    {
        public class Properties
        {
            public FloatN input = Vector3.one;
        }

        override public string name { get { return "Normalize"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var invLength = new VFXExpressionDivide(VFXOperatorUtility.OneExpression[1], VFXOperatorUtility.Length(inputExpression[0]));
            var invLengthVector = VFXOperatorUtility.CastFloat(invLength, inputExpression[0].ValueType);
            return new[] { new VFXExpressionMul(inputExpression[0], invLengthVector) };
        }
    }

}
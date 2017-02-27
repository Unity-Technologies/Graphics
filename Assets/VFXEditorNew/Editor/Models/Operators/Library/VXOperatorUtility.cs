using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXOperatorUtility
    {
        public static readonly Dictionary<int, VFXExpression> OneExpression = new Dictionary<int, VFXExpression>
        {
            { 1, new VFXValueFloat(1.0f, true) },
            { 2, new VFXValueFloat2(Vector2.one, true) },
            { 3, new VFXValueFloat3(Vector3.one, true) },
            { 4, new VFXValueFloat4(Vector4.one, true) },
        };

        public static readonly Dictionary<int, VFXExpression> HalfExpression = new Dictionary<int, VFXExpression>
        {
            { 1, new VFXValueFloat(0.5f, true) },
            { 3, new VFXValueFloat3(Vector3.one*0.5f, true) },
            { 2, new VFXValueFloat2(Vector2.one*0.5f, true) },
            { 4, new VFXValueFloat4(Vector4.one*0.5f, true) },
        };

        public static readonly Dictionary<int, VFXExpression> ZeroExpression = new Dictionary<int, VFXExpression>
        {
            { 1, new VFXValueFloat(0.0f, true) },
            { 2, new VFXValueFloat2(Vector2.zero, true) },
            { 3, new VFXValueFloat3(Vector3.zero, true) },
            { 4, new VFXValueFloat4(Vector4.zero, true) },
        };

        static public VFXExpression Clamp(VFXExpression input, VFXExpression min, VFXExpression max)
        {
            //Max(Min(x, max), min))
            var maxExp = new VFXExpressionMax(input, min);
            return new VFXExpressionMin(maxExp, max);
        }

        static public VFXExpression Frac(VFXExpression input)
        {
            //x - floor(x)
            var floor = new VFXExpressionFloor(input);
            return new VFXExpressionSubtract(input, floor);
        }

        static public VFXExpression Sqrt(VFXExpression input)
        {
            //pow(x, 0.5f)
            return new VFXExpressionPow(input, HalfExpression[VFXExpression.TypeToSize(input.ValueType)]);
        }

        static public VFXExpression Dot(VFXExpression a, VFXExpression b)
        {
            //a.x*b.x + a.y*b.y + ...
            var size = VFXExpression.TypeToSize(a.ValueType);
            if (a.ValueType != b.ValueType)
            {
                throw new ArgumentException(string.Format("Invalid Dot type input : {0} and {1}", a.ValueType, b.ValueType));
            }

            var mul = new VFXExpressionMul(a, b);
            var sum = new Stack<VFXExpression>();
            for (int iChannel = 0; iChannel < size; ++iChannel)
            {
                sum.Push(new VFXExpressionExtractComponent(mul, iChannel));
            }

            while (sum.Count > 1)
            {
                var top = sum.Pop();
                var bottom = sum.Pop();
                sum.Push(new VFXExpressionAdd(top, bottom));
            }
            return sum.Pop();
        }

        static public VFXExpression Lerp(VFXExpression x, VFXExpression y, VFXExpression s)
        {
            //x + s(y - x)
            var yMinusx = new VFXExpressionSubtract(y, x);
            var sMul_yMinusx = new VFXExpressionMul(s, yMinusx);
            return new VFXExpressionAdd(x, sMul_yMinusx);
        }

        static public VFXExpression Length(VFXExpression v)
        {
            //sqrt(dot(v, v))
            var dot = Dot(v, v);
            return Sqrt(dot);
        }

        static public IEnumerable<VFXExpression> ExtractComponents(VFXExpression expression)
        {
            if (expression.ValueType == VFXValueType.kFloat)
            {
                return new[] { expression };
            }

            var components = new List<VFXExpression>();
            for (int i = 0; i < VFXExpression.TypeToSize(expression.ValueType); ++i)
            {
                components.Add(new VFXExpressionExtractComponent(expression, i));
            }
            return components;
        }

        static public IEnumerable<VFXExpression> UnifyFloatLevel(IEnumerable<VFXExpression> inputExpression, float defaultValue = 0.0f)
        {
            var maxValueType = inputExpression.Select(o => o.ValueType).OrderBy(t => VFXExpression.TypeToSize(t)).Last();
            var newVFXExpression = inputExpression.Select(o => CastFloat(o, maxValueType, defaultValue));
            return newVFXExpression.ToArray();
        }

        static public VFXExpression CastFloat(VFXExpression from, VFXValueType toValueType, float defautValue = 0.0f)
        {
            if (!VFXExpressionFloatOperation.IsFloatValueType(from.ValueType) || !VFXExpressionFloatOperation.IsFloatValueType(toValueType))
            {
                throw new ArgumentException(string.Format("Invalid CastFloat : {0} to {1}", from, toValueType));
            }

            if (from.ValueType == toValueType)
            {
                return from;
            }

            var fromValueType = from.ValueType;
            var fromValueTypeSize = VFXExpression.TypeToSize(fromValueType);
            var toValueTypeSize = VFXExpression.TypeToSize(toValueType);

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

    }
}
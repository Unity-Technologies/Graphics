using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXOperatorUtility
    {
        public static readonly Dictionary<int, VFXExpression> OneExpression = new Dictionary<int, VFXExpression>
        {
            { 1, VFXValue.Constant(1.0f) },
            { 2, VFXValue.Constant(Vector2.one) },
            { 3, VFXValue.Constant(Vector3.one) },
            { 4, VFXValue.Constant(Vector4.one) },
        };

        public static readonly Dictionary<int, VFXExpression> MinusOneExpression = new Dictionary<int, VFXExpression>
        {
            { 1, VFXValue.Constant(-1.0f) },
            { 2, VFXValue.Constant(-Vector2.one) },
            { 3, VFXValue.Constant(-Vector3.one) },
            { 4, VFXValue.Constant(-Vector4.one) },
        };

        public static readonly Dictionary<int, VFXExpression> HalfExpression = new Dictionary<int, VFXExpression>
        {
            { 1, VFXValue.Constant(0.5f) },
            { 3, VFXValue.Constant(Vector3.one * 0.5f) },
            { 2, VFXValue.Constant(Vector2.one * 0.5f) },
            { 4, VFXValue.Constant(Vector4.one * 0.5f) },
        };

        public static readonly Dictionary<int, VFXExpression> ZeroExpression = new Dictionary<int, VFXExpression>
        {
            { 1, VFXValue.Constant(0.0f) },
            { 2, VFXValue.Constant(Vector2.zero) },
            { 3, VFXValue.Constant(Vector3.zero) },
            { 4, VFXValue.Constant(Vector4.zero) },
        };

        // unified binary op
        static public VFXExpression UnifyOp(Func<VFXExpression, VFXExpression, VFXExpression> f, VFXExpression e0, VFXExpression e1)
        {
            var unifiedExp = VFXOperatorUtility.UnifyFloatLevel(new VFXExpression[2] {e0, e1}).ToArray();
            return f(unifiedExp[0], unifiedExp[1]);
        }

        // unified ternary op
        static public VFXExpression UnifyOp(Func<VFXExpression, VFXExpression, VFXExpression, VFXExpression> f, VFXExpression e0, VFXExpression e1, VFXExpression e2)
        {
            var unifiedExp = VFXOperatorUtility.UnifyFloatLevel(new VFXExpression[3] {e0, e1, e2}).ToArray();
            return f(unifiedExp[0], unifiedExp[1], unifiedExp[2]);
        }

        static public VFXExpression Negate(VFXExpression input)
        {
            var minusOne = VFXOperatorUtility.MinusOneExpression[VFXExpression.TypeToSize(input.valueType)];
            return (minusOne * input);
        }

        static public VFXExpression Clamp(VFXExpression input, VFXExpression min, VFXExpression max)
        {
            //Max(Min(x, max), min))
            var maxExp = new VFXExpressionMax(input, CastFloat(min, input.valueType));
            return new VFXExpressionMin(maxExp, CastFloat(max, input.valueType));
        }

        static public VFXExpression Saturate(VFXExpression input)
        {
            //Max(Min(x, 1.0f), 0.0f))
            return Clamp(input, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f));
        }

        static public VFXExpression Frac(VFXExpression input)
        {
            //x - floor(x)
            return input - new VFXExpressionFloor(input);
        }

        static public VFXExpression Sqrt(VFXExpression input)
        {
            //pow(x, 0.5f)
            return new VFXExpressionPow(input, HalfExpression[VFXExpression.TypeToSize(input.valueType)]);
        }

        static public VFXExpression Dot(VFXExpression a, VFXExpression b)
        {
            //a.x*b.x + a.y*b.y + ...
            var size = VFXExpression.TypeToSize(a.valueType);
            if (a.valueType != b.valueType)
            {
                throw new ArgumentException(string.Format("Invalid Dot type input : {0} and {1}", a.valueType, b.valueType));
            }

            var mul = (a * b);
            var sum = new Stack<VFXExpression>();
            if (size == 1)
            {
                sum.Push(mul);
            }
            else
            {
                for (int iChannel = 0; iChannel < size; ++iChannel)
                {
                    sum.Push(mul[iChannel]);
                }
            }

            while (sum.Count > 1)
            {
                var top = sum.Pop();
                var bottom = sum.Pop();
                sum.Push(top + bottom);
            }
            return sum.Pop();
        }

        static public VFXExpression Distance(VFXExpression x, VFXExpression y)
        {
            //length(a - b)
            return Length(x - y);
        }

        static public VFXExpression SqrDistance(VFXExpression x, VFXExpression y)
        {
            //dot(a - b)
            var delta = (x - y);
            return Dot(delta, delta);
        }

        static public VFXExpression Lerp(VFXExpression x, VFXExpression y, VFXExpression s)
        {
            //x + s(y - x)
            return (x + s * (y - x));
        }

        static public VFXExpression Length(VFXExpression v)
        {
            //sqrt(dot(v, v))
            var dot = Dot(v, v);
            return Sqrt(dot);
        }

        static public VFXExpression Normalize(VFXExpression v)
        {
            var invLength = (VFXOperatorUtility.OneExpression[1] / VFXOperatorUtility.Length(v));
            var invLengthVector = VFXOperatorUtility.CastFloat(invLength, v.valueType);
            return (v * invLengthVector);
        }

        static public VFXExpression Fmod(VFXExpression x, VFXExpression y)
        {
            //frac(x / y) * y
            return VFXOperatorUtility.Frac(x / y) * y;
        }

        static public VFXExpression Fit(VFXExpression value, VFXExpression oldRangeMin, VFXExpression oldRangeMax, VFXExpression newRangeMin, VFXExpression newRangeMax)
        {
            //percent = (value - oldRangeMin) / (oldRangeMax - oldRangeMin)
            //lerp(newRangeMin, newRangeMax, percent)
            VFXExpression percent = (value - oldRangeMin) / (oldRangeMax - oldRangeMin);
            return Lerp(newRangeMin, newRangeMax, percent);
        }

        static public VFXExpression Smoothstep(VFXExpression x, VFXExpression y, VFXExpression s)
        {
            VFXExpression t = (s - x) / (y - x);
            t = Clamp(t, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f));

            VFXExpression result = (VFXValue.Constant(3.0f) - VFXValue.Constant(2.0f) * t);

            result = (result * t);
            result = (result * t);

            return result;
        }

        static public VFXExpression Discretize(VFXExpression value, VFXExpression granularity)
        {
            return new VFXExpressionFloor(value / granularity) * granularity;
        }

        static public VFXExpression ColorLuma(VFXExpression color)
        {
            //(0.299*R + 0.587*G + 0.114*B)
            var coefficients = VFXValue.Constant(new Vector4(0.299f, 0.587f, 0.114f, 0.0f));
            return Dot(color, coefficients);
        }

        static public VFXExpression DegToRad(VFXExpression degrees)
        {
            return (degrees * CastFloat(VFXValue.Constant(Mathf.PI / 180.0f), degrees.valueType));
        }

        static public VFXExpression RadToDeg(VFXExpression radians)
        {
            return (radians * CastFloat(VFXValue.Constant(180.0f / Mathf.PI), radians.valueType));
        }

        static public VFXExpression PolarToRectangular(VFXExpression theta, VFXExpression distance)
        {
            //x = cos(angle) * distance
            //y = sin(angle) * distance
            var result = new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionCos(theta), new VFXExpressionSin(theta) });
            return (result * CastFloat(distance, VFXValueType.kFloat2));
        }

        static public VFXExpression[] RectangularToPolar(VFXExpression coord)
        {
            //theta = atan2(coord.y, coord.x)
            //distance = length(coord)
            var components = VFXOperatorUtility.ExtractComponents(coord).ToArray();
            var theta = new VFXExpressionATan2(components[1], components[0]);
            var distance = Length(coord);
            return new VFXExpression[] { theta, distance };
        }

        static public VFXExpression SphericalToRectangular(VFXExpression theta, VFXExpression phi, VFXExpression distance)
        {
            //x = cos(theta) * cos(phi) * distance
            //y = sin(theta) * cos(phi) * distance
            //z = sin(phi) * distance
            var cosTheta = new VFXExpressionCos(theta);
            var cosPhi = new VFXExpressionCos(phi);
            var sinTheta = new VFXExpressionSin(theta);
            var sinPhi = new VFXExpressionSin(phi);

            var x = (cosTheta * cosPhi);
            var y = sinPhi;
            var z = (sinTheta * cosPhi);

            var result = new VFXExpressionCombine(new VFXExpression[] { x, y, z });
            return (result * CastFloat(distance, VFXValueType.kFloat3));
        }

        static public VFXExpression[] RectangularToSpherical(VFXExpression coord)
        {
            //distance = length(coord)
            //theta = atan2(z, x)
            //phi = asin(y / distance)
            var components = VFXOperatorUtility.ExtractComponents(coord).ToArray();
            var distance = Length(coord);
            var theta = new VFXExpressionATan2(components[2], components[0]);
            var phi = new VFXExpressionASin(components[1] / distance);
            return new VFXExpression[] { theta, phi, distance };
        }

        static public VFXExpression CircleArea(VFXExpression radius)
        {
            //pi * r * r
            var pi = VFXValue.Constant(Mathf.PI);
            return (pi * radius * radius);
        }

        static public VFXExpression CircleCircumference(VFXExpression radius)
        {
            //2 * pi * r
            var two = VFXValue.Constant(2.0f);
            var pi = VFXValue.Constant(Mathf.PI);
            return (two * pi * radius);
        }

        static public VFXExpression BoxVolume(VFXExpression dimensions)
        {
            //x * y * z
            var components = ExtractComponents(dimensions).ToArray();
            return (components[0] * components[1] * components[2]);
        }

        static public VFXExpression SphereVolume(VFXExpression radius)
        {
            //(4 / 3) * pi * r * r * r
            var multiplier = VFXValue.Constant((4.0f / 3.0f) * Mathf.PI);
            return (multiplier * radius * radius * radius);
        }

        static public VFXExpression CylinderVolume(VFXExpression radius, VFXExpression height)
        {
            //pi * r * r * h
            var pi = VFXValue.Constant(Mathf.PI);
            return (pi * radius * radius * height);
        }

        static public VFXExpression ConeVolume(VFXExpression radius0, VFXExpression radius1, VFXExpression height)
        {
            //pi/3 * (r0 * r0 + r0 * r1 + r1 * r1) * h
            var piOver3 = VFXValue.Constant(Mathf.PI / 3.0f);
            VFXExpression r0r0 = (radius0 * radius0);
            VFXExpression r0r1 = (radius0 * radius1);
            VFXExpression r1r1 = (radius1 * radius1);
            VFXExpression result = (r0r0 + r0r1 + r1r1);
            return (piOver3 * result * height);
        }

        static public VFXExpression TorusVolume(VFXExpression majorRadius, VFXExpression minorRadius)
        {
            //(pi * r * r) * (2 * pi * R)
            return CircleArea(minorRadius) * CircleCircumference(majorRadius);
        }

        static public VFXExpression SignedDistanceToPlane(VFXExpression planePosition, VFXExpression planeNormal, VFXExpression position)
        {
            VFXExpression d = Dot(planePosition, planeNormal);
            return Dot(position, planeNormal) - d;
        }

        static public IEnumerable<VFXExpression> ExtractComponents(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kFloat)
            {
                return new[] { expression };
            }

            var components = new List<VFXExpression>();
            for (int i = 0; i < VFXExpression.TypeToSize(expression.valueType); ++i)
            {
                components.Add(expression[i]);
            }
            return components;
        }

        static public IEnumerable<VFXExpression> UnifyFloatLevel(IEnumerable<VFXExpression> inputExpression, float defaultValue = 0.0f)
        {
            if (inputExpression.Count() <= 1)
            {
                return inputExpression;
            }

            var maxValueType = inputExpression.Select(o => o.valueType).OrderBy(t => VFXExpression.TypeToSize(t)).Last();
            var newVFXExpression = inputExpression.Select(o => CastFloat(o, maxValueType, defaultValue));
            return newVFXExpression.ToArray();
        }

        static public VFXExpression CastFloat(VFXExpression from, VFXValueType toValueType, float defaultValue = 0.0f)
        {
            if (!VFXExpressionFloatOperation.IsFloatValueType(from.valueType) || !VFXExpressionFloatOperation.IsFloatValueType(toValueType))
            {
                throw new ArgumentException(string.Format("Invalid CastFloat : {0} to {1}", from, toValueType));
            }

            if (from.valueType == toValueType)
            {
                return from;
            }

            var fromValueType = from.valueType;
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
                    inputComponent[iChannel] = from[iChannel];
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
                    outputComponent[iChannel] = VFXValue.Constant(defaultValue);
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

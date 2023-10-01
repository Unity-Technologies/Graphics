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
        public static Dictionary<VFXValueType, VFXExpression> GenerateExpressionConstant(float baseValue)
        {
            return new Dictionary<VFXValueType, VFXExpression>()
            {
                { VFXValueType.Float, VFXValue.Constant(baseValue) },
                { VFXValueType.Float2, VFXValue.Constant(Vector2.one * baseValue) },
                { VFXValueType.Float3, VFXValue.Constant(Vector3.one * baseValue) },
                { VFXValueType.Float4, VFXValue.Constant(Vector4.one * baseValue) },
                { VFXValueType.Int32, VFXValue.Constant((int)baseValue) },
                { VFXValueType.Uint32, VFXValue.Constant((uint)baseValue) }
            };
        }

        public static readonly Dictionary<VFXValueType, VFXExpression> OneExpression = GenerateExpressionConstant(1.0f);
        public static readonly Dictionary<VFXValueType, VFXExpression> MinusOneExpression = GenerateExpressionConstant(-1.0f);
        public static readonly Dictionary<VFXValueType, VFXExpression> HalfExpression = GenerateExpressionConstant(0.5f);
        public static readonly Dictionary<VFXValueType, VFXExpression> ZeroExpression = GenerateExpressionConstant(0.0f);
        public static readonly Dictionary<VFXValueType, VFXExpression> TwoExpression = GenerateExpressionConstant(2.0f);
        public static readonly Dictionary<VFXValueType, VFXExpression> ThreeExpression = GenerateExpressionConstant(3.0f);
        public static readonly Dictionary<VFXValueType, VFXExpression> TenExpression = GenerateExpressionConstant(10.0f);
        public static readonly Dictionary<VFXValueType, VFXExpression> PiExpression = GenerateExpressionConstant(Mathf.PI);
        public static readonly Dictionary<VFXValueType, VFXExpression> TauExpression = GenerateExpressionConstant(2.0f * Mathf.PI);
        public static readonly Dictionary<VFXValueType, VFXExpression> E_NapierConstantExpression = GenerateExpressionConstant(Mathf.Exp(1));
        public static readonly Dictionary<VFXValueType, VFXExpression> EpsilonExpression = GenerateExpressionConstant(1e-5f);
        public static readonly Dictionary<VFXValueType, VFXExpression> EpsilonSqrExpression = GenerateExpressionConstant(1e-10f);


        public enum Base
        {
            Base2,
            Base10,
            BaseE,
        }

        static private VFXExpression BaseToConstant(Base _base, VFXValueType type)
        {
            switch (_base)
            {
                case Base.Base2: return TwoExpression[type];
                case Base.Base10: return TenExpression[type];
                case Base.BaseE: return E_NapierConstantExpression[type];
                default:
                    throw new NotImplementedException();
            }
        }

        static public VFXExpression Negate(VFXExpression input)
        {
            var minusOne = MinusOneExpression[input.valueType];
            return (minusOne * input);
        }

        static public VFXExpression Reciprocal(VFXExpression input)
        {
            return OneExpression[input.valueType] / input;
        }

        static public VFXExpression Mad(VFXExpression input, VFXExpression scale, VFXExpression bias)
        {
            return input * scale + bias;
        }

        static public VFXExpression Clamp(VFXExpression input, VFXExpression min, VFXExpression max)
        {
            return Clamp(input, min, max, true);
        }

        static public VFXExpression Clamp(VFXExpression input, VFXExpression min, VFXExpression max, bool autoCast)
        {
            //Max(Min(x, max), min))
            if (autoCast)
            {
                min = CastFloat(min, input.valueType);
                max = CastFloat(max, input.valueType);
            }
            var maxExp = new VFXExpressionMax(input, min);
            return new VFXExpressionMin(maxExp, max);
        }

        static public VFXExpression Saturate(VFXExpression input)
        {
            //Max(Min(x, 1.0f), 0.0f))
            return new VFXExpressionSaturate(input);
        }

        static public VFXExpression Frac(VFXExpression input)
        {
            //x - floor(x)
            return new VFXExpressionFrac(input);
        }

        static public VFXExpression Ceil(VFXExpression input)
        {
            // ceil(x) = -floor(-x)
            return new VFXExpressionCeil(input);
        }

        static public VFXExpression Round(VFXExpression input)
        {
            //x = floor(x + 0.5)
            return new VFXExpressionRound(input);
        }

        static public VFXExpression Log(VFXExpression input, VFXExpression _base)
        {
            //log2(x)/log2(b)
            return new VFXExpressionLog2(input) / new VFXExpressionLog2(_base);
        }

        static public VFXExpression Log(VFXExpression input, Base _base)
        {
            return Log(input, BaseToConstant(_base, input.valueType));
        }

        static public VFXExpression Exp(VFXExpression input, Base _base)
        {
            return new VFXExpressionPow(BaseToConstant(_base, input.valueType), input);
        }

        static public VFXExpression SnapToClosestPowerOfBase(VFXExpression input, VFXExpression _base)
        {
            var exactPower = Log(input, _base);
            var nextPower = Round(exactPower);
            return new VFXExpressionPow(_base, nextPower);
        }


        static public VFXExpression Atanh(VFXExpression input)
        {
            //0.5*Log((1+x)/(1-x), e)
            var half = HalfExpression[input.valueType];
            var one = OneExpression[input.valueType];
            var e = E_NapierConstantExpression[input.valueType];

            return half * Log((one + input) / (one - input), e);
        }

        static public VFXExpression SinH(VFXExpression input)
        {
            //0.5*(e^x - e^-x)
            var half = HalfExpression[input.valueType];
            var minusOne = MinusOneExpression[input.valueType];
            var e = E_NapierConstantExpression[input.valueType];

            return half * (new VFXExpressionPow(e, input) - new VFXExpressionPow(e, minusOne * input));
        }

        static public VFXExpression CosH(VFXExpression input)
        {
            //0.5*(e^x + e^-x)
            var half = HalfExpression[input.valueType];
            var minusOne = MinusOneExpression[input.valueType];
            var e = E_NapierConstantExpression[input.valueType];

            return half * (new VFXExpressionPow(e, input) + new VFXExpressionPow(e, minusOne * input));
        }

        static public VFXExpression TanH(VFXExpression input)
        {
            //(1-e^2x)/(1+e^2x)
            var two = TwoExpression[input.valueType];
            var one = OneExpression[input.valueType];
            var minusOne = MinusOneExpression[input.valueType];
            var e = E_NapierConstantExpression[input.valueType];
            var E_minusTwoX = new VFXExpressionPow(e, minusOne * two * input);

            return (one - E_minusTwoX) / (one + E_minusTwoX);
        }

        static public VFXExpression VanDerCorputSequence(VFXExpression bits) //expect an uint return a float
        {
            bits = bits << 16 | bits >> 16;
            bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAA) >> 1);
            bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCC) >> 2);
            bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0) >> 4);
            bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00) >> 8);

            return new VFXExpressionCastUintToFloat(bits) * new VFXValue<float>(2.3283064365386963e-10f); // / 0x100000000;
        }

        static public VFXExpression Sqrt(VFXExpression input)
        {
            //pow(x, 0.5f)
            return new VFXExpressionPow(input, HalfExpression[input.valueType]);
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

        static public VFXExpression Cross(VFXExpression lhs, VFXExpression rhs)
        {
            return new VFXExpressionCombine(new[]
            {
                lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.x * rhs.y - lhs.y * rhs.x,
            });
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

        static public VFXExpression InverseLerp(VFXExpression x, VFXExpression y, VFXExpression s)
        {
            //(s - x)/(y - x)
            return (s - x) / (y - x);
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
            var invLength = (OneExpression[VFXValueType.Float] / Length(v));
            var invLengthVector = CastFloat(invLength, v.valueType);
            return (v * invLengthVector);
        }

        static public VFXExpression SafeNormalize(VFXExpression v)
        {
            var sqrDist = Dot(v, v);
            var condition = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, sqrDist, VFXOperatorUtility.EpsilonSqrExpression[VFXValueType.Float]);
            return new VFXExpressionBranch(condition, VFXOperatorUtility.ZeroExpression[v.valueType], Normalize(v));
        }

        static public VFXExpression Modulo(VFXExpression x, VFXExpression y)
        {
            if (VFXExpression.IsFloatValueType(x.valueType))
            {
                //fmod : frac(x / y) * y
                return Frac(x / y) * y;
            }
            else
            {
                //Std 1152 If the quotient a/b is representable, the expression (a/b)*b + a%b shall equal a.
                return x - (x / y) * y;
            }
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
            var type = x.valueType;

            var t = (s - x) / (y - x);
            t = Clamp(t, ZeroExpression[type], OneExpression[type]);

            var result = (ThreeExpression[type] - TwoExpression[type] * t);

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
            return (result * CastFloat(distance, VFXValueType.Float2));
        }

        static public VFXExpression[] RectangularToPolar(VFXExpression coord)
        {
            //theta = atan2(coord.y, coord.x)
            //distance = length(coord)
            var theta = Atan2(coord);
            var distance = Length(coord);
            return new VFXExpression[] { theta, distance };
        }

        static public VFXExpression SphericalToRectangular(VFXExpression distance, VFXExpression theta, VFXExpression phi)
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
            return (result * CastFloat(distance, VFXValueType.Float3));
        }

        static public VFXExpression[] RectangularToSpherical(VFXExpression coord)
        {
            //distance = length(coord)
            //theta = atan2(z, x)
            //phi = asin(y / distance)
            var components = ExtractComponents(coord).ToArray();
            var distance = Length(coord);
            var theta = new VFXExpressionATan2(components[2], components[0]);
            var phi = new VFXExpressionASin(components[1] / distance);
            return new VFXExpression[] { distance, theta, phi };
        }

        static public VFXExpression CircleArea(VFXExpression radius, VFXExpression scale = null)
        {
            //pi * r * r
            var pi = VFXValue.Constant(Mathf.PI);
            var area = pi * radius * radius;
            if (scale != null) //Circle are inside z=0 plane
            {
                scale = new VFXExpressionAbs(scale);
                area = area * scale.x * scale.y;
            }
            return area;
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

        static public VFXExpression SphereVolume(VFXExpression radius, VFXExpression scale = null)
        {
            //(4 / 3) * pi * r * r * r
            var multiplier = VFXValue.Constant((4.0f / 3.0f) * Mathf.PI);
            var volume = (multiplier * radius * radius * radius);
            if (scale != null)
            {
                scale = new VFXExpressionAbs(scale);
                volume = volume * scale.x * scale.y * scale.z;
            }
            return volume;
        }

        static public VFXExpression ConeVolume(VFXExpression radius0, VFXExpression radius1, VFXExpression height, VFXExpression scale = null)
        {
            //pi/3 * (r0 * r0 + r0 * r1 + r1 * r1) * h
            var piOver3 = VFXValue.Constant(Mathf.PI / 3.0f);
            var r0r0 = (radius0 * radius0);
            var r0r1 = (radius0 * radius1);
            var r1r1 = (radius1 * radius1);
            var result = (r0r0 + r0r1 + r1r1);
            var volume = (piOver3 * result * height);
            if (scale != null)
            {
                scale = new VFXExpressionAbs(scale);
                volume = volume * scale.x * scale.y * scale.z;
            }
            return volume;
        }

        static public VFXExpression TorusVolume(VFXExpression majorRadius, VFXExpression minorRadius, VFXExpression scale = null)
        {
            //(pi * r * r) * (2 * pi * R)
            var volume = CircleArea(minorRadius) * CircleCircumference(majorRadius);
            if (scale != null)
            {
                scale = new VFXExpressionAbs(scale);
                volume = volume * scale.x * scale.y * scale.z;
            }
            return volume;
        }

        static public VFXExpression SignedDistanceToPlane(VFXExpression planePosition, VFXExpression planeNormal, VFXExpression position)
        {
            VFXExpression d = Dot(planePosition, planeNormal);
            return Dot(position, planeNormal) - d;
        }

        static public VFXExpression GammaToLinear(VFXExpression gamma)
        {
            var components = ExtractComponents(gamma).ToArray();
            if (components.Length != 3 && components.Length != 4)
                throw new ArgumentException("input expression must be a 3 or 4 components vector");

            VFXExpression exp = VFXValue.Constant(2.2f);
            for (int i = 0; i < 3; ++i)
                components[i] = new VFXExpressionPow(components[i], exp);

            return new VFXExpressionCombine(components);
        }

        static public VFXExpression LinearToGamma(VFXExpression linear)
        {
            var components = ExtractComponents(linear).ToArray();
            if (components.Length != 3 && components.Length != 4)
                throw new ArgumentException("input expression must be a 3 or 4 components vector");
            VFXExpression exp = VFXValue.Constant(1.0f / 2.2f);
            for (int i = 0; i < 3; ++i)
                components[i] = new VFXExpressionBranch(
                    new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Greater,components[i], ZeroExpression[VFXValueType.Float]),
                    new VFXExpressionPow(components[i], exp),
                    ZeroExpression[VFXValueType.Float]);

            return new VFXExpressionCombine(components);
        }

        static public IEnumerable<VFXExpression> ExtractComponents(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.Float)
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

        static public VFXExpression CastFloat(VFXExpression from, VFXValueType toValueType, float defaultValue = 0.0f)
        {
            if (!VFXExpressionNumericOperation.IsFloatValueType(from.valueType) || !VFXExpressionNumericOperation.IsFloatValueType(toValueType))
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

        static public VFXExpression BuildRandom(VFXSeedMode seedMode, bool constant, RandId randId, VFXExpression seed = null)
        {
            if (seedMode == VFXSeedMode.PerParticleStrip || constant)
            {
                if (seed == null)
                    throw new ArgumentNullException("seed");
                return FixedRandom(seed, seedMode);
            }
            return new VFXExpressionRandom(seedMode == VFXSeedMode.PerParticle, randId);
        }

        static public VFXExpression FixedRandom(uint hash, VFXSeedMode mode)
        {
            return FixedRandom(VFXValue.Constant<uint>(hash), mode);
        }

        static public VFXExpression FixedRandom(VFXExpression hash, VFXSeedMode mode)
        {
            VFXExpression seed = new VFXExpressionBitwiseXor(hash, VFXBuiltInExpression.SystemSeed);
            if (mode != VFXSeedMode.PerComponent)
                seed = new VFXExpressionBitwiseXor(new VFXAttributeExpression(mode == VFXSeedMode.PerParticle ? VFXAttribute.ParticleId : VFXAttribute.StripIndex), seed);
            return new VFXExpressionFixedRandom(seed);
        }

        public enum SequentialAddressingMode
        {
            Wrap,
            Clamp,
            Mirror
        };

        static public VFXExpression ApplyAddressingMode(VFXExpression index, VFXExpression count, SequentialAddressingMode mode)
        {
            VFXExpression r = null;
            count = new VFXExpressionMax(count, OneExpression[VFXValueType.Uint32]);

            if (mode == SequentialAddressingMode.Wrap)
            {
                r = Modulo(index, count);
            }
            else if (mode == SequentialAddressingMode.Clamp)
            {
                var countMinusOne = count - OneExpression[VFXValueType.Uint32];
                r = new VFXExpressionMin(index, countMinusOne);
            }
            else if (mode == SequentialAddressingMode.Mirror)
            {
                var two = TwoExpression[VFXValueType.Uint32];
                var cycle = count * two - two;
                cycle = new VFXExpressionMax(cycle, OneExpression[VFXValueType.Uint32]);
                var modulo = Modulo(index, cycle);
                //var compare = new VFXExpressionCondition(VFXCondition.Less, new VFXExpressionCastUintToFloat(modulo), new VFXExpressionCastUintToFloat(count)); <= Use this line for 7.x.x/8.x.x/9.x.x backport
                var compare = new VFXExpressionCondition(VFXValueType.Uint32, VFXCondition.Less, modulo, count);
                r = new VFXExpressionBranch(compare, modulo, cycle - modulo);
            }
            return r;
        }

        static public VFXExpression SequentialLine(VFXExpression start, VFXExpression end, VFXExpression index, VFXExpression count, SequentialAddressingMode mode)
        {
            VFXExpression dt = ApplyAddressingMode(index, count, mode);
            dt = new VFXExpressionCastUintToFloat(dt);
            var size = new VFXExpressionCastUintToFloat(count) - VFXOperatorUtility.OneExpression[VFXValueType.Float];
            size = new VFXExpressionMax(size, VFXOperatorUtility.OneExpression[VFXValueType.Float]);
            dt = dt / size;
            dt = new VFXExpressionCombine(dt, dt, dt);
            return VFXOperatorUtility.Lerp(start, end, dt);
        }

        static public VFXExpression SequentialCircle(VFXExpression center, VFXExpression radius, VFXExpression normal, VFXExpression up, VFXExpression index, VFXExpression count, SequentialAddressingMode mode)
        {
            VFXExpression countForAddressing = count;
            if (mode == SequentialAddressingMode.Clamp || mode == SequentialAddressingMode.Mirror)
            {
                //Explicitly close the circle loop, if `index` equals to `count`, adds an extra step.
                countForAddressing = count + OneExpression[VFXValueType.Uint32];
            }
            VFXExpression dt = ApplyAddressingMode(index, countForAddressing, mode);
            dt = new VFXExpressionCastUintToFloat(dt);
            dt = dt / new VFXExpressionCastUintToFloat(count);

            var cos = new VFXExpressionCos(dt * VFXOperatorUtility.TauExpression[VFXValueType.Float]) as VFXExpression;
            var sin = new VFXExpressionSin(dt * VFXOperatorUtility.TauExpression[VFXValueType.Float]) as VFXExpression;
            var left = VFXOperatorUtility.Normalize(VFXOperatorUtility.Cross(normal, up));

            radius = new VFXExpressionCombine(radius, radius, radius);
            sin = new VFXExpressionCombine(sin, sin, sin);
            cos = new VFXExpressionCombine(cos, cos, cos);

            return center + (cos * up + sin * left) * radius;
        }

        static public VFXExpression Sequential3D(VFXExpression origin, VFXExpression axisX, VFXExpression axisY, VFXExpression axisZ, VFXExpression index, VFXExpression countX, VFXExpression countY, VFXExpression countZ, SequentialAddressingMode mode)
        {
            index = ApplyAddressingMode(index, countX * countY * countZ, mode);
            var z = new VFXExpressionCastUintToFloat(VFXOperatorUtility.Modulo(index, countZ));
            var y = new VFXExpressionCastUintToFloat(VFXOperatorUtility.Modulo(index / countZ, countY));
            var x = new VFXExpressionCastUintToFloat(index / (countY * countZ));

            VFXExpression volumeSize = new VFXExpressionCombine(new VFXExpressionCastUintToFloat(countX), new VFXExpressionCastUintToFloat(countY), new VFXExpressionCastUintToFloat(countZ));
            volumeSize = volumeSize - VFXOperatorUtility.OneExpression[VFXValueType.Float3];
            var scaleAxisZero = Saturate(volumeSize); //Handle special case for one count => lead to be centered on origin (instead of -axis)
            volumeSize = new VFXExpressionMax(volumeSize, VFXOperatorUtility.OneExpression[VFXValueType.Float3]);
            var dt = new VFXExpressionCombine(x, y, z) / volumeSize;
            dt = dt * VFXOperatorUtility.TwoExpression[VFXValueType.Float3] - VFXOperatorUtility.OneExpression[VFXValueType.Float3];

            var r = origin;
            r += dt.xxx * scaleAxisZero.xxx * axisX;
            r += dt.yyy * scaleAxisZero.yyy * axisY;
            r += dt.zzz * scaleAxisZero.zzz * axisZ;

            return r;
        }

        static public VFXExpression GetPerspectiveMatrix(VFXExpression fov, VFXExpression aspect, VFXExpression zNear, VFXExpression zFar, VFXExpression lensShift)
        {
            var fovHalf = fov / TwoExpression[VFXValueType.Float];
            var cotangent = new VFXExpressionCos(fovHalf) / new VFXExpressionSin(fovHalf);
            var deltaZ = zNear - zFar;
            var minusTwoExp = MinusOneExpression[VFXValueType.Float] * TwoExpression[VFXValueType.Float];

            var zero = ZeroExpression[VFXValueType.Float];
            var m0 = new VFXExpressionCombine(cotangent / aspect, zero, zero, zero);
            var m1 = new VFXExpressionCombine(zero, cotangent, zero, zero);
            var m2 = new VFXExpressionCombine(minusTwoExp * lensShift.x, minusTwoExp * lensShift.y, MinusOneExpression[VFXValueType.Float] * (zFar + zNear) / deltaZ, OneExpression[VFXValueType.Float]);
            var m3 = new VFXExpressionCombine(zero, zero, TwoExpression[VFXValueType.Float] * zNear * zFar / deltaZ, zero);

            return new VFXExpressionVector4sToMatrix(m0, m1, m2, m3);
        }

        static public VFXExpression GetOrthographicMatrix(VFXExpression orthoSize, VFXExpression aspect, VFXExpression zNear, VFXExpression zFar)
        {
            var deltaZ = zNear - zFar;
            var oneOverSize = OneExpression[VFXValueType.Float] / orthoSize;

            var zero = ZeroExpression[VFXValueType.Float];
            var m0 = new VFXExpressionCombine(oneOverSize / aspect, zero, zero, zero);
            var m1 = new VFXExpressionCombine(zero, oneOverSize, zero, zero);
            var m2 = new VFXExpressionCombine(zero, zero, MinusOneExpression[VFXValueType.Float] * TwoExpression[VFXValueType.Float] / deltaZ, zero);
            var m3 = new VFXExpressionCombine(zero, zero, (zFar + zNear) / deltaZ, OneExpression[VFXValueType.Float]);

            return new VFXExpressionVector4sToMatrix(m0, m1, m2, m3);
        }

        static public VFXExpression InverseTransposeTRS(VFXExpression matrix)
        {
            return new VFXExpressionTransposeMatrix(new VFXExpressionInverseTRSMatrix(matrix));
        }

        static public VFXExpression IsTRSMatrixZeroScaled(VFXExpression matrix)
        {
            var i = new VFXExpressionMatrixToVector3s(matrix, VFXValue.Constant(0));
            var j = new VFXExpressionMatrixToVector3s(matrix, VFXValue.Constant(1));
            var k = new VFXExpressionMatrixToVector3s(matrix, VFXValue.Constant(2));

            var sqrLengthI = Dot(i, i);
            var sqrLengthJ = Dot(j, j);
            var sqrLengthK = Dot(k, k);

            var epsilon = EpsilonSqrExpression[VFXValueType.Float];

            var compareI = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, sqrLengthI, epsilon);
            var compareJ = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, sqrLengthJ, epsilon);
            var compareK = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, sqrLengthK, epsilon);

            var condition = new VFXExpressionLogicalOr(compareI, new VFXExpressionLogicalOr(compareJ, compareK));
            return condition;
        }

        static public VFXExpression Atan2(VFXExpression coord)
        {
            var components = ExtractComponents(coord).ToArray();
            var theta = new VFXExpressionATan2(components[1], components[0]);
            return theta;
        }

        static public VFXExpression Max3(VFXExpression x, VFXExpression y, VFXExpression z)
        {
            return new VFXExpressionMax(new VFXExpressionMax(x, y), z);
        }

        static public VFXExpression Max3(VFXExpression vector3)
        {
            var x = new VFXExpressionExtractComponent(vector3, 0);
            var y = new VFXExpressionExtractComponent(vector3, 1);
            var z = new VFXExpressionExtractComponent(vector3, 2);
            return Max3(x, y, z);
        }

        static public VFXExpression Min3(VFXExpression x, VFXExpression y, VFXExpression z)
        {
            return new VFXExpressionMin(new VFXExpressionMin(x, y), z);
        }

        static public VFXExpression Min3(VFXExpression vector3)
        {
            var x = new VFXExpressionExtractComponent(vector3, 0);
            var y = new VFXExpressionExtractComponent(vector3, 1);
            var z = new VFXExpressionExtractComponent(vector3, 2);
            return Min3(x, y, z);
        }

        static public VFXExpression UniformScaleMatrix(VFXExpression scale)
        {
            var scale3 = new VFXExpressionCombine(scale, scale, scale);
            var zero = ZeroExpression[VFXValueType.Float3];
            return new VFXExpressionTRSToMatrix(zero, zero, scale3);
        }
    }
}

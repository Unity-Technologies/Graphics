using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionRandom : VFXExpression
    {
        [Flags]
        public enum RandomFlags
        {
            Fixed = (1 << 0),
            PerElement = (1 << 1),

            None = 0
        }

        public VFXExpressionRandom() : this(RandomFlags.None, VFXValueType.kFloat, VFXValue.Constant(0)) {}

        public VFXExpressionRandom(RandomFlags flags, VFXValueType valueType, VFXExpression seed) : base((flags & RandomFlags.PerElement) != 0 ? VFXExpression.Flags.PerElement : VFXExpression.Flags.None, new VFXExpression[] { seed })
        {
            m_ValueType = valueType;
            m_RandomFlags = flags;
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXNoneOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return m_ValueType;
            }
        }

        private static float randLcg(ref uint seed, uint userSeed)
        {
            uint multiplier = 0x0019660d;
            uint increment = 0x3c6ef35f;

            seed = multiplier * seed + increment;
            uint finalSeed = seed ^ userSeed;
            return BitConverter.ToSingle(BitConverter.GetBytes((finalSeed >> 9) | 0x3f800000), 0) - 1.0f;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            uint seed = constParents[0].Get<uint>();

            VFXExpression result;
            switch (m_ValueType)
            {
                case VFXValueType.kFloat: result = VFXValue.Constant(randLcg(ref seed, 0)); break;
                case VFXValueType.kFloat2: result = VFXValue.Constant(new Vector2(randLcg(ref seed, 0), randLcg(ref seed, 0))); break;
                case VFXValueType.kFloat3: result = VFXValue.Constant(new Vector3(randLcg(ref seed, 0), randLcg(ref seed, 0), randLcg(ref seed, 0))); break;
                case VFXValueType.kFloat4: result = VFXValue.Constant(new Vector4(randLcg(ref seed, 0), randLcg(ref seed, 0), randLcg(ref seed, 0), randLcg(ref seed, 0))); break;
                default:
                    throw new NotImplementedException();
            }

            return result;
        }

        public override string GetCodeString(string[] parents)
        {
            if ((m_RandomFlags & RandomFlags.Fixed) != 0)
            {
                switch (m_ValueType)
                {
                    case VFXValueType.kFloat: return string.Format("FIXEDRAND({0})", parents[0]);
                    case VFXValueType.kFloat2: return string.Format("FIXEDRAND2({0})", parents[0]);
                    case VFXValueType.kFloat3: return string.Format("FIXEDRAND3({0})", parents[0]);
                    case VFXValueType.kFloat4: return string.Format("FIXEDRAND4({0})", parents[0]);
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (m_ValueType)
                {
                    case VFXValueType.kFloat: return string.Format("RAND({0})", parents[0]);
                    case VFXValueType.kFloat2: return string.Format("RAND2({0})", parents[0]);
                    case VFXValueType.kFloat3: return string.Format("RAND3({0})", parents[0]);
                    case VFXValueType.kFloat4: return string.Format("RAND4({0})", parents[0]);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            if ((m_RandomFlags & (RandomFlags.Fixed | RandomFlags.PerElement)) == RandomFlags.PerElement)
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
        }

        private VFXValueType m_ValueType;
        private RandomFlags m_RandomFlags;
    }
}

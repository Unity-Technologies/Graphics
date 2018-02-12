using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

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

        public VFXExpressionRandom() : this(RandomFlags.None, VFXValue.Constant(0u)) {}

        public VFXExpressionRandom(RandomFlags flags, VFXExpression seed) : base((flags & RandomFlags.PerElement) != 0 ? VFXExpression.Flags.PerElement : VFXExpression.Flags.None, (seed != null) ? new VFXExpression[] { seed }
                                                                                 : new VFXExpression[] {})
        {
            m_RandomFlags = flags;
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXGenerateRandomOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (constParents.Length > 0)
            {
                var oldState = UnityEngine.Random.state;
                UnityEngine.Random.InitState((int)constParents[0].Get<uint>());

                var result = VFXValue.Constant(UnityEngine.Random.value);

                UnityEngine.Random.state = oldState;

                return result;
            }
            else
            {
                return VFXValue.Constant(UnityEngine.Random.value);
            }
        }

        public override string GetCodeString(string[] parents)
        {
            if ((m_RandomFlags & RandomFlags.Fixed) != 0)
            {
                return string.Format("FIXED_RAND({0})", (parents.Length > 0) ? parents[0] : "0");
            }
            else
            {
                return string.Format("RAND");
            }
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            if ((m_RandomFlags & (RandomFlags.Fixed | RandomFlags.PerElement)) == (RandomFlags.Fixed | RandomFlags.PerElement))
                yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
            if ((m_RandomFlags & (RandomFlags.Fixed | RandomFlags.PerElement)) == RandomFlags.PerElement)
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
        }

        private RandomFlags m_RandomFlags;
    }
}

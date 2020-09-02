using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Runtime.CompilerServices;

namespace UnityEditor.VFX
{
    #pragma warning disable 0659
    class VFXExpressionRandom : VFXExpression
    {
        public VFXExpressionRandom(bool perElement, object parent, uint id = 0) : base(perElement ? VFXExpression.Flags.PerElement : VFXExpression.Flags.None)
        {
            m_Parent = new WeakReference(parent);
            m_Id = id;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = obj as VFXExpressionRandom;
            if (other == null)
                return false;

            return ReferenceEquals(m_Parent.Target, other.m_Parent.Target) && m_Id == other.m_Id;
        }

        protected override int GetInnerHashCode()
        {
            int hash = base.GetInnerHashCode();
            hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(m_Parent.Target);
            hash = (hash * 397) ^ (int)m_Id;
            return hash;
        }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.GenerateRandom; } }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return VFXValue.Constant(UnityEngine.Random.value);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("Rand(attributes.seed)");
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            if (Is(Flags.PerElement))
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
        }

        // These fields are used to check from expression equality
        private WeakReference m_Parent;
        private uint m_Id;
    }

    class VFXExpressionFixedRandom : VFXExpression
    {
        public VFXExpressionFixedRandom() : this(VFXValue<uint>.Default) {}
        public VFXExpressionFixedRandom(VFXExpression hash) : base(VFXExpression.Flags.None, hash) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.GenerateFixedRandom; }}

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState((int)constParents[0].Get<uint>());

            var result = VFXValue.Constant(UnityEngine.Random.value);

            UnityEngine.Random.state = oldState;

            return result;
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("FixedRand({0})", parents[0]);
        }
    }
    #pragma warning restore 0659
}

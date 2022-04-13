using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Runtime.CompilerServices;

namespace UnityEditor.VFX
{
    struct RandId
    {
        public RandId(object owner, int id = 0)
        {
            this.owner = new WeakReference(owner);
            this.id = id;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RandId))
                return false;

            var other = (RandId)obj;
            return ReferenceEquals(owner.Target, other.owner.Target) && id == other.id;
        }

        public override int GetHashCode()
        {
            // This is not good practice as hashcode will mutate when target gets destroyed but in our case we don't care.
            // Any entry in cache will just be lost, but it would have never been accessed anyway (as owner is lost)
            return (RuntimeHelpers.GetHashCode(owner.Target) * 397) ^ id;
        }

        WeakReference owner;
        int id;
    }

#pragma warning disable 0659
    class VFXExpressionRandom : VFXExpression
    {
        public VFXExpressionRandom(bool perElement, RandId id) : base(perElement ? VFXExpression.Flags.PerElement : VFXExpression.Flags.None)
        {
            m_Id = id;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = obj as VFXExpressionRandom;
            if (other == null)
                return false;

            return m_Id.Equals(other.m_Id);
        }

        protected override int GetInnerHashCode()
        {
            return (base.GetInnerHashCode() * 397) ^ m_Id.GetHashCode();
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

        private RandId m_Id;
    }

    class VFXExpressionFixedRandom : VFXExpression
    {
        public VFXExpressionFixedRandom() : this(VFXValue<uint>.Default) { }
        public VFXExpressionFixedRandom(VFXExpression hash) : base(VFXExpression.Flags.None, hash) { }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.GenerateFixedRandom; } }

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

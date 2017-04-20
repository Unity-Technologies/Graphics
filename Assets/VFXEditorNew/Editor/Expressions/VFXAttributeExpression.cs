using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    sealed class VFXAttributeExpression : VFXExpression
    {
        private static readonly VFXAttributeExpression Seed              = new VFXAttributeExpression("seed", VFXValueType.kUint);
        private static readonly VFXAttributeExpression Position          = new VFXAttributeExpression("position", VFXValueType.kFloat3);
        private static readonly VFXAttributeExpression Velocity          = new VFXAttributeExpression("velocity", VFXValueType.kFloat3);
        private static readonly VFXAttributeExpression Color             = new VFXAttributeExpression("color", VFXValueType.kFloat3);
        private static readonly VFXAttributeExpression Alpha             = new VFXAttributeExpression("alpha", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression Phase             = new VFXAttributeExpression("phase", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression Size              = new VFXAttributeExpression("size", VFXValueType.kFloat2);
        private static readonly VFXAttributeExpression Lifetime          = new VFXAttributeExpression("lifetime", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression Age               = new VFXAttributeExpression("age", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression Angle             = new VFXAttributeExpression("angle", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression AngularVelocity   = new VFXAttributeExpression("angularVelocity", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression TexIndex          = new VFXAttributeExpression("texIndex", VFXValueType.kFloat);
        private static readonly VFXAttributeExpression Pivot             = new VFXAttributeExpression("pivot", VFXValueType.kFloat3);
        private static readonly VFXAttributeExpression ParticleId        = new VFXAttributeExpression("particleId", VFXValueType.kUint);
        private static readonly VFXAttributeExpression Front             = new VFXAttributeExpression("front", VFXValueType.kFloat3);
        private static readonly VFXAttributeExpression Side              = new VFXAttributeExpression("side", VFXValueType.kFloat3);
        private static readonly VFXAttributeExpression Up                = new VFXAttributeExpression("up", VFXValueType.kFloat3);

        private static readonly VFXAttributeExpression[] AllExpressions = CollectStaticReadOnlyExpression<VFXAttributeExpression>(typeof(VFXAttributeExpression));
        public static readonly string[] All = AllExpressions.Select(e => e.attributeName).ToArray();

        public static VFXExpression Find(string attributeName)
        {
            var expression = AllExpressions.FirstOrDefault(e => e.attributeName == attributeName);
            if (expression == null)
            {
                Debug.LogErrorFormat("Unable to find attribute expression : {0}", attributeName);
            }
            return expression;
        }

        private VFXAttributeExpression(string name, VFXValueType type)
        {
            m_Flags = Flags.ValidOnGPU;
            m_AttributeName = name;
            m_ValueType = type;
        }

        public override VFXExpressionOp Operation
        {
            get
            {
                return VFXExpressionOp.kVFXNoneOp; //TODOPAUL : Should we need an explicit Op for this ?
            }
        }

        private VFXValueType m_ValueType;
        public override VFXValueType ValueType
        {
            get
            {
                return m_ValueType;
            }
        }

        private string m_AttributeName;
        public string attributeName
        {
            get
            {
                return m_AttributeName;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return this;
        }
    }
}
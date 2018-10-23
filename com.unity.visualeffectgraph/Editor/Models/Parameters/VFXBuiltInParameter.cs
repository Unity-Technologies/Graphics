using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class BuiltInVariant : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_expressionOp", VFXBuiltInExpression.All.Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "BuiltIn", variantProvider = typeof(BuiltInVariant))]
    class VFXBuiltInParameter : VFXOperator
    {
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected VFXExpressionOperation m_expressionOp;

        override public string name { get { return m_expressionOp.ToString(); } }

        public override void Sanitize()
        {
            if (VFXBuiltInExpression.Find(m_expressionOp) == null)
            {
                switch (m_expressionOp)
                {
                    // Due to reorganization, some indices have changed
                    case VFXExpressionOperation.Tan:         m_expressionOp = VFXExpressionOperation.DeltaTime; break;
                    case VFXExpressionOperation.ASin:        m_expressionOp = VFXExpressionOperation.TotalTime; break;
                    case VFXExpressionOperation.ACos:        m_expressionOp = VFXExpressionOperation.SystemSeed; break;
                    case VFXExpressionOperation.RGBtoHSV:    m_expressionOp = VFXExpressionOperation.LocalToWorld; break;
                    case VFXExpressionOperation.HSVtoRGB:    m_expressionOp = VFXExpressionOperation.WorldToLocal; break;

                    default:
                        Debug.LogWarning(string.Format("Expression operator for the BuiltInParameter is invalid ({0}). Reset to none", m_expressionOp));
                        m_expressionOp = VFXExpressionOperation.None;
                        break;
                }
            }

            if (outputSlots.Count > 0 && outputSlots[0].GetType() == typeof(Matrix4x4))
                outputSlots[0].Detach(); // In order not to have a bad conversion

            base.Sanitize(); // Will call ResyncSlots
        }

        private Type GetOutputType()
        {
            switch (m_expressionOp)
            {
                case VFXExpressionOperation.LocalToWorld:
                case VFXExpressionOperation.WorldToLocal:
                    return typeof(Transform);
                default:
                {
                    var exp = VFXBuiltInExpression.Find(m_expressionOp);
                    if (exp != null)
                        return VFXExpression.TypeToType(VFXBuiltInExpression.Find(m_expressionOp).valueType);
                    return null;
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                Type outputType = GetOutputType();
                if (outputType != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(outputType, m_expressionOp.ToString()));
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = VFXBuiltInExpression.Find(m_expressionOp);
            if (expression == null)
                return new VFXExpression[] {};
            return new VFXExpression[] { expression };
        }
    }
}

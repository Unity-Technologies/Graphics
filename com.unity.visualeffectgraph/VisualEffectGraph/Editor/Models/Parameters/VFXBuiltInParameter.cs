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
        protected VFXExpressionOp m_expressionOp;

        override public string name { get { return m_expressionOp.ToString(); } }

        public override void Sanitize()
        {
            if (VFXBuiltInExpression.Find(m_expressionOp) == null)
            {
                switch (m_expressionOp)
                {
                    // Do to reorganization, some indices have changed
                    case VFXExpressionOp.kVFXTanOp:         m_expressionOp = VFXExpressionOp.kVFXDeltaTimeOp; break;
                    case VFXExpressionOp.kVFXASinOp:        m_expressionOp = VFXExpressionOp.kVFXTotalTimeOp; break;
                    case VFXExpressionOp.kVFXACosOp:        m_expressionOp = VFXExpressionOp.kVFXSystemSeedOp; break;
                    case VFXExpressionOp.kVFXRGBtoHSVOp:    m_expressionOp = VFXExpressionOp.kVFXLocalToWorldOp; break;
                    case VFXExpressionOp.kVFXHSVtoRGBOp:    m_expressionOp = VFXExpressionOp.kVFXWorldToLocalOp; break;

                    default:
                        Debug.LogWarning(string.Format("Expression operator for the BuiltInParameter is invalid ({0}). Reset to none", m_expressionOp));
                        m_expressionOp = VFXExpressionOp.kVFXNoneOp;
                        break;
                }
            }
            base.Sanitize(); // Will call ResyncSlots
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var expression = VFXBuiltInExpression.Find(m_expressionOp);
                if (expression != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(expression.valueType), m_expressionOp.ToString()));
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

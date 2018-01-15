using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

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

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var expression = VFXBuiltInExpression.Find(this.m_expressionOp);
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

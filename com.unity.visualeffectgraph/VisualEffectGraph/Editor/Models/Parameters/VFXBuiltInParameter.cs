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
        [SerializeField, VFXSetting]
        protected VFXExpressionOp m_expressionOp;

        protected sealed override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "m_expressionOp";
            }
        }

        override public string name { get { return m_expressionOp.ToString(); } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = VFXBuiltInExpression.Find(m_expressionOp);
            if (expression == null)
                return new VFXExpression[] {};
            return new VFXExpression[] { expression };
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    // DEPRECATED
    class VFXBuiltInParameter : VFXOperator
    {
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected VFXExpressionOperation m_expressionOp;

        override public string name { get { return ObjectNames.NicifyVariableName(m_expressionOp.ToString()); } }

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
                return new VFXExpression[] { };
            return new VFXExpression[] { expression };
        }

        public override void Sanitize(int version)
        {
            Debug.Log("Sanitizing Graph: Automatically replace BuiltInParameter by new BuiltInParameter implementation for " + m_expressionOp.ToString());

            var entry = VFXDynamicBuiltInParameter.s_BuiltInInfo.First(o => o.Value.expression.operation == m_expressionOp);
            VFXDynamicBuiltInParameter.BuiltInFlag newBuiltIn = entry.Key;

            var newBuiltinParameter = CreateInstance<VFXDynamicBuiltInParameter>();
            newBuiltinParameter.SetSettingValue("m_BuiltInParameters", newBuiltIn);
            VFXSlot.CopyLinksAndValue(newBuiltinParameter.GetOutputSlot(0), GetOutputSlot(0), true);
            ReplaceModel(newBuiltinParameter, this);

            base.Sanitize(version);
        }
    }
}

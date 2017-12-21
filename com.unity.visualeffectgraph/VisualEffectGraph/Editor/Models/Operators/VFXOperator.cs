using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXOperator()
        {
            m_UICollapsed = false;
        }

        protected IEnumerable<VFXExpression> GetRawInputExpressions()
        {
            List<VFXExpression> results = new List<VFXExpression>();
            GetInputExpressionsRecursive(results, inputSlots);
            return results;
        }

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return GetRawInputExpressions();
        }

        private static void GetInputExpressionsRecursive(List<VFXExpression> results, IEnumerable<VFXSlot> slots)
        {
            foreach (var s in slots)
            {
                if (s.GetExpression() != null)
                {
                    results.Add(s.GetExpression());
                }
                else
                {
                    GetInputExpressionsRecursive(results, s.children);
                }
            }
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        // As connections changed can be triggered from ResyncSlots, we need to make sure it is not reentrant
        [NonSerialized]
        private bool m_ResyncingSlots = false;

        protected override void ResyncSlots(bool notify)
        {
            if (!m_ResyncingSlots)
            {
                m_ResyncingSlots = true;
                try
                {
                    base.ResyncSlots(notify);
                    if (notify)
                        foreach (var slot in outputSlots) // invalidate expressions on output slots
                            slot.InvalidateExpressionTree();
                }
                finally
                {
                    m_ResyncingSlots = false;
                }
            }
        }

        sealed override protected void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged)
            {
                //VFXSlot slot = model as VFXSlot;
                //if (slot != null && slot.direction == VFXSlot.Direction.kInput)
                ResyncSlots(true);
            }

            base.OnInvalidate(model, cause);
        }

        protected void SetOutputExpressions(VFXExpression[] outputExpressions)
        {
            if (outputExpressions.Length != GetNbOutputSlots())
                throw new Exception(string.Format("Numbers of ouput expressions ({0}) does not match number of output slots ({1})", outputExpressions.Length, GetNbOutputSlots()));

            int i = 0;
            foreach (var slot in outputSlots)
                slot.SetExpression(outputExpressions[i++]);
        }

        public override void UpdateOutputExpressions()
        {
            var inputExpressions = GetInputExpressions();
            var outputExpressions = BuildExpression(inputExpressions.ToArray());
            SetOutputExpressions(outputExpressions);
        }
    }
}

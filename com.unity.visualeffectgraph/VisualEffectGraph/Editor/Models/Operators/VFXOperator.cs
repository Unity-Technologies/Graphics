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

        private static void GetOutputWithExpressionSlotRecursive(List<VFXSlot> result, IEnumerable<VFXSlot> slots)
        {
            foreach (var s in slots)
            {
                if (s.DefaultExpr != null) /* actually GetExpression, but this way, we avoid a recursion  */
                {
                    result.Add(s);
                }
                else
                {
                    GetOutputWithExpressionSlotRecursive(result, s.children);
                }
            }
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        // As connections changed can be triggered from ResyncSlots, we need to make sure it is not reentrant
        [NonSerialized]
        private bool m_ResyncingSlots = false;

        public override bool ResyncSlots(bool notify)
        {
            bool changed = false;
            if (!m_ResyncingSlots)
            {
                m_ResyncingSlots = true;
                try
                {
                    changed = base.ResyncSlots(notify);
                    if (notify)
                        foreach (var slot in outputSlots) // invalidate expressions on output slots
                            slot.InvalidateExpressionTree();
                }
                finally
                {
                    m_ResyncingSlots = false;
                }
            }
            return changed;
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
            var outputSlotWithExpression = new List<VFXSlot>();
            GetOutputWithExpressionSlotRecursive(outputSlotWithExpression, outputSlots);
            if (outputExpressions.Length != outputSlotWithExpression.Count)
                throw new Exception(string.Format("Numbers of output expressions ({0}) does not match number of output (with expression)s slots ({1})", outputExpressions.Length, outputSlotWithExpression.Count));

            for (int i = 0; i < outputSlotWithExpression.Count; ++i)
                outputSlotWithExpression[i].SetExpression(outputExpressions[i]);
        }

        public override void UpdateOutputExpressions()
        {
            var inputExpressions = GetInputExpressions();
            var outputExpressions = BuildExpression(inputExpressions.ToArray());
            SetOutputExpressions(outputExpressions);
        }
    }
}

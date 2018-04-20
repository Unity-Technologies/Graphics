using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXOperator()
        {
            m_UICollapsed = false;
        }

        private static void GetSlotPredicateRecursive(List<VFXSlot> result, IEnumerable<VFXSlot> slots, Func<VFXSlot, bool> fnTest)
        {
            foreach (var s in slots)
            {
                if (fnTest(s))
                {
                    result.Add(s);
                }
                else
                {
                    GetSlotPredicateRecursive(result, s.children, fnTest);
                }
            }
        }

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

        protected override sealed void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged)
            {
                //VFXSlot slot = model as VFXSlot;
                //if (slot != null && slot.direction == VFXSlot.Direction.kInput)
                ResyncSlots(true);
            }

            base.OnInvalidate(model, cause);
        }

        public override sealed void UpdateOutputExpressions()
        {
            var outputSlotWithExpression = new List<VFXSlot>();
            var inputSlotWithExpression = new List<VFXSlot>();
            GetSlotPredicateRecursive(outputSlotWithExpression, outputSlots, s => s.DefaultExpr != null);
            GetSlotPredicateRecursive(inputSlotWithExpression, inputSlots, s => s.GetExpression() != null);

            var currentSpace = CoordinateSpace.Local;
            var inputSlotSpaceable = inputSlots.Where(o => o.Spaceable);
            if (inputSlotSpaceable.Any())
            {
                currentSpace = inputSlots.Select(o => o.Space).Distinct().OrderBy(o => (int)o).First();
            }

            var inputExpressions = inputSlotWithExpression.Select(o => ConvertSpace(o.GetExpression(), o, currentSpace));
            inputExpressions = ApplyPatchInputExpression(inputExpressions);

            var outputExpressions = BuildExpression(inputExpressions.ToArray());
            if (outputExpressions.Length != outputSlotWithExpression.Count)
                throw new Exception(string.Format("Numbers of output expressions ({0}) does not match number of output (with expression)s slots ({1})", outputExpressions.Length, outputSlotWithExpression.Count));

            for (int i = 0; i < outputSlotWithExpression.Count; ++i)
            {
                var slot = outputSlotWithExpression[i];
                if (slot.Spaceable)
                {
                    slot.Space = currentSpace;
                }
                slot.SetExpression(outputExpressions[i]);
            }
        }

        protected virtual IEnumerable<VFXExpression> ApplyPatchInputExpression(IEnumerable<VFXExpression> inputExpression)
        {
            return inputExpression;
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);
    }
}

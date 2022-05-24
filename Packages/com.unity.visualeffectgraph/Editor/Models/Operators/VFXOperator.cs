using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

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

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            var outputSlotSpaceable = outputSlots.Where(o => o.spaceable);
            bool needUpdateInputSpaceable = false;
            foreach (var output in outputSlotSpaceable)
            {
                var currentSpaceForSlot = GetOutputSpaceFromSlot(output);
                if (currentSpaceForSlot != output.space)
                {
                    output.space = currentSpaceForSlot;
                    needUpdateInputSpaceable = true;
                }
            }

            //If one of output slot has changed its space, expression tree for inputs,
            //and more generally, current operation expression graph is invalid.
            //=> Trigger invalidation on input is enough to recompute the graph from this operator
            if (needUpdateInputSpaceable)
            {
                var inputSlotSpaceable = inputSlots.Where(o => o.spaceable);
                foreach (var input in inputSlotSpaceable)
                {
                    input.Invalidate(InvalidationCause.kSpaceChanged);
                }
            }

            if (cause == InvalidationCause.kConnectionChanged)
            {
                if (model is VFXSlot slot && slot.direction == VFXSlot.Direction.kInput)
                    ResyncSlots(true);
            }

            if (cause == InvalidationCause.kParamChanged ||
                cause == InvalidationCause.kExpressionValueInvalidated)
            {
                if (model is VFXSlot && ((VFXSlot)model).direction == VFXSlot.Direction.kInput)
                {
                    foreach (var slot in outputSlots)
                        slot.Invalidate(InvalidationCause.kExpressionValueInvalidated);
                }
            }

            base.OnInvalidate(model, cause);
        }

        public override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            /* Most common case : space is the maximal output space from input slot */
            var space = VFXCoordinateSpace.None;
            foreach (var inputSlot in inputSlots)
            {
                if (inputSlot.spaceable)
                {
                    var currentSpace = inputSlot.space;
                    if (space == VFXCoordinateSpace.None || (currentSpace != VFXCoordinateSpace.None && space < currentSpace))
                    {
                        space = currentSpace;
                    }
                }
            }
            return space;
        }

        public override sealed void UpdateOutputExpressions()
        {
            var outputSlotWithExpression = new List<VFXSlot>();
            var inputSlotWithExpression = new List<VFXSlot>();
            GetSlotPredicateRecursive(outputSlotWithExpression, outputSlots, s => s.DefaultExpr != null);
            GetSlotPredicateRecursive(inputSlotWithExpression, inputSlots, s => s.GetExpression() != null);

            IEnumerable<VFXExpression> inputExpressions = inputSlotWithExpression.Select(o => o.GetExpression());
            inputExpressions = ApplyPatchInputExpression(inputExpressions);

            var outputExpressions = BuildExpression(inputExpressions.ToArray());
            if (outputExpressions.Length != outputSlotWithExpression.Count)
                throw new Exception(string.Format("Numbers of output expressions ({0}) does not match number of output (with expression)s slots ({1})", outputExpressions.Length, outputSlotWithExpression.Count));

            for (int i = 0; i < outputSlotWithExpression.Count; ++i)
            {
                var slot = outputSlotWithExpression[i];
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
